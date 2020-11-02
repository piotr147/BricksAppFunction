using BricksAppFunction.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace BricksAppFunction.Utilities
{
    public static class SetUpdater
    {
        private const int TimeoutMiliseconds = 10000;

        public async static Task UpdateSetsWithNumberBeingReminderOf(int remainder, int divisor)
        {
            string str = Environment.GetEnvironmentVariable("sqldb_connectionstring");

            using SqlConnection conn = new SqlConnection(str);
            conn.Open();
            List<LegoSet> sets = DbUtils.GetSetsOfActiveSubscriptions(conn).Where(s => s.Number % divisor == remainder % divisor).ToList();
            List<LegoSet> updatedSets = await GetSetsToUpdate(sets);
            DbUtils.UpdateSetsWithInfoFromDb(conn, updatedSets);
            DbUtils.UpdateSetsInDb(conn, updatedSets);
        }

        private async static Task<List<LegoSet>> GetSetsToUpdate(List<LegoSet> sets)
        {
            List<LegoSet> updatedSets = new List<LegoSet>();

            foreach (LegoSet set in sets)
            {
                LegoSet updatedSet;
                try
                {
                    updatedSet = await PromoklockiHtmlParser.GetSetInfo(set.Link).TimeoutAfter(TimeoutMiliseconds);
                }
                catch (Exception)
                {
                    continue;
                }

                updatedSet
                    .WithLastLowestPrice(set.LowestPrice)
                    .WithDailyLowestPrice(GetDailyLowestPrice(set, updatedSet));

                if (updatedSet.LowestPrice != set.LowestPrice)
                {
                    updatedSets.Add(updatedSet);
                }
            }

            return updatedSets;
        }

        private static decimal GetDailyLowestPrice(LegoSet set, LegoSet updatedSet)
            => set.DailyLowestPrice <= updatedSet.LowestPrice
                ? set.DailyLowestPrice
                : updatedSet.LowestPrice;
    }
}
