namespace BricksAppFunction
{
    public class Subscription
    {
        public int SubscriberId { get; set; }
        public int SetNumber { get; set; }
        public string Mail { get; set; }
        public decimal LowestPrice { get; set; }
        public decimal? LastLowestPrice { get; set; }
        public decimal LowestPriceEver { get; set; }
    }
}