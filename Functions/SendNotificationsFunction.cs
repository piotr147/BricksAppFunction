using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using BricksAppFunction.Models;
using BricksAppFunction.Utilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace BricksAppFunction
{
    public static class SendNotificationsFunction
    {
        [FunctionName("SendNotificationsFunction")]
        public async static Task Run([TimerTrigger("0 0 4-23 * * *")] TimerInfo myTimer, ILogger log)

        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");

            using SqlConnection conn = new SqlConnection(str);
            conn.Open();
            List<LegoSet> updatedSets = DbUtils.GetSetsWithEmailToSend(conn);

            List<Subscription> subscriptions = DbUtils.GetActiveSubscriptions(conn);
            Dictionary<(int number, bool isBigUpdate), MailMessage> messages = MessageCreator.GetMessagesForUpdatedSets(updatedSets);

            await EmailSender.SendEmails(subscriptions, messages);
            DbUtils.UpdateSetsAfterSendingEmails(conn, updatedSets);
        }
    }
}
