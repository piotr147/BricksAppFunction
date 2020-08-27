using System;
using System.Collections.Generic;
using System.Text;

namespace BricksAppFunction
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
