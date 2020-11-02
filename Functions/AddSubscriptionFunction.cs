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
using System.Text.RegularExpressions;
using System.Data;
using System.Diagnostics;
using BricksAppFunction.Utilities;
using BricksAppFunction.Models;

namespace BricksAppFunction
{
    public static class AddSubscriptionFunction
    {
        private static readonly Regex CatalogNumberRegex = new Regex(@"-\d{3,8}-");
        private static readonly Regex PromoklockiUrlRegex = new Regex(@"https://promoklocki\.pl/lego-.*\d{3,8}-.*p\d*");

        [FunctionName("AddSubscription")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string mail = data?.mail;
            string url = data?.url;
            bool onlyBigUpdates = data?.onlyBigUpdates ?? false;

            if (url == null || mail == null || !IsUrlCorrec(url))
            {
                return new BadRequestResult();
            }

            try
            {
                int catalogNumber = GetCatalogNumber(url);
                string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");
                using (SqlConnection conn = new SqlConnection(str))
                {
                    conn.Open();
                    if (!SetIsInDb(conn, catalogNumber))
                    {
                        LegoSet set = await PromoklockiHtmlParser.GetSetInfo(url);
                        await AddNewSet(conn, set);
                    }

                    AddNewSubscription(conn, mail, catalogNumber, onlyBigUpdates);

                }

                stopwatch.Stop();
                return new OkObjectResult($"You have just subscribed Lego {catalogNumber} set. ({stopwatch.ElapsedMilliseconds})");
            }
            catch (Exception e)
            {
                return new BadRequestResult();
            }
        }

        private static bool IsUrlCorrec(string url) => PromoklockiUrlRegex.IsMatch(url);

        private static bool SetIsInDb(SqlConnection conn, int catalogNumber)
        {
            string query = @"select count(*) from Sets where number = @catalogNumber;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@catalogNumber", SqlDbType.Int).Value = catalogNumber;
            return (int)cmd.ExecuteScalar() > 0;
        }

        private static int GetCatalogNumber(string url)
        {
            string extractedCatalogNumberWithTrash = CatalogNumberRegex.Match(url).Value;
            string extractedCatalogNumber = extractedCatalogNumberWithTrash.Remove(extractedCatalogNumberWithTrash.Length - 1, 1).Remove(0, 1);
            if (!int.TryParse(extractedCatalogNumber, out int catalogNumber))
            {
                throw new Exception("Incorrect url");
            }

            return catalogNumber;
        }

        private async static Task AddNewSet(SqlConnection conn, LegoSet set)
        {
            string query = @$"insert into sets values(@number, @name, @series, @url, @lowestPrice, @lowestPriceEver, @lastUpdate, @lowestPrice, 100000, NULL, @lowestPrice, NULL, 0, null);";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@number", SqlDbType.Int).Value = set.Number;
            cmd.Parameters.Add("@name", SqlDbType.VarChar).Value = set.Name;
            cmd.Parameters.Add("@series", SqlDbType.VarChar).Value = set.Series;
            cmd.Parameters.Add("@url", SqlDbType.VarChar).Value = set.Link;
            cmd.Parameters.Add("@lowestPrice", SqlDbType.Float).Value = set.LowestPrice;
            cmd.Parameters.Add("@lowestPriceEver", SqlDbType.Float).Value = set.LowestPriceEver;
            cmd.Parameters.Add("@lastUpdate", SqlDbType.DateTime).Value = DateTime.Now;
            cmd.ExecuteScalar();
        }

        private static void AddNewSubscription(SqlConnection conn, string mail, int catalogNumber, bool onlyBigUpdates)
        {
            if(SubscriptionAlreadyExists(conn, mail, catalogNumber))
            {
                UpdateExistingSubscription(conn, mail, catalogNumber, onlyBigUpdates);
                return;
            }

            int userId = GetUserId(conn, mail);
            string query = @"Insert into subscriptions values(@userId, @catalogNumber, 0, @onlyBigUpdates);";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
            cmd.Parameters.Add("@catalogNumber", SqlDbType.Int).Value = catalogNumber;
            cmd.Parameters.Add("@onlyBigUpdates", SqlDbType.Bit).Value = onlyBigUpdates;
            cmd.ExecuteScalar();
        }

        private static bool SubscriptionAlreadyExists(SqlConnection conn, string mail, int catalogNumber)
        {
            string query = @"
                select count(*) from subscriptions s1
                join subscribers s2 on s2.id = s1.subscriberid
                where s2.mail = @mail and s1.setnumber = @catalogNumber;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@mail", SqlDbType.VarChar).Value = mail;
            cmd.Parameters.Add("@catalogNumber", SqlDbType.Int).Value = catalogNumber;

            return 1 <= (int)cmd.ExecuteScalar();
        }

        private static void UpdateExistingSubscription(SqlConnection conn, string mail, int catalogNumber, bool onlyBigUpdates)
        {
            string query = @"
                update subscriptions
                set isdeleted = 0
                , onlyBigUpdates = @onlyBigUpdates
                from subscriptions s1
                join subscribers s2 on s2.id = s1.subscriberid
                where s2.mail = @mail and s1.setnumber = @catalogNumber;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@mail", SqlDbType.VarChar).Value = mail;
            cmd.Parameters.Add("@catalogNumber", SqlDbType.Int).Value = catalogNumber;
            cmd.Parameters.Add("@onlyBigUpdates", SqlDbType.Int).Value = onlyBigUpdates;
            cmd.ExecuteNonQuery();
        }

        private static int GetUserId(SqlConnection conn, string mail)
        {
            if(!DbUtils.UserExists(conn, mail))
            {
                AddNewUser(conn, mail);
            }

            string query = @"select id from Subscribers where mail = @mail and isdeleted = 0;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@mail", SqlDbType.VarChar, 50).Value = mail;

            return (int)cmd.ExecuteScalar();
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
