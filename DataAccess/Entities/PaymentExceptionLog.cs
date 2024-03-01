using Dapper.Contrib.Extensions;
using System;

namespace DataAccess.Entities
{
    [Table("dbo.PaymentExceptionLog")]
    public class PaymentExceptionLog
    {
        public long Id { get; set; }
        public DateTime CreatedTime { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExcptionTrace { get; set; }
        public string UserId { get; set; }
        public string Comment { get; set; }
        public string EndPoint { get; set; }


    }



}
