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
        public async static Task Run([TimerTrigger("0 0 4-23 * * *")]TimerInfo myTimer, ILogger log)
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
                UpdateSetsInDb(conn, updatedSets);
                Dictionary<int, (string plain, string html)> messages = GetMessagesForUpdatedSets(updatedSets);
                List<(string mail, int catalogNumber)> subscriptions = GetActiveSubscriptions(conn);
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
                    LastLowestPrice = reader.IsDBNull(7) ? null : (decimal?)reader.GetFloat(7)
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
                if(updatedSet.LowestPrice != set.LowestPrice)
                {
                    updatedSets.Add(updatedSet);
                }
            }

            return updatedSets;
        }

        private static void UpdateSetsInDb(SqlConnection conn, List<LegoSet> updatedSets)
        {
            UpdatePrices(conn, updatedSets);
            UpdatesLastUpdates(conn);
        }

        private static void UpdatePrices(SqlConnection conn, List<LegoSet> updatedSets)
        {
            foreach (LegoSet set in updatedSets)
            {
                string query = $@"
                    update Sets set lowestPrice = @lowestPrice
                    ,lowestPriceEver = @lowestPriceEver
                    ,lastLowestPrice = @lastLowestPrice
                    where number = @catalogNumber;";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.Add("@lowestPrice", SqlDbType.Float).Value = set.LowestPrice;
                cmd.Parameters.Add("@lowestPriceEver", SqlDbType.Float).Value = set.LowestPriceEver;
                cmd.Parameters.Add("@lastLowestPrice", SqlDbType.Float).Value = set.LastLowestPrice;
                cmd.Parameters.Add("@catalogNumber", SqlDbType.Int).Value = set.Number;
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

        private static Dictionary<int, (string plain, string html)> GetMessagesForUpdatedSets(List<LegoSet> updatedSets)
        {
            var messages = new Dictionary<int, (string plain, string html)>();

            foreach(LegoSet set in updatedSets)
            {
                if(set.LastLowestPrice != null)
                {
                    messages.Add(set.Number, (GetUpdatePlainMessage(set), GetUpdateHtmlMessage(set)));
                }
            }

            return messages;
        }

        private static string GetUpdateHtmlMessage(LegoSet set) =>
            $@"
                Lego {set.Series} - {set.Number} - <strong>{set.Name}</strong>
                {PriceInfoLine(set)}
                <a href=""{set.Link}"">{set.Link}</a>";

        private static string GetUpdatePlainMessage(LegoSet set)
        {
            string verbToUse = set.LowestPrice > set.LastLowestPrice ? "decreased" : "increased";
            return $@"Lego {set.Series} - {set.Number} - {set.Name}\n
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

        private static List<(string mail, int catalogNumber)> GetActiveSubscriptions(SqlConnection conn)
        {
            var subscriptions = new List<(string mail, int catalogNumber)>();
            string query = @"
                select s2.mail, s1.SetNumber from subscriptions s1
                join subscribers s2 on s2.id = s1.subscriberid
                where s1.isdeleted = 0;";

            using(SqlCommand cmd = new SqlCommand(query, conn))
            {
                using SqlDataReader reader = cmd.ExecuteReader();
                subscriptions = ReadSubscriptions(reader);
            }

            return subscriptions;
        }

        private static List<(string mail, int catalogNumber)> ReadSubscriptions(SqlDataReader reader)
        {
            var subscriptions = new List<(string mail, int catalogNumber)>();
            while (reader.Read())
            {
                subscriptions.Add((reader.GetString(0), reader.GetInt32(1)));
            }

            return subscriptions;
        }

        private async static Task SendEmails(List<(string mail, int catalogNumber)> subscriptions, Dictionary<int, (string plain, string html)> messages)
        {
            string subject = $"Lego update {DateTime.Now.Day:D2}.{DateTime.Now.Month:D2}";
            List<string> mails = subscriptions.Select(s => s.mail).Distinct().ToList();
            var senderMail = new EmailAddress("piotr.koskiewicz@gmail.com", "Piotr");

            foreach (string mail in mails)
            {
                try
                {
                    List<int> setNumbers = subscriptions.Where(s => s.mail == mail).Select(s => s.catalogNumber).ToList();
                    List<string> plainSetsMessages = messages.Where(m => setNumbers.Contains(m.Key)).Select(m => (m.Value.plain)).ToList();
                    List<string> htmlSetsMessages = messages.Where(m => setNumbers.Contains(m.Key)).Select(m => (m.Value.html)).ToList();
                    string plainMessage = string.Join('\n', plainSetsMessages);
                    string htmlMessage = string.Join("<br/><hr/>", htmlSetsMessages);

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
