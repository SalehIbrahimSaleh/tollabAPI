using DataAccess.Entities;
using DataAccess.Repositories;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class PaymentExceptionLogService
    {
        GenericRepository<PaymentExceptionLog> _repository;
        public PaymentExceptionLogService()
        {
            _repository = new GenericRepository<PaymentExceptionLog>();
        }

        public async Task<long> Insert(PaymentExceptionLog log)
        {
            var result = await _repository.Add(log);
            return result;
        }

      
    }
}
