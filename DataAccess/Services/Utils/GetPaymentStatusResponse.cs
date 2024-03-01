using System;

namespace DataAccess.Services.Utils
{
    public class GetPaymentStatusResponse
    {

        public long InvoiceId { get; set; }
        public string InvoiceStatus { get; set; }
        public string InvoiceReference { get; set; }
        public string CustomerReference { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ExpiryDate { get; set; }
        public decimal InvoiceValue { get; set; }
        public string Comments { get; set; }
        public string CustomerName { get; set; }
        public string CustomerMobile { get; set; }
        public string CustomerEmail { get; set; }
        public string UserDefinedField { get; set; }
        public string InvoiceDisplayValue { get; set; }

    }
}
