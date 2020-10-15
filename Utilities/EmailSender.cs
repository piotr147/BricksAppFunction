using BricksAppFunction.Models;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BricksAppFunction.Utilities
{
    public static class EmailSender
    {
        private static readonly SendGridClient _client = new SendGridClient(Environment.GetEnvironmentVariable("sendgrid_key"));

        public async static Task SendEmails(List<Subscription> subscriptions, Dictionary<int, MailMessage> messages)
        {
            string subject = $"Lego update {DateTime.Now.Day:D2}.{DateTime.Now.Month:D2}";
            List<string> mails = subscriptions.Select(s => s.Mail).Distinct().ToList();
            var senderMail = new EmailAddress("piotr.koskiewicz@gmail.com", "Piotr");

            foreach (string mail in mails)
            {
                try
                {
                    List<(int number, bool onlyBig)> setNumbers = subscriptions.Where(s => s.Mail == mail).Select(s => (s.CatalogNumber, s.OnlyBigUpdates)).ToList();

                    List<string> plainSetsMessages = GetPlainMessagesForSpecificSubscriber(setNumbers, messages);
                    List<string> htmlSetsMessages = GetHtmlMessagesForSpecificSubscriber(setNumbers, messages);
                    string plainMessage = string.Join('\n', plainSetsMessages);
                    string htmlMessage = string.Join("<br/><hr/>", htmlSetsMessages);

                    if (string.IsNullOrWhiteSpace(plainMessage) || string.IsNullOrWhiteSpace(htmlMessage))
                    {
                        continue;
                    }

                    var receiverMail = new EmailAddress(mail, "Brick buddy");
                    SendGridMessage msg = MailHelper.CreateSingleEmail(senderMail, receiverMail, subject, plainMessage, htmlMessage);
                    var result = await _client.SendEmailAsync(msg);
                }
                catch (Exception)
                {
                }
            }
        }

        private static List<string> GetPlainMessagesForSpecificSubscriber(List<(int number, bool onlyBig)> setNumbers, Dictionary<int, MailMessage> messages) =>
            messages
                .Where(m => setNumbers.Any(s => s.number == m.Key && (!s.onlyBig || m.Value.IsBigUpdate || m.Value.IsLowestPriceEver)))
                .OrderBy(m => m.Value.DiffPercent)
                .Select(m => m.Value.Plain)
                .ToList();

        private static List<string> GetHtmlMessagesForSpecificSubscriber(List<(int number, bool onlyBig)> setNumbers, Dictionary<int, MailMessage> messages) =>
            messages
                .Where(m => setNumbers.Any(s => s.number == m.Key && (!s.onlyBig || m.Value.IsBigUpdate || m.Value.IsLowestPriceEver)))
                .OrderBy(m => m.Value.DiffPercent)
                .Select(m => m.Value.Html)
                .ToList();
    }
}
