using System;
using System.Collections.Generic;
using System.Text;

namespace BricksAppFunction
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
        public string Link { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
