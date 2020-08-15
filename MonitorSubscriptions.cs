using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BricksAppFunction.Utilities;
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
                List<LegoSet> sets = DbUtils.GetSetsOfActiveSubscriptions(conn);
                List<LegoSet> updatedSets = await GetSetsToUpdate(sets);

                DbUtils.UpdateWithInfoFromDb(conn, updatedSets);
                Dictionary<int, MailMessage> messages = GetMessagesForUpdatedSets(updatedSets);
                List<Subscription> subscriptions = DbUtils.GetActiveSubscriptions(conn);

                DbUtils.UpdateSetsInDb(conn, updatedSets);
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

        private static Dictionary<int, MailMessage> GetMessagesForUpdatedSets(List<LegoSet> updatedSets)
        {
            var messages = new Dictionary<int, MailMessage>();

            foreach (LegoSet set in updatedSets)
            {
                if (set.LastLowestPrice != null)
                {
                    messages.Add(
                        set.Number,
                        new MailMessage
                        {
                            Plain = GetUpdatePlainMessage(set),
                            Html = GetUpdateHtmlMessage(set),
                            IsBigUpdate = CheckForBigUpdates(set),
                            IsLowestPrice = IsLowestPrice(set),
                            DiffPercent = CalculateDiffPercent(set)
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
                {EventualLowestPriceEverHtml(set)}
                <a href=""{set.Link}"">{set.Link}</a>";

        private static string PriceInfoLine(LegoSet set)
        {
            string verbToUse = set.LowestPrice < set.LastLowestPrice ? "decreased" : "increased";
            string color = set.LowestPrice < set.LastLowestPrice
                ? ColorForPriceDecrease(set)
                : ColorForPriceIncrease(set);

            return @$"
                <p style=""color:{color};"">
                    <strong>Price {verbToUse} from {set.LastLowestPrice:0.00} to {set.LowestPrice:0.00}</strong>
                </p>";
        }

        private static string ColorForPriceDecrease(LegoSet set) =>
            IsBigUpdate(set)
                ? "GreenYellow"
                : "MediumSeaGreen";

        private static string ColorForPriceIncrease(LegoSet set) =>
            IsBigUpdate(set)
                ? "Red"
                : "Crimson";

        private static string EventualLowestPriceEverHtml(LegoSet set) =>
            set.LowestPrice > set.LowestPriceEver
                ? ""
                : @"
                    <p style=""color:yellow;"">
                        <strong>LOWEST PRICE EVER</strong>
                    </p>";

        private static string GetUpdatePlainMessage(LegoSet set)
        {
            string verbToUse = set.LowestPrice > set.LastLowestPrice ? "decreased" : "increased";
            return $@"
                Lego {set.Series} - {set.Number} - {set.Name}\n
                {set.LowestShop}\n
                Price {verbToUse} from {set.LastLowestPrice:0.00} to {set.LowestPrice:0.00}\n
                {EventualLowestPriceEverPlain(set)}
                {set.Link}";
        }

        private static string EventualLowestPriceEverPlain(LegoSet set) =>
            set.LowestPrice > set.LowestPriceEver
                ? ""
                : @"LOWEST PRICE EVER\n";

        private static bool CheckForBigUpdates(LegoSet set)
        {
            if (IsBigUpdate(set))
            {
                set.LastReportedLowestPrice = set.LowestPrice;
                return true;
            }

            return false;
        }

        private static bool IsBigUpdate(LegoSet set) =>
            CalculateDiffPercent(set) > 0.01f;

        private static bool IsLowestPrice(LegoSet set) =>
            set.LowestPrice <= set.LowestPriceEver;

        private static float CalculateDiffPercent(LegoSet set) =>
            (float)((set.LowestPrice - set.LastReportedLowestPrice) / set.LowestPrice);


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
                    List<(int number, bool onlyBig)> setNumbers = subscriptions.Where(s => s.Mail == mail).Select(s => (s.CatalogNumber, s.OnlyBigUpdates)).ToList();

                    List<string> plainSetsMessages = messages
                        .Where(m => setNumbers.Any(s => s.number == m.Key && (!s.onlyBig || m.Value.IsBigUpdate)))
                        .OrderBy(m => m.Value.DiffPercent)
                        .Select(m => m.Value.Plain)
                        .ToList();
                    List<string> htmlSetsMessages = messages
                        .Where(m => setNumbers.Any(s => s.number == m.Key && (!s.onlyBig || m.Value.IsBigUpdate)))
                        .OrderBy(m => m.Value.DiffPercent)
                        .Select(m => m.Value.Html)
                        .ToList();
                    string plainMessage = string.Join('\n', plainSetsMessages);
                    string htmlMessage = string.Join("<br/><hr/>", htmlSetsMessages);

                    if (string.IsNullOrWhiteSpace(plainMessage) || string.IsNullOrWhiteSpace(htmlMessage))
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

        private static List<string> GetPlainMessagesForSpecificSubscriber(List<(int number, bool onlyBig)> setNumbers, Dictionary<int, MailMessage> messages) =>
            messages
                .Where(m => setNumbers.Any(s => s.number == m.Key && (!s.onlyBig || m.Value.IsBigUpdate || m.Value.IsLowestPrice)))
                .OrderBy(m => m.Value.DiffPercent)
                .Select(m => m.Value.Plain)
                .ToList();

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

