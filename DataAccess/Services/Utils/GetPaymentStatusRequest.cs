namespace DataAccess.Services.Utils
{
    public class GetPaymentStatusRequest
    {
         
        public string Key { get; set; }
        
        public string KeyType { get; set; } // InvoiceId  PaymentId
    }
    public enum TransactionType
    {
        Track = 1,
        Course,
        Live
    }
}
