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
    }
}
