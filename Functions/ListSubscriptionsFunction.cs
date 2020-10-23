using BricksAppFunction.Models;
using BricksAppFunction.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BricksAppFunction
{
    public static class ListSubscriptionsFunction
    {
        [FunctionName("ListSubscriptionsFunction")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request. Hour: {DateTime.Now.Hour}");

            string mail = req.Query["mail"];

            string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");
            using SqlConnection conn = new SqlConnection(str);
            conn.Open();

            if (!DbUtils.UserExists(conn, mail))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            List<Subscription> subscribedSets = DbUtils.GetActiveSubscriptionsOfUser(conn, mail);
            var json = JsonConvert.SerializeObject(new { subscriptions = subscribedSets }, Formatting.Indented);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
