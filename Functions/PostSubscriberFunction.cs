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
using System.Diagnostics;
using BricksAppFunction.Utilities;

namespace BricksAppFunction
{
    public static class PostSubscriberFunction
    {
        [FunctionName("PostSubscriberFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            log.LogInformation("C# HTTP trigger function processed a request.");

            string mail = req.Query["mail"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            mail ??= data?.mail;

            string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");

            using SqlConnection conn = new SqlConnection(str);
            conn.Open();

            if (DbUtils.UserExists(conn, mail))
            {
                return new BadRequestResult();
            }

            AddNewUser(conn, mail);

            stopwatch.Stop();
            return new OkObjectResult($"You mail has been added ({stopwatch.ElapsedMilliseconds})");
        }

        private static void AddNewUser(SqlConnection conn, string mail)
        {
            string query = @"insert into Subscribers values(@mail, 0);";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@mail", SqlDbType.VarChar, 50).Value = mail;
            try
            {
                cmd.ExecuteScalar();
            }
            catch (Exception e)
            {
            }
        }
    }
}