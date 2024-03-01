using DataAccess.Entities;
using DataAccess.DTOs;
using DataAccess.PaymentModels;
using DataAccess.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;
using TollabAPI.Utils;

namespace TollabAPI.Controllers
{
    [Authorize]
    public class TransactionsController : ApiController
    {
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        private CustomeResponseMessage response = new CustomeResponseMessage();
        StudentService _studentService;
        TransactionService _transactionService;
        MyFatoorahPaymentLogService _myFatoorahPaymentLogService;
        PaymentExceptionLogService _PaymentExceptionLogService;
        public TransactionsController()
        {
            _studentService = new StudentService();
            _transactionService = new TransactionService();
            _myFatoorahPaymentLogService = new MyFatoorahPaymentLogService();
            _PaymentExceptionLogService = new PaymentExceptionLogService();
        }



        [HttpGet]
        [Route("api/CheckPromoCode")]
        public async Task<HttpResponseMessage> CheckPromoCode(string PromocodeText)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var PromoCodeData = await _transactionService.CheckPromoCode(StudentData.Id, PromocodeText);
                if (PromoCodeData == null)
                {
                    response.clearBody();
                    response.AddModel(AppConstants.User, new { });
                    response.AddError(AppConstants.Message, "Invalid PromoCode");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }
                response.AddModel(AppConstants.User, PromoCodeData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return response.getResponseMessage(HttpStatusCode.OK);

            }
            catch (Exception e)
            {

                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }


