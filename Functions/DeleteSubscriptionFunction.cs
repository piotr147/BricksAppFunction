using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;

namespace BricksAppFunction
{
    public static class DeleteSubscriptionFunction
    {
        [FunctionName("DeleteSubscriptionFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string mail = req.Query["mail"];
            string catalogNumberString = req.Query["number"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            mail ??= data?.mail;
            catalogNumberString = catalogNumberString ?? data?.number;
            if(!int.TryParse(catalogNumberString, out int catalogNumber))
            {
                return new BadRequestResult();
            }

            string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");
            using SqlConnection conn = new SqlConnection(str);
            conn.Open();
            DeleteSubscriptionIfExists(conn, mail, catalogNumber);

            return new OkObjectResult("Subscription has been deleted");
        }

        private static void DeleteSubscriptionIfExists(SqlConnection conn, string mail, int catalogNumber)
        {
            string query = @"
                update subscriptions
                set isdeleted = 1
                from subscriptions s1
                join subscribers s2 on s2.id = s1.subscriberid
                where s2.mail = @mail and s1.setnumber = @catalogNumber;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@mail", SqlDbType.VarChar).Value = mail;
            cmd.Parameters.Add("@catalogNumber", SqlDbType.Int).Value = catalogNumber;
            cmd.ExecuteNonQuery();
        }
    }
}
