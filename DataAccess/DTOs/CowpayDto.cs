using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.DTOs
{
    public class CowpayDto
    {
             public string merchant_reference_id { get; set; }
            public string customer_merchant_profile_id { get; set; }
            public string customer_name { get; set; }
            public string customer_email { get; set; }
            public string customer_mobile { get; set; }
            public string amount { get; set; }
            public string signature { get; set; }
            public string description { get; set; }
        
    }
}
