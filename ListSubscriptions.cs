using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BricksAppFunction
{
    public static class ListSubscriptions
    {
        [FunctionName("ListSubscriptions")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string mail = req.Query["mail"];

            string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");
            using SqlConnection conn = new SqlConnection(str);
            conn.Open();

            if (!DbUtils.UserExists(conn, mail))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            List<string> subscribedSets = GetUsersSets(conn, mail);
            var json = JsonConvert.SerializeObject(new { sets = subscribedSets }, Formatting.Indented);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private static List<string> GetUsersSets(SqlConnection conn, string mail)
        {
            List<string> sets = new List<string>();

            string query = @"
                Select st.number, st.name, st.series
                from sets st
                join subscriptions s1 on s1.setnumber = st.number
                join subscribers s2 on s2.id = s1.subscriberid
                where s2.mail = @mail";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@mail", SqlDbType.VarChar).Value = mail;

            return ReadSets(cmd.ExecuteReader());
        }

        private static List<string> ReadSets(SqlDataReader reader)
        {
            List<string> sets = new List<string>();

            using (reader)
            {
                while(reader.Read())
                {
                    int number = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    string series = reader.GetString(2);
                    sets.Add($"{number} - {series} - {name}");
                }
            }

            return sets;
        }
    }
}
