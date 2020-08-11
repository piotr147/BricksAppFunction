using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BricksAppFunction
{
    public static class MonitorSubscriptions
    {
        private const int HourOfDailyReport = 22; // It's Azure's hour, not ours
        private static readonly SendGridClient _client = new SendGridClient(Environment.GetEnvironmentVariable("sendgrid_key"));

        [FunctionName("MonitorSubscriptions")]
        public async static Task Run([TimerTrigger("0 0 4-23 * * *")] TimerInfo myTimer, ILogger log)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();
                List<LegoSet> sets = GetSetsOfActiveSubscriptions(conn);
                List<LegoSet> updatedSets = await GetSetsToUpdate(sets);
                UpdateWithInfoFromDb(conn, updatedSets);
                Dictionary<int, MailMessage> messages = GetMessagesForUpdatedSets(updatedSets);
                List<Subscription> subscriptions = GetActiveSubscriptions(conn);
                UpdateSetsInDb(conn, updatedSets);
                await SendEmails(subscriptions, messages);
            }

            stopwatch.Stop();
            log.LogInformation($"Stopwatch: {stopwatch.ElapsedMilliseconds}");
            if (TimeForDailyReport())
            {
                DbUtils.ArchiveSets();
                await SendPerformanceLogEmail((int)stopwatch.ElapsedMilliseconds);
            }
        }

        private static List<LegoSet> GetSetsOfActiveSubscriptions(SqlConnection conn)
        {
            string query = @"select * from Sets where number in (select setnumber from subscriptions where isdeleted = 0);";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();
            return ReadSetsFromQueryResults(reader);
        }

        private static List<LegoSet> ReadSetsFromQueryResults(SqlDataReader reader)
        {
            List<LegoSet> sets = new List<LegoSet>();

            while (reader.Read())
            {
                sets.Add(
                new LegoSet
                {
                    Number = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Series = reader.GetString(2),
                    Link = reader.GetString(3),
                    LowestPrice = (decimal)reader.GetDouble(4),
                    LowestPriceEver = (decimal)reader.GetDouble(5),
                    LastUpdate = reader.GetDateTime(6),
                    LastLowestPrice = reader.IsDBNull(7) ? null : (decimal?)reader.GetFloat(7),
                    LastReportedLowestPrice = (decimal)reader.GetFloat(10)
                });
            }

            return sets;
        }

        private async static Task<List<LegoSet>> GetSetsToUpdate(List<LegoSet> sets)
        {
            List<LegoSet> updatedSets = new List<LegoSet>();

            foreach (LegoSet set in sets)
            {
                LegoSet updatedSet = await PromoklockiHtmlParser.GetSetInfo(set.Link);
                updatedSet.LastLowestPrice = set.LowestPrice;
                if (updatedSet.LowestPrice != set.LowestPrice)
                {
                    updatedSets.Add(updatedSet);
                }
            }

            return updatedSets;
        }

        private static void UpdateWithInfoFromDb(SqlConnection conn, List<LegoSet> updatedSets)
        {
            foreach (var set in updatedSets)
            {
                UpdateLastReportedPrice(conn, set);
            }
        }

        private static void UpdateLastReportedPrice(SqlConnection conn, LegoSet set)
        {
            string query = @"
                select LastReportedLowestPrice from sets
                where number = @number";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@number", SqlDbType.Float).Value = set.Number;
            set.WithLastReportedLowestPrice(Convert.ToDecimal(cmd.ExecuteScalar()));
        }

        private static void UpdateSetsInDb(SqlConnection conn, List<LegoSet> updatedSets)
        {
            UpdatePricesAndShops(conn, updatedSets);
            UpdatesLastUpdates(conn);
        }

        private static void UpdatePricesAndShops(SqlConnection conn, List<LegoSet> updatedSets)
        {
            foreach (LegoSet set in updatedSets)
            {
                string query = $@"
                    update Sets set lowestPrice = @lowestPrice
                    ,lowestPriceEver = @lowestPriceEver
                    ,lastLowestPrice = @lastLowestPrice
                    ,lowestShop = @lowestShop
                    ,lastReportedLowestPrice = @lastReportedLowestPrice
                    where number = @catalogNumber;";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add("@lowestPrice", SqlDbType.Float).Value = set.LowestPrice;
                cmd.Parameters.Add("@lowestPriceEver", SqlDbType.Float).Value = set.LowestPriceEver;
                cmd.Parameters.Add("@lastLowestPrice", SqlDbType.Float).Value = set.LastLowestPrice;
                cmd.Parameters.Add("@lowestShop", SqlDbType.VarChar).Value = set.LowestShop ?? string.Empty;
                cmd.Parameters.Add("@catalogNumber", SqlDbType.Int).Value = set.Number;
                cmd.Parameters.Add("@lastReportedLowestPrice", SqlDbType.Float).Value = set.LastReportedLowestPrice;
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdatesLastUpdates(SqlConnection conn)
        {
            string query = $@"
                update Sets set lastUpdate = @lastUpdate;
                update Sets set dailyLowestPrice = lowestPrice where lowestPrice < dailyLowestPrice";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@lastUpdate", SqlDbType.DateTime).Value = DateTime.Now;
            cmd.ExecuteNonQuery();
        }

        private static Dictionary<int, MailMessage> GetMessagesForUpdatedSets(List<LegoSet> updatedSets)
        {
            var messages = new Dictionary<int, MailMessage>();

            foreach (LegoSet set in updatedSets)
            {
                if (set.LastLowestPrice != null)
                {
                    messages.Add(
                        set.Number,
                        new MailMessage {
                            Plain = GetUpdatePlainMessage(set),
                            Html = GetUpdateHtmlMessage(set),
                            IsBigUpdate = CheckForBigUpdates(set)
                        });
                }
            }

            return messages;
        }

        private static string GetUpdateHtmlMessage(LegoSet set) =>
            $@"
                Lego {set.Series} - {set.Number} - <strong>{set.Name}</strong>
                {set.LowestShop}
                {PriceInfoLine(set)}
                <a href=""{set.Link}"">{set.Link}</a>";

        private static string GetUpdatePlainMessage(LegoSet set)
        {
            string verbToUse = set.LowestPrice > set.LastLowestPrice ? "decreased" : "increased";
            return $@"
                Lego {set.Series} - {set.Number} - {set.Name}\n
                {set.LowestShop}\n
                Price {verbToUse} from {set.LastLowestPrice} to {set.LowestPrice}\n
                {set.Link}";
        }

        private static string PriceInfoLine(LegoSet set)
        {
            string verbToUse = set.LowestPrice < set.LastLowestPrice ? "decreased" : "increased";
            string color = set.LowestPrice < set.LastLowestPrice ? "green" : "red";
            return @$"
                <p style=""color:{color};"">
                    Price {verbToUse} from {set.LastLowestPrice} to {set.LowestPrice}
                </p>";
        }

        private static bool CheckForBigUpdates(LegoSet set)
        {
            if(Math.Abs(set.LowestPrice - set.LastReportedLowestPrice) / set.LowestPrice > 0.01m)
            {
                set.LastReportedLowestPrice = set.LowestPrice;
                return true;
            }

            return false;
        }

        private static List<Subscription> GetActiveSubscriptions(SqlConnection conn)
        {
            var subscriptions = new List<Subscription>();
            string query = @"
                select s2.mail, s1.SetNumber, s1.onlyBigUpdates from subscriptions s1
                join subscribers s2 on s2.id = s1.subscriberid
                where s1.isdeleted = 0;";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                using SqlDataReader reader = cmd.ExecuteReader();
                subscriptions = ReadSubscriptions(reader);
            }

            return subscriptions;
        }

        private static List<Subscription> ReadSubscriptions(SqlDataReader reader)
        {
            var subscriptions = new List<Subscription>();
            while (reader.Read())
            {
                subscriptions.Add(
                    new Subscription
                    {
                        Mail = reader.GetString(0),
                        CatalogNumber = reader.GetInt32(1),
                        OnlyBigChanges = reader.GetBoolean(2)
                    });
            }

            return subscriptions;
        }

        private async static Task SendEmails(
            List<Subscription> subscriptions,
            Dictionary<int, MailMessage> messages)
        {
            string subject = $"Lego update {DateTime.Now.Day:D2}.{DateTime.Now.Month:D2}";
            List<string> mails = subscriptions.Select(s => s.Mail).Distinct().ToList();
            var senderMail = new EmailAddress("piotr.koskiewicz@gmail.com", "Piotr");

            foreach (string mail in mails)
            {
                try
                {
                    List<(int number, bool onlyBig)> setNumbers = subscriptions.Where(s => s.Mail == mail).Select(s => (s.CatalogNumber, s.OnlyBigChanges)).ToList();

                    List<string> plainSetsMessages = messages.Where(m => setNumbers.Any(s => s.number == m.Key && (!s.onlyBig || m.Value.IsBigUpdate))).Select(m => m.Value.Plain).ToList();
                    List<string> htmlSetsMessages = messages.Where(m => setNumbers.Any(s => s.number == m.Key && (!s.onlyBig || m.Value.IsBigUpdate))).Select(m => m.Value.Html).ToList();
                    string plainMessage = string.Join('\n', plainSetsMessages);
                    string htmlMessage = string.Join("<br/><hr/>", htmlSetsMessages);

                    if(string.IsNullOrWhiteSpace(plainMessage) || string.IsNullOrWhiteSpace(htmlMessage))
                    {
                        continue;
                    }

                    var receiverMail = new EmailAddress(mail, "Brick buddy");
                    SendGridMessage msg = MailHelper.CreateSingleEmail(senderMail, receiverMail, subject, plainMessage, htmlMessage);
                    await _client.SendEmailAsync(msg);
                }
                catch (Exception)
                {
                }
            }
        }

        private static bool TimeForDailyReport() => DateTime.Now.Hour == HourOfDailyReport;

        private static async Task SendPerformanceLogEmail(int miliseconds)
        {
            var senderMail = new EmailAddress("piotr.koskiewicz@gmail.com", "Piotr");
            string subject = $"Lego performance report {DateTime.Now.Day}.{DateTime.Now.Month}";
            string message = $"ElapsedMilliseconds: {miliseconds}, hour: {DateTime.Now.Hour}";

            var receiverMail = new EmailAddress("piotr888k@gmail.com", "Brick buddy");
            SendGridMessage msg = MailHelper.CreateSingleEmail(senderMail, receiverMail, subject, message, message);
            await _client.SendEmailAsync(msg);
        }
    }
}

