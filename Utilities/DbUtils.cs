using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace BricksAppFunction.Utilities
{
    public static class DbUtils
    {
        public static bool UserExists(SqlConnection conn, string mail)
        {
            string query = @"select count(*) from Subscribers where mail = @mail and isdeleted = 0;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@mail", SqlDbType.VarChar, 50).Value = mail;
            return (int)cmd.ExecuteScalar() > 0;
        }

        public static void ArchiveSets()
        {
            string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");

            using SqlConnection conn = new SqlConnection(str);
            conn.Open();

            //TODO: archive lowest shop
            string query = @"
                Insert into SetsArchive
                select number, getdate(), dailyLowestPrice, NULL
                from Sets;
                Update Sets
                set dailyLowestPrice = 100000;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        public static List<LegoSet> GetSetsOfActiveSubscriptions(SqlConnection conn)
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

        public static void UpdateWithInfoFromDb(SqlConnection conn, List<LegoSet> updatedSets)
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

        public static void UpdateSetsInDb(SqlConnection conn, List<LegoSet> updatedSets)
        {
            UpdatePricesAndShops(conn, updatedSets);
            UpdateLastUpdates(conn);
            UpdateDailyLowests(conn);
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

        private static void UpdateLastUpdates(SqlConnection conn)
        {
            string query = $@"
                update Sets set lastUpdate = @lastUpdate;";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@lastUpdate", SqlDbType.DateTime).Value = DateTime.Now;
            cmd.ExecuteNonQuery();
        }

        private static void UpdateDailyLowests(SqlConnection conn)
        {
            string query = $@"
                update Sets set dailyLowestPrice = lowestPrice, dailyLowestShop = lowestShop where lowestPrice < dailyLowestPrice";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.ExecuteNonQuery();
        }

        public static List<Subscription> GetActiveSubscriptions(SqlConnection conn)
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
                        OnlyBigUpdates = reader.GetBoolean(2)
                    });
            }

            return subscriptions;
        }

        public static List<Subscription> GetActiveSubscriptionsOfUser(SqlConnection conn, string mail)
        {
            List<Subscription> subscription = new List<Subscription>();

            string query = @"
                Select s2.mail, st.number, st.name, st.series, s1.onlybigupdates
                from sets st
                join subscriptions s1 on s1.setnumber = st.number
                join subscribers s2 on s2.id = s1.subscriberid
                where s2.mail = @mail and s1.isdeleted = 0";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@mail", SqlDbType.VarChar).Value = mail;

            return ReadSubscriptionsOfUser(cmd.ExecuteReader());
        }

        private static List<Subscription> ReadSubscriptionsOfUser(SqlDataReader reader)
        {
            List<Subscription> sets = new List<Subscription>();

            using (reader)
            {
                while (reader.Read())
                {
                    sets.Add(new Subscription
                    {
                        Mail = reader.GetString(0),
                        CatalogNumber = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        Series = reader.GetString(3),
                        OnlyBigUpdates = reader.GetBoolean(4)
                    });
                }
            }

            return sets;
        }
    }
}