        [HttpGet]
        [Route("api/BuyNow")]
        public async Task<HttpResponseMessage> BuyNow(long CourseId, string PromocodeText = "")
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (CourseId < 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.BuyNowV2(CourseId, StudentData, PromocodeText, StudentData.CountryId);
                if (resultData == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Course Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                if (resultData == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 2 || resultData == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 5)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Course By Track Subscription");
                    response.AddError(AppConstants.Code, AppConstants.This_Course_By_Track_Subscription);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                if (resultData == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                if (resultData == 10)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Operation_Not_Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {

                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [HttpGet]
        [Route("api/BuyNow")]
        public async Task<HttpResponseMessage> BuyLiveNow(long liveId, string PromocodeText = "")
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (liveId < 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.BuyLive(liveId, StudentData, PromocodeText, StudentData.CountryId);
                if (resultData == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid live Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                if (resultData == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 2 || resultData == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                //if (resultData == 5)
                //{
                //    response.clearBody();
                //    response.AddError(AppConstants.Message, "This Course By Track Subscription");
                //    response.AddError(AppConstants.Code, AppConstants.This_Course_By_Track_Subscription);
                //    return response.getResponseMessage(HttpStatusCode.BadRequest);
                //}
                if (resultData == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                if (resultData == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                if (resultData == 10)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Operation_Not_Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {

                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [HttpGet]
        [Route("api/BuyCourseByApplePurchasing")]
        public async Task<HttpResponseMessage> BuyCourseByApplePurchasing(long CourseId)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (CourseId < 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.BuyCourseByApplePurchasing(CourseId, StudentData);
                if (resultData == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Course Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                if (resultData == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 2 || resultData == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 5)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Course By Track Subscription");
                    response.AddError(AppConstants.Code, AppConstants.This_Course_By_Track_Subscription);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                if (resultData == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [HttpGet]
        [Route("api/BuyLiveByApplePurchasing")]
        public async Task<HttpResponseMessage> BuyLiveByApplePurchasing(long liveId)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (liveId < 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.BuyLiveByApplePurchasing(liveId, StudentData);
                if (resultData == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Course Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                if (resultData == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 2 || resultData == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 5)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Course By Track Subscription");
                    response.AddError(AppConstants.Code, AppConstants.This_Course_By_Track_Subscription);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                if (resultData == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [HttpGet]
        [Route("api/TrackSubscription")]
        public async Task<HttpResponseMessage> TrackSubscription(long TrackId, string PromocodeText = "", bool isPayFromWallet = false)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (TrackId < 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.TrackSubscription(TrackId, StudentData.Id, PromocodeText, StudentData.CountryId,isPayFromWallet);
                if (resultData == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Course Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                if (resultData == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 2 || resultData == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 5)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Track By Course Subscription");
                    response.AddError(AppConstants.Code, AppConstants.This_Track_By_Course_Subscription);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price or duration");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price_Or_Duration);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [HttpGet]
        [Route("api/TrackSubscriptionByApplePurchasing")]
        public async Task<HttpResponseMessage> TrackSubscriptionByApplePurchasing(long TrackId)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (TrackId < 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.TrackSubscriptionByApplePurchasing(TrackId, StudentData.Id, StudentData.CountryId);
                if (resultData == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Course Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                if (resultData == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 2 || resultData == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 5)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Track By Course Subscription");
                    response.AddError(AppConstants.Code, AppConstants.This_Track_By_Course_Subscription);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price or duration");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price_Or_Duration);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [HttpGet]
        [Route("api/GetAllStudentTransactions")]
        public async Task<HttpResponseMessage> GetAllStudentTransactions(int Page = 0)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.GetTransactions(StudentData.Id, Page);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_Found);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return response.getResponseMessage(HttpStatusCode.OK);



            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }


        [AllowAnonymous]
        [HttpGet]
        [Route("api/OnPaymentSuccess")]
        public async Task<HttpResponseMessage> OnPaymentSuccess(string paymentId, int IsInvoice = 0)
        {
            try
            {
                BankResponse bankResponse = null;


                if (string.IsNullOrEmpty(paymentId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Result Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var resultData = await _transactionService.PaymentSuccess(paymentId);

                string uri = "https://api.myfatoorah.com/v2/GetPaymentStatus";
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", "nZ6ZCaIe1JbhVsbEdIPEISgty9KpEZtD-ojVm3GIo2PoE1EWSa-HibbYSwegi8sXUjt5vX_V36YVHPJmOE-8RrXcV7DPuOBE7bIfxbo4p_mu5c-gSqvhtbTaLaJ5hWKbCtAMHN01BGwci1Qax9sZrsN27ZDU_Lthj6toxVvlQOCVGPtAwfIG3XajawHT79f9tHwixTBpGDZnTXwRoCP6teiLiryyPMjasqugUBJpp6oOBlrW1d4HLV73h5tDJbLEeeLo_532c3ONCHLrzTnMCFZe3d8ZE5AgEYw_jf2pun1owXUCG-zlBOyyjCEp46VJ_-2wGDtG2aKWE4ooTtCFYGKQ_lbT6t4lCLvj_yCdi43khIwclP-Ug2CnMurld_9PiIfLWRN3F0MXepN7WLJBlR0Z5BUZPXguQGJ6DtqJx6Vmn598BFMTJyz43aT_lSqA7wG20-xh_e9jNBoEMYEDov4Zsw-nPJANQDzJVmxlaT76_VaRWyz8B3p3rtOmlP2zgwQjKqosq0iblbdIWWheBKJjmjl4Isq4cbrrm1pqevl31ykkWVd3gThaUXiLkUJNieGVv1VUoZlO3A53zHj-ievz_7vTlL5Kr34GrtjgFZ03jD1fJsPuYmX35Fj2aPXr6yiwwhHL85Gr3gvUFDQJ2VunMFmz1eBEC-JsKDajH4BwxPrv");

                String KeyTypeValue = "";

                if (IsInvoice == 0)
                    KeyTypeValue = paymentId.Length > 10 ? "PaymentId" : "InvoiceId";
                else
                    KeyTypeValue = "InvoiceId";



                var BodyModel = new { Key = paymentId, KeyType = KeyTypeValue };

                var response2 = await client.PostAsJsonAsync(uri, BodyModel);
                var responseString = await response2.Content.ReadAsStringAsync();
                var tempResponse = JObject.Parse(responseString);
                responseString = tempResponse.ToString();
                var responseCode = response2.StatusCode;

                var BankResponseModel = JsonConvert.DeserializeObject<BankResponse>((JObject.Parse(responseString)).ToString());
                if (BankResponseModel != null)
                {
                    bankResponse = await _transactionService.LogResponseToDataBase(BankResponseModel);
                }
                if (bankResponse == null || bankResponse.IsSuccess == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var responseCustome = Request.CreateResponse(HttpStatusCode.Moved);
                responseCustome.Headers.Location = new Uri("https://tollab.azurewebsites.net/PayMethods/Success?paymentId=" + paymentId + "");
                response.AddModel(AppConstants.User, "https://tollab.azurewebsites.net/PayMethods/Success?paymentId=" + paymentId + "");
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return responseCustome;//response.getResponseMessage(HttpStatusCode.OK);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [AllowAnonymous]
        [HttpGet]
        [Route("api/GetFromBank")]
        public async Task<HttpResponseMessage> GetFromBank()
        {
            try
            {
                BankResponse BankResponse = null;
                var resultData = await _transactionService.GetALLPendingOperations();
                foreach (var item in resultData)
                {
                    string uri = "https://apitest.myfatoorah.com/v2/GetPaymentStatus";
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization
                    = new AuthenticationHeaderValue("Bearer", "7Fs7eBv21F5xAocdPvvJ-sCqEyNHq4cygJrQUFvFiWEexBUPs4AkeLQxH4pzsUrY3Rays7GVA6SojFCz2DMLXSJVqk8NG-plK-cZJetwWjgwLPub_9tQQohWLgJ0q2invJ5C5Imt2ket_-JAlBYLLcnqp_WmOfZkBEWuURsBVirpNQecvpedgeCx4VaFae4qWDI_uKRV1829KCBEH84u6LYUxh8W_BYqkzXJYt99OlHTXHegd91PLT-tawBwuIly46nwbAs5Nt7HFOozxkyPp8BW9URlQW1fE4R_40BXzEuVkzK3WAOdpR92IkV94K_rDZCPltGSvWXtqJbnCpUB6iUIn1V-Ki15FAwh_nsfSmt_NQZ3rQuvyQ9B3yLCQ1ZO_MGSYDYVO26dyXbElspKxQwuNRot9hi3FIbXylV3iN40-nCPH4YQzKjo5p_fuaKhvRh7H8oFjRXtPtLQQUIDxk-jMbOp7gXIsdz02DrCfQIihT4evZuWA6YShl6g8fnAqCy8qRBf_eLDnA9w-nBh4Bq53b1kdhnExz0CMyUjQ43UO3uhMkBomJTXbmfAAHP8dZZao6W8a34OktNQmPTbOHXrtxf6DS-oKOu3l79uX_ihbL8ELT40VjIW3MJeZ_-auCPOjpE3Ax4dzUkSDLCljitmzMagH2X8jN8-AYLl46KcfkBV");

                    var BodyModel = new { Key = item.PaymentId, KeyType = "PaymentId" };
                    var response = await client.PostAsJsonAsync(uri, BodyModel);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var tempResponse = JObject.Parse(responseString);
                    responseString = tempResponse.ToString();
                    var responseCode = response.StatusCode;

                    BankResponse = JsonConvert.DeserializeObject<BankResponse>((JObject.Parse(responseString)).ToString());
                    if (BankResponse != null)
                    {

                        await _transactionService.LogResponseToDataBase(BankResponse);
                    }


                }
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_Found);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }
                response.AddModel(AppConstants.User, BankResponse);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return response.getResponseMessage(HttpStatusCode.OK);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }


        [AllowAnonymous]
        [HttpGet]
        [Route("api/OnPaymentError")]
        public async Task<HttpResponseMessage> OnPaymentError(string paymentId)
        {
            try
            {
                BankResponse bankResponse = null;


                if (string.IsNullOrEmpty(paymentId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Result Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }
                var resultData = await _transactionService.PaymentSuccess(paymentId);

                string uri = "https://api.myfatoorah.com/v2/GetPaymentStatus";
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", "nZ6ZCaIe1JbhVsbEdIPEISgty9KpEZtD-ojVm3GIo2PoE1EWSa-HibbYSwegi8sXUjt5vX_V36YVHPJmOE-8RrXcV7DPuOBE7bIfxbo4p_mu5c-gSqvhtbTaLaJ5hWKbCtAMHN01BGwci1Qax9sZrsN27ZDU_Lthj6toxVvlQOCVGPtAwfIG3XajawHT79f9tHwixTBpGDZnTXwRoCP6teiLiryyPMjasqugUBJpp6oOBlrW1d4HLV73h5tDJbLEeeLo_532c3ONCHLrzTnMCFZe3d8ZE5AgEYw_jf2pun1owXUCG-zlBOyyjCEp46VJ_-2wGDtG2aKWE4ooTtCFYGKQ_lbT6t4lCLvj_yCdi43khIwclP-Ug2CnMurld_9PiIfLWRN3F0MXepN7WLJBlR0Z5BUZPXguQGJ6DtqJx6Vmn598BFMTJyz43aT_lSqA7wG20-xh_e9jNBoEMYEDov4Zsw-nPJANQDzJVmxlaT76_VaRWyz8B3p3rtOmlP2zgwQjKqosq0iblbdIWWheBKJjmjl4Isq4cbrrm1pqevl31ykkWVd3gThaUXiLkUJNieGVv1VUoZlO3A53zHj-ievz_7vTlL5Kr34GrtjgFZ03jD1fJsPuYmX35Fj2aPXr6yiwwhHL85Gr3gvUFDQJ2VunMFmz1eBEC-JsKDajH4BwxPrv");

                var BodyModel = new { Key = paymentId, KeyType = "PaymentId" };
                var response2 = await client.PostAsJsonAsync(uri, BodyModel);
                var responseString = await response2.Content.ReadAsStringAsync();
                var tempResponse = JObject.Parse(responseString);
                responseString = tempResponse.ToString();
                var responseCode = response2.StatusCode;

                var BankResponseModel = JsonConvert.DeserializeObject<BankResponse>((JObject.Parse(responseString)).ToString());
                if (BankResponseModel != null)
                {
                    bankResponse = await _transactionService.LogResponseToDataBase(BankResponseModel);
                }
                if (bankResponse == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_Found);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }
                var responseCustome = Request.CreateResponse(HttpStatusCode.Moved);
                responseCustome.Headers.Location = new Uri("http://tollab.azurewebsites.net/PayMethods/Error");

                response.AddModel(AppConstants.User, "http://tollab.azurewebsites.net/PayMethods/Error");
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return responseCustome; //response.getResponseMessage(HttpStatusCode.OK);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
        }






        [HttpPost]
        [Route("api/GenenrateCowpayCode")]
        public async Task<CowPayLog> GenenrateCowpayCode(CowpayDto cowPayLog)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return null;
                }
                string uri = "https://cowpay.me/api/v2/charge/fawry";
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization= new AuthenticationHeaderValue("Bearer", "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJhdWQiOiIzIiwianRpIjoiNDk5ZTVmZGRmZjFlYTIxNDNjMTk1ZDllNTZjYWZjMWRjNWQ5MThjODc1ODc4NTAzMDIwYjhlYzk4ZWM1NjQ3OGYzZmRjYjRmNWYyNTEzMTMiLCJpYXQiOjE2NjQ0MDM2NjkuMjU1MzA1LCJuYmYiOjE2NjQ0MDM2NjkuMjU1MzA5LCJleHAiOjQ4MjAwNzcyNjkuMjQ4NDEsInN1YiI6IjM3NiIsInNjb3BlcyI6W119.fNmJTyKlohBFs6ufjpdpxbkcLpllHjFHN4NL_RATFJ_BJNne8xNNPy7Vx81Mrvdbd3vMAh6TdrSOIdJf2xbOLg");
                
                var cowPayCallback = await client.PostAsJsonAsync(uri, cowPayLog);
                var responseString = await cowPayCallback.Content.ReadAsStringAsync();
                var tempResponse = JObject.Parse(responseString);
                 //var FawryResponseModel = JsonConvert.DeserializeObject<CowPayLog>((JObject.Parse(responseString)).ToString());
                 var paymentModel = new JavaScriptSerializer().Deserialize<CowPayLog>(responseString);

                paymentModel.CreationDate = DateTime.UtcNow;
                await _transactionService.addCowPayLog(paymentModel);
                

                return paymentModel;
            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/CowPayCallBack")]
        public async Task<HttpResponseMessage> CowPayCallBack()
        {
            try
            {
                var rawMessage = await Request.Content.ReadAsStringAsync();
                var paymentModel = new JavaScriptSerializer().Deserialize<CowPayLog>(rawMessage);

                paymentModel.CreationDate = DateTime.UtcNow;
                await _transactionService.addCowPayLog(paymentModel);
                response.AddModel(AppConstants.User, true);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return response.getResponseMessage(HttpStatusCode.OK);

            }
            catch (Exception e)
            {
                CowPayLog cowPayLog = new CowPayLog()
                {
                    signature = e.ToString(),
                    CreationDate = DateTime.UtcNow

                };
                await _transactionService.addCowPayLog(cowPayLog);
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
        }


        //buy course or track by dashboard link in pay page 

        [HttpGet]
        [AllowAnonymous]
        [Route("api/BuyCourseByLink")]
        public async Task<HttpResponseMessage> BuyCourseByLink(string PaymentKey, string CourseCode, string PromocodeText)
        {
            try
            {

                if (string.IsNullOrEmpty(PaymentKey) || string.IsNullOrEmpty(CourseCode))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var StudentData = await _studentService.GetStudentByPaymentKey(PaymentKey);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.BuyNowByCourseCode(CourseCode, StudentData, PromocodeText);
                if (resultData == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Course Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                if (resultData == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 2 || resultData == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 5)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Course By Track Subscription");
                    response.AddError(AppConstants.Code, AppConstants.This_Course_By_Track_Subscription);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                if (resultData == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [HttpGet]
        [AllowAnonymous]
        [Route("api/TrackSubscriptionByLink")]
        public async Task<HttpResponseMessage> TrackSubscriptionByLink(string PaymentKey, string TrackCode, string PromocodeText)
        {
            try
            {

                if (string.IsNullOrEmpty(PaymentKey) || string.IsNullOrEmpty(TrackCode))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var StudentData = await _studentService.GetStudentByPaymentKey(PaymentKey);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _transactionService.TrackSubscriptionByTrackCode(TrackCode, StudentData.Id, PromocodeText, StudentData.CountryId);
                if (resultData == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Course Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                if (resultData == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 2 || resultData == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 5)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Track By Course Subscription");
                    response.AddError(AppConstants.Code, AppConstants.This_Track_By_Course_Subscription);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price or duration");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price_Or_Duration);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }
        //-----------------------------------------------------//

        public async Task CheckPaymentIds()
        {
            try
            {

                var PaymentIds = await _transactionService.CheckPaymentIds();
                foreach (var paymentId in PaymentIds)
                {

                    BankResponse bankResponse = null;
                    var resultData = await _transactionService.PaymentSuccess(paymentId);
                    string uri = "https://api.myfatoorah.com/v2/GetPaymentStatus";
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization
                    = new AuthenticationHeaderValue("Bearer", "nZ6ZCaIe1JbhVsbEdIPEISgty9KpEZtD-ojVm3GIo2PoE1EWSa-HibbYSwegi8sXUjt5vX_V36YVHPJmOE-8RrXcV7DPuOBE7bIfxbo4p_mu5c-gSqvhtbTaLaJ5hWKbCtAMHN01BGwci1Qax9sZrsN27ZDU_Lthj6toxVvlQOCVGPtAwfIG3XajawHT79f9tHwixTBpGDZnTXwRoCP6teiLiryyPMjasqugUBJpp6oOBlrW1d4HLV73h5tDJbLEeeLo_532c3ONCHLrzTnMCFZe3d8ZE5AgEYw_jf2pun1owXUCG-zlBOyyjCEp46VJ_-2wGDtG2aKWE4ooTtCFYGKQ_lbT6t4lCLvj_yCdi43khIwclP-Ug2CnMurld_9PiIfLWRN3F0MXepN7WLJBlR0Z5BUZPXguQGJ6DtqJx6Vmn598BFMTJyz43aT_lSqA7wG20-xh_e9jNBoEMYEDov4Zsw-nPJANQDzJVmxlaT76_VaRWyz8B3p3rtOmlP2zgwQjKqosq0iblbdIWWheBKJjmjl4Isq4cbrrm1pqevl31ykkWVd3gThaUXiLkUJNieGVv1VUoZlO3A53zHj-ievz_7vTlL5Kr34GrtjgFZ03jD1fJsPuYmX35Fj2aPXr6yiwwhHL85Gr3gvUFDQJ2VunMFmz1eBEC-JsKDajH4BwxPrv");

                    var BodyModel = new { Key = paymentId, KeyType = "PaymentId" };
                    var response2 = await client.PostAsJsonAsync(uri, BodyModel);

                    var responseString = await response2.Content.ReadAsStringAsync();
                    var tempResponse = JObject.Parse(responseString);

                    responseString = tempResponse.ToString();
                    var responseCode = response2.StatusCode;
                    var BankResponseModel = JsonConvert.DeserializeObject<BankResponse>((JObject.Parse(responseString)).ToString());

                    if (BankResponseModel != null)
                    {
                        bankResponse = await _transactionService.LogResponseToDataBase(BankResponseModel);
                    }

                }
            }
            catch (Exception e)
            {
            }


        }





        #region Added Work
        [HttpGet]
        [Route("api/WalletSubscription")]
        public async Task<HttpResponseMessage> WalletSubscription(string message, string PromocodeText = "", string userId = "", bool isWebHook = false)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var bankResponse = JsonConvert.DeserializeObject<BankResponse>((JObject.Parse(message)).ToString());
                    if (bankResponse != null)
                    {
                        var transaction = await _transactionService.AddStudentTransaction(4,null , bankResponse);
                        if (transaction == 0)
                        {
                            response.clearBody();
                            response.AddError(AppConstants.Message, "Transaction Failed");
                            response.AddError(AppConstants.Code, AppConstants.Error);
                            return response.getResponseMessage(HttpStatusCode.BadRequest);
                        }
                        var logTransaction = await _transactionService.LogResponseToDataBase(bankResponse);
                    }

                }

                string IdentityUserId = !isWebHook ? User.Identity.GetUserId() : userId;
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                 
                var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                if (StudentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                 
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
            catch (Exception e)
            {

                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }
        [HttpGet]
        [Route("api/buy-course-from-web")]
        public async Task<HttpResponseMessage> BuyCourseFromWeb(string message, long CourseId, string PromocodeText = "",string userId = "", bool isWebHook = false, bool isPayFromWallet = false)
        {
            try
            {
            

                 string IdentityUserId = User.Identity.GetUserId();
               
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var student = await _studentService.GetStudentByIdentityIdAsync(User.Identity.GetUserId());
               
                if (CourseId < 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                
                if (student == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var result = await _transactionService.BuyCourseFromWeb(CourseId, student, PromocodeText, student.CountryId,isPayFromWallet);
                if (result == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Course Id");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                if (result == 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You are enrolled Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (result == 2 || result == 8)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This PromoCode InValid");
                    response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (result == 3)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (result == 4)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                    response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (result == 5)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Course By Track Subscription");
                    response.AddError(AppConstants.Code, AppConstants.This_Course_By_Track_Subscription);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (result == 6)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "not valid price");
                    response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                if (result == 7)
                {
                    response.AddModel(AppConstants.User, true);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Returned Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);

                }
                if (result == 10)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Operation_Not_Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }



                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);

            }
            catch (Exception e)
            {

                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }

        [HttpGet]
        [Route("api/buy-live-from-web")]
        public async Task<HttpResponseMessage> BuyLiveFromWeb(string message, long liveId, string PromocodeText = "" ,string userId = "", bool isWebHook = false, bool isPayFromWallet = false)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    var bankResponse = JsonConvert.DeserializeObject<BankResponse>((JObject.Parse(message)).ToString());
                    if (bankResponse != null)
                    {
                        var transaction = await _transactionService.AddStudentTransaction(3, liveId, bankResponse);
                        if (transaction == 0)
                        {
                            response.clearBody();
                            response.AddError(AppConstants.Message, "Transaction Failed");
                            response.AddError(AppConstants.Code, AppConstants.Error);
                            return response.getResponseMessage(HttpStatusCode.BadRequest);
                        }
                        var logTransaction = await _transactionService.LogResponseToDataBase(bankResponse);
                    }

                }

                string IdentityUserId = !isWebHook ? User.Identity.GetUserId() : userId;
                if (string.IsNullOrEmpty(IdentityUserId))
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Student not found");
                        response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }
                    if (liveId < 1)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Invalid Parametrs");
                        response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }
                    var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                    if (StudentData == null)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Student not found");
                        response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }

                    var resultData = await _transactionService.BuyLive(liveId, StudentData, PromocodeText, StudentData.CountryId,isPayFromWallet);
                    if (resultData == 0)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Invalid live Id");
                        response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Course_Id);
                        return response.getResponseMessage(HttpStatusCode.OK);
                    }

                    if (resultData == 1)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "You are enrolled Before");
                        response.AddError(AppConstants.Code, AppConstants.Result_Enrolled_Before);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }
                    if (resultData == 2 || resultData == 8)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "This PromoCode InValid");
                        response.AddError(AppConstants.Code, AppConstants.Result_This_PromoCode_InValid);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }
                    if (resultData == 3)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "You Are Used This PromoCode Before");
                        response.AddError(AppConstants.Code, AppConstants.Result_You_Are_Used_This_PromoCode_Before);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }
                    if (resultData == 4)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "You Need Charge Your Wallet");
                        response.AddError(AppConstants.Code, AppConstants.Result_You_Need_Charge_Your_Wallet);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }

                    if (resultData == 6)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "not valid price");
                        response.AddError(AppConstants.Code, AppConstants.Not_Valid_Price);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }
                    if (resultData == 7)
                    {
                        response.AddModel(AppConstants.User, true);
                        response.AddMeta(AppConstants.Result, AppConstants.Success);
                        response.AddMeta(AppConstants.Message, "Returned Successfuly");
                        return response.getResponseMessage(HttpStatusCode.OK);

                    }
                    if (resultData == 10)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Operation_Not_Completed");
                        response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }




                
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
            catch (Exception e)
            {

                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }


        }


        [HttpGet]
        [Route("api/insert-invoice-log")]
        public async Task<long> InsertMyFatoorahInvoiceLog(long invoiceId)
        {
            try
            {
                var result = await _myFatoorahPaymentLogService.InsertInvoice(invoiceId);
                return result;
            } catch (Exception ex)
            {
                return 0;
            }


        }

        [HttpGet]
        [Route("api/insert-error")]
        public async Task<long> InsertError( string id,string error)
        {
            try
            {
                LogError logError = new LogError()
                {
                    ErrorMessage = error,
                    ErrorCode = id,
                };
             return  await _studentService.AddLog(logError);
            }
            catch (Exception ex)
            {
                return 0;
            }


        }
        [HttpGet]
        [Route("api/update-invoice-log")]
        public async Task<bool> UpdateMyFatoorahInvoiceLog(long invoiceId)
        {
            try
            {
                var result=await  _myFatoorahPaymentLogService.UpdateInvoice(invoiceId,true,true,"paid");
                return result;
            }
            catch (Exception ex)
            {
                return false;
            }


        }



        [HttpGet]
        [Route("api/log-payment-exception")]
        public async Task LogExceptionsOnPayments(string exception, string trace="",string userId="",string comment="",string endPoint="")
        {
            _ = await _PaymentExceptionLogService.Insert(new PaymentExceptionLog()
            {
                CreatedTime = DateTime.Now,
                Comment=comment,
                EndPoint=endPoint,
                ExceptionMessage=exception,
                ExcptionTrace=trace,
                UserId=userId
            });

        }
        #endregion 
    }
}
