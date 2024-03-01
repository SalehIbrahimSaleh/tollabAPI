using DataAccess.PaymentModels;
using DataAccess.Services;
using DataAccess.Services.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace TollabAPI.Controllers
{
    public class FIxesController : ApiController
    {
        private readonly StudentService _studentService;
        private readonly MyFatoorahPaymentLogService _myfatoorahService;
        private readonly MyFatoorahPaymentService _paymentService;
        private readonly TransactionService _transactionService;
        public FIxesController()
        {
            _studentService = new StudentService();
            _myfatoorahService = new MyFatoorahPaymentLogService();
            _paymentService = new MyFatoorahPaymentService();
            _transactionService = new TransactionService();
        }

        [HttpGet]
        [Route("api/fix-failed-invoices")]
        public async Task FixInvoicesFromWeb()
        {
            try
            {
                var failedInvoices = await _myfatoorahService.GetAllUnresponded();
                if (failedInvoices.Any())
                {
                    foreach (var inv in failedInvoices.ToList())
                    {
                        var message = await _paymentService.GetPaymentStatus(new GetPaymentStatusRequest
                        {
                            Key = inv.InvoiceId.ToString(),
                            KeyType = "invoiceId"
                        }).ConfigureAwait(false);
                        var response = JsonConvert.DeserializeObject<GenericResponse<GetPaymentStatusResponse>>(message);

                        if (response.IsSuccess)
                        {
                            if (response.Data.InvoiceStatus.ToLower() =="paid")
                            {
                                var computedFiled = response.Data.UserDefinedField.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                var userId = response.Data.CustomerName ?? "";
                                switch (computedFiled[0])
                                {
                                    case string transaction when int.Parse(transaction) == (int)TransactionType.Course:

                                        var courseId = !string.IsNullOrEmpty(response.Data.CustomerReference) ? long.Parse(response.Data.CustomerReference) : 0;

                                        if (!string.IsNullOrEmpty(message))
                                        {
                                            

                                            if (!string.IsNullOrEmpty(userId))
                                            {
                                                var StudentData = await _studentService.GetStudentById(long.Parse(userId));
                                                var resultData = await _transactionService.BuyCourseFromWeb(courseId, StudentData, "", StudentData.CountryId);
                                                if (resultData == 7 || resultData == 1)
                                                {
                                                    if (resultData==7)
                                                    {
                                                        var bankResponse = JsonConvert.DeserializeObject<BankResponse>((JObject.Parse(message)).ToString());

                                                        if (bankResponse != null)
                                                        {

                                                            //var transactionResult = await _transactionService.AddStudentTransaction(2, courseId, bankResponse);
                                                            //if (transactionResult == 0)
                                                            //{
                                                            //    await _myfatoorahService.UpdateInvoice(inv.InvoiceId, true, false, "Invoice Transaction Failed");
                                                            //    break;
                                                            //}


                                                            await _transactionService.LogResponseToDataBase(bankResponse);
                                                        }
                                                    }
                                                    if (response.Data != null)
                                                    {
                                                        await _myfatoorahService.UpdateInvoice(inv.InvoiceId, true, true, response.Data.InvoiceStatus.ToLower());
                                                    }
                                                }
                                            }
                                        }

                                        break;

                                    case string transaction when int.Parse(transaction) == (int)TransactionType.Live:
                                        var liveId = !string.IsNullOrEmpty(response.Data.CustomerReference) ? long.Parse(response.Data.CustomerReference) : 0;
                                        if (!string.IsNullOrEmpty(message))
                                        {
                                            var bankResponse = JsonConvert.DeserializeObject<BankResponse>((JObject.Parse(message)).ToString());
                                            if (bankResponse != null)
                                            {
                                                var transactionResult = await _transactionService.AddStudentTransaction(3, liveId, bankResponse);
                                                if (transactionResult == 0)
                                                {
                                                    await _myfatoorahService.UpdateInvoice(inv.InvoiceId, true, false, "Invoice Transaction Failed");
                                                    break;
                                                }
                                                await _transactionService.LogResponseToDataBase(bankResponse);
                                            }

                                            if (!string.IsNullOrEmpty(userId))
                                            {
                                                var StudentData = await _studentService.GetStudentByIdentityIdAsync(userId);
                                                var resultData = await _transactionService.BuyLiveFromWeb(liveId, StudentData, "", StudentData.CountryId);
                                                if (resultData == 7)
                                                {
                                                    if (response.Data != null)
                                                    {
                                                        await _myfatoorahService.UpdateInvoice(inv.InvoiceId, true, true, response.Data.InvoiceStatus.ToLower());
                                                    }
                                                }
                                            }
                                        }
                                        break;

                                }

                            }


                        }
                        else
                        {
                            if (response.Data != null)
                            {
                                await _myfatoorahService.UpdateInvoice(inv.InvoiceId, true, false, response.Data.InvoiceStatus.ToLower());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }
    }
}