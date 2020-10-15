namespace BricksAppFunction.Models
{
    public class MailMessage
    {
        public string Html { get; set; }
        public string Plain { get; set; }
        public bool IsBigUpdate { get; set; }
        public bool IsLowestPriceEver { get; set; }
        public float DiffPercent { get; set; }
    }
}
