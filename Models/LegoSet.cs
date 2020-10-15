using System;

namespace BricksAppFunction.Models
{
    public class LegoSet
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Series { get; set; }
        public decimal Price { get; set; }
        public decimal LowestPrice { get; set; }
        public decimal? LastLowestPrice { get; set; }
        public decimal LowestPriceEver { get; set; }
        public decimal LastReportedLowestPrice { get; set; }
        public string Link { get; set; }
        public DateTime LastUpdate { get; set; }
        public decimal DailyLowestPrice { get; set; }
        public bool OnlyBigUpdates { get; set; }
        public string LowestShop { get; set; }

        public LegoSet WithLastReportedLowestPrice(decimal price)
        {
            LastReportedLowestPrice = price;
            return this;
        }
    }
}
