﻿using DataAccess.Entities;
using DataAccess.Entities.Views;
using DataAccess.Services;
using DataAccess.Utils;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using TollabAPI.Models;
using TollabAPI.SaftyNet;
using TollabAPI.Utils;

namespace TollabAPI.Controllers
{
    [Authorize]
    public class StudentsController : ApiController
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
        private StudentService _studentService;
        private PaymentMethodTypeService _paymentMethodTypeService;
        private TransactionService _transactionService;

        public StudentsController()
        {
            _studentService = new StudentService();
            _paymentMethodTypeService = new PaymentMethodTypeService();
            _transactionService = new TransactionService();
        }

        #region Register ,Login ,Get Profile, Update Profile ,Change Photo

        [AllowAnonymous]
        [HttpPost]
        [Route("api/Register")]
        public async Task<HttpResponseMessage> Register(Student student)
        {
            Student Resultdata = null;
            string Image = null;
            if (string.IsNullOrEmpty(student.Name) || student.CountryId == 0 || string.IsNullOrEmpty(student.Phone) || string.IsNullOrEmpty(student.PhoneKey) || string.IsNullOrEmpty(student.Email))
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Result Invalid Parametrs");
                response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            student.Phone = student.Phone.Trim();
            student.Phone = student.Phone.Replace(" ", "");

            if (!student.PhoneKey.StartsWith("+"))
            {
                student.PhoneKey = "+" + student.PhoneKey;
            }
            string PhoneNumber = MobileNumberChecker.handelMobileNumber(student.PhoneKey + student.Phone);
            ApplicationUser userAfterCreated = null;
            ApplicationUser userBeforeCreated = null;

            string email = student.Email;
            if (string.IsNullOrEmpty(student.Email))
                email = PhoneNumber + "@Tollabapp.com";

            userBeforeCreated = await UserManager.FindByEmailAsync(email);

            if (userBeforeCreated != null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "This Email Registerd Before");
                response.AddError(AppConstants.Code, AppConstants.This_Phone_Registerd_Before);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            var OldStudent = await _studentService.GetStudentProfileByPhoneOrEmail(student.Phone,student.Email);
            if (OldStudent != null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "This Phone Or Email Registerd Before");
                response.AddError(AppConstants.Code, AppConstants.This_Phone_Registerd_Before);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            try
            {
                var Identityuser = new ApplicationUser() { UserName = PhoneNumber, Email = email, PhoneNumber = PhoneNumber };

                IdentityResult result = await UserManager.CreateAsync(Identityuser, !string.IsNullOrEmpty(student.Password) ? student.Password : PhoneNumber);

                if (result.Succeeded)
                {
                    if (student.Photo != null)
                    {
                        Image = SetPhoto(student.Photo);
                    }
                    userAfterCreated = await UserManager.FindByEmailAsync(email);
                    student.CreationDate = DateTime.UtcNow;
                    student.IdentityId = userAfterCreated.Id;
                    student.Photo = Image;
                    student.Enabled = true;

                    Resultdata = await _studentService.Register(student);
                    if (Resultdata == null)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Please Try Again");
                        return response.getResponseMessage(HttpStatusCode.NotFound);
                    }
                    Resultdata.Vcode = 0;
                    response.AddModel(AppConstants.Student, Resultdata);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Registered Successfuly");
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                response.clearBody();
                response.AddError(AppConstants.Message, result.Errors.FirstOrDefault());
                response.AddError(AppConstants.Code, AppConstants.This_Phone_Registerd_Before);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            catch (Exception e)
            {
                //var UserToDelete = UserManager.FindByEmail(email);
                //if (UserToDelete != null)
                //{
                //    await _studentService.DeleteUser(UserToDelete.Id);
                //    await UserManager.DeleteAsync(UserToDelete);
                //}
                //if (e.HResult == -2146232060)
                //{
                //    response.clearBody();
                //    response.AddError(AppConstants.Message, "The Email Registerd Before ");
                //    return response.getResponseMessage(HttpStatusCode.InternalServerError);
                //}
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("api/get-student-by-Phone")]
        public async Task<HttpResponseMessage> GetStudentProfileByPhoneAndSendCode(string PhoneNumberWithKey,string email)
        {
            PhoneNumberWithKey = MobileNumberChecker.handelMobileNumber(PhoneNumberWithKey);
             
            try
            {
                var studentData = await _studentService.GetStudentProfileByPhoneAndSendCode(PhoneNumberWithKey,email);
                if (studentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Please complete registeration");
                    response.AddError(AppConstants.Code, AppConstants.Plese_complete_registeration);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                 
                if (studentData.Enabled == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                studentData.UserType = "student";
                response.AddModel(AppConstants.User, studentData);
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
        [Route("api/login-web")]
        public async Task<HttpResponseMessage> StudentLoginFromWeb(string PhoneNumberWithKey, string email)
        {
            // directpayment();
            if (string.IsNullOrEmpty(PhoneNumberWithKey))
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "invalide PhoneNumber");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            PhoneNumberWithKey = MobileNumberChecker.handelMobileNumber(PhoneNumberWithKey);
            if (string.IsNullOrEmpty(email))
                email = PhoneNumberWithKey + "@Tollabapp.com";

            var AppUser = await UserManager.FindByEmailAsync(email);
            if (AppUser == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Student Not Register Yet");
                response.AddError(AppConstants.Code, AppConstants.Result_Student_not_Register_Yet);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            try
            {
                var studentData = await _studentService.GetStudentByIdentityIdAsync(AppUser.Id);
                if (studentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Please complete registeration");
                    response.AddError(AppConstants.Code, AppConstants.Plese_complete_registeration);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                var resultData = await _studentService.GetStudentProfileAndSendCode(PhoneNumberWithKey, AppUser.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Please complete registeration");
                    response.AddError(AppConstants.Code, AppConstants.Plese_complete_registeration);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                if (resultData.Enabled == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                resultData.UserType = "student";
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
        [Route("api/StudentLogin")]
        public async Task<HttpResponseMessage> StudentLogin(string PhoneNumberWithKey)
        {
            // directpayment();
            if (string.IsNullOrEmpty(PhoneNumberWithKey))
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "invalide PhoneNumber");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            PhoneNumberWithKey = MobileNumberChecker.handelMobileNumber(PhoneNumberWithKey);
            string email = PhoneNumberWithKey + "@Tollabapp.com";

            var AppUser = await UserManager.FindByEmailAsync(email);
            if (AppUser == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Student Not Register Yet");
                response.AddError(AppConstants.Code, AppConstants.Result_Student_not_Register_Yet);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            try
            {
                var studentData = await _studentService.GetStudentByIdentityIdAsync(AppUser.Id);
                if (studentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Please complete registeration");
                    response.AddError(AppConstants.Code, AppConstants.Plese_complete_registeration);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                if (studentData.NumberCurrentLoginCount >= studentData.NumberMaxLoginCount)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                var resultData = await _studentService.GetStudentProfileAndSendCode(PhoneNumberWithKey, AppUser.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Please complete registeration");
                    response.AddError(AppConstants.Code, AppConstants.Plese_complete_registeration);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                if (resultData.Enabled == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                resultData.UserType = "student";
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
        [HttpPost]
        [Route("api/students/credentials/login")]
        public async Task<HttpResponseMessage> StudentCredentialsLogin(StudentCredentialsLoginModel loginModel)
        {
            if (string.IsNullOrEmpty(loginModel.UserName) || string.IsNullOrEmpty(loginModel.Password))
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Please enter UserName and Password");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            var AppUser = await UserManager.FindByEmailAsync(loginModel.UserName);
            if (AppUser == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Student Not Register Yet");
                response.AddError(AppConstants.Code, AppConstants.Result_Student_not_Register_Yet);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            var studentData = await _studentService.GetStudentByIdentityIdAsync(AppUser.Id);

            if (studentData == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Please complete registeration");
                response.AddError(AppConstants.Code, AppConstants.Plese_complete_registeration);
                return response.getResponseMessage(HttpStatusCode.NotFound);
            }
            if (studentData.Enabled == false )
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Your Account Is Disabled");
                response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                return response.getResponseMessage(HttpStatusCode.NotFound);
            }

            var existingUser = await UserManager.FindByIdAsync(studentData.IdentityId);
            if (existingUser == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Invalid User name Or Password");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            //TODO: Ibrahim Verifying is always failing
            var verificationResult = UserManager.PasswordHasher.VerifyHashedPassword(existingUser.PasswordHash, loginModel.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Invalid User name Or Password");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            var phoneNumber = MobileNumberChecker.handelMobileNumber(studentData.PhoneKey + studentData.Phone);
            var result = GetToken(phoneNumber, loginModel.Password);

            if (result.Item1)
            {
                //add token to data base
                var gToken = result.Item2.FirstOrDefault();
                var RealToken = (JObject)gToken;
                JToken Mytoken;
                RealToken.TryGetValue("access_token", out Mytoken);

                TokenStore tokenStore = new TokenStore()
                {
                    Token = "Bearer " + Mytoken.ToString(),
                    CreationDate = DateTime.UtcNow,
                    Valid = true,
                    StudentId = studentData.Id
                };
                await _studentService.AddTokenStoreAsync(tokenStore);
            }

            studentData.UserType = "student";
            studentData.Token = result.Item2;
            response.AddModel(AppConstants.User, studentData);
            response.AddMeta(AppConstants.Result, AppConstants.Success);
            response.AddMeta(AppConstants.Message, "Successfuly verifyied");
            return response.getResponseMessage(HttpStatusCode.OK);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/StudentLoginAndroid")]
        public async Task<HttpResponseMessage> StudentLoginAndroid(LoginAndoridModel loginAndoridModel)
        {
            // directpayment();

            if (string.IsNullOrEmpty(loginAndoridModel.PhoneNumberWithKey))
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "invalide PhoneNumber");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            loginAndoridModel.PhoneNumberWithKey = MobileNumberChecker.handelMobileNumber(loginAndoridModel.PhoneNumberWithKey);
            string email = loginAndoridModel.PhoneNumberWithKey + "@Tollabapp.com";

            var AppUser = await UserManager.FindByEmailAsync(email);
            if (AppUser == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Student Not Register Yet");
                response.AddError(AppConstants.Code, AppConstants.Result_Student_not_Register_Yet);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            try
            {
                var studentData = await _studentService.GetStudentByIdentityIdAsync(AppUser.Id);
                if (studentData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Please complete registeration");
                    response.AddError(AppConstants.Code, AppConstants.Plese_complete_registeration);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                if (!string.IsNullOrEmpty(loginAndoridModel.CheckToken))
                {
                    var statement = OfflineVerify.ParseAndVerify(loginAndoridModel.CheckToken);
                    if ((statement.BasicIntegrity == false || statement.CtsProfileMatch == false) && statement.ApkPackageName == "co.xapps.tollab")
                    {
                        studentData.Enabled = false;
                        await _studentService.UpdateProfile(studentData);
                        DisableReason disableReason = new DisableReason
                        {
                            CreationDate = DateTime.UtcNow,
                            Reason = "BasicIntegrity is " + statement.BasicIntegrity + "" + " CtsProfileMatch is " + statement.CtsProfileMatch + "",
                            StudentId = studentData.Id
                        };
                        await _studentService.AddDisableReason(disableReason);
                        //UserDeviceLog userDeviceLog = new UserDeviceLog
                        //{
                        //    AppVersion = "Android100",
                        //    CreationDate = DateTime.UtcNow,
                        //    DeviceName = "BasicIntegrity is " + statement.BasicIntegrity + "" + " CtsProfileMatch is " + statement.CtsProfileMatch + "",
                        //    StudentId = studentData.Id,
                        //    OS = "android",

                        //};
                        //await  _studentService.AddUserDeviceLogAsync(userDeviceLog);
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Your Account Is Disabled");
                        response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                        return response.getResponseMessage(HttpStatusCode.NotFound);
                    }
                }
                if (string.IsNullOrEmpty(loginAndoridModel.CheckToken) && !loginAndoridModel.State.StartsWith("1W7E8I6X9U3S11A3D"))
                {
                    studentData.Enabled = false;
                    await _studentService.UpdateProfile(studentData);
                    DisableReason disableReason = new DisableReason
                    {
                        CreationDate = DateTime.UtcNow,
                        Reason = "CheckToken is null and  State value is false ",
                        StudentId = studentData.Id
                    };
                    await _studentService.AddDisableReason(disableReason);

                    //UserDeviceLog userDeviceLog = new UserDeviceLog
                    //{
                    //    AppVersion ="",
                    //    CreationDate = DateTime.UtcNow,
                    //    DeviceName = "CheckToken is null and  State value is false ",
                    //    StudentId = studentData.Id,
                    //    OS = "android",

                    //};
                    //await _studentService.AddUserDeviceLogAsync(userDeviceLog);

                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                if (studentData.NumberCurrentLoginCount >= studentData.NumberMaxLoginCount)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                var resultData = await _studentService.GetStudentProfileAndSendCode(loginAndoridModel.PhoneNumberWithKey, AppUser.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Please complete registeration");
                    response.AddError(AppConstants.Code, AppConstants.Plese_complete_registeration);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                if (resultData.Enabled == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                resultData.UserType = "student";
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
        [Route("api/LoginAsGuest")]
        public async Task<HttpResponseMessage> LoginAsGuest()
        {
            try
            {
                var resultData = await _studentService.GetGuestProfile("01064850055G");
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Is Invalid Code");
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                var token = GetToken("Guest@Tollab.com", "Guest123456");

                var tokenString = JsonConvert.SerializeObject(token);
                if (tokenString.Contains("invalid_grant"))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "An Error Occurred");
                    return response.getResponseMessage(HttpStatusCode.InternalServerError);
                }
                resultData.Token = token.Item2;
                resultData.UserType = "Guest";
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Successfuly verifyied");
                return response.getResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("api/ResetUserPassword")]
        public async Task<HttpResponseMessage> ResetUserPassword(string email, string password)
        {
            var resultData = await UserManager.FindByEmailAsync(email);
            if (resultData == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Bad Request - Invalid Email");
                response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Email);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            string code = await UserManager.GeneratePasswordResetTokenAsync(resultData.Id);
             await UserManager.ResetPasswordAsync(resultData.Id, code, password);

            response.clearBody();
            response.AddModel(AppConstants.User, new { Email = resultData.Email, Password = password });
            return response.getResponseMessage(HttpStatusCode.OK);
        }
        [AllowAnonymous]
        [HttpGet]
        [Route("api/ResetPassword")]
        public async Task<HttpResponseMessage> Verify(string email, string secret)
        {
            if (secret != "5136842")
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Bad Request");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            var resultData = await UserManager.FindByEmailAsync(email);
            if (resultData == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Bad Request - Invalid Email");
                response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Email);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }

            string code = await UserManager.GeneratePasswordResetTokenAsync(resultData.Id);
            var password = System.Web.Security.Membership.GeneratePassword(6, 0);
            await UserManager.ResetPasswordAsync(resultData.Id, code, password);

            response.clearBody();
            response.AddModel(AppConstants.User, new { Email = resultData.Email, Password = password });
            return response.getResponseMessage(HttpStatusCode.OK);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("api/Verify")]
        public async Task<HttpResponseMessage> Verify(string PhoneKey, string Phone, int? vcode)
        {
            if (string.IsNullOrEmpty(Phone))
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Invalide PhoneNumber");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            if (vcode == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "This Is Invalid Code");
                response.AddError(AppConstants.Code, AppConstants.Result_Invalide_code);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            string PhoneNumberWithKey = PhoneKey + Phone;

            PhoneNumberWithKey = MobileNumberChecker.handelMobileNumber(PhoneNumberWithKey);
            try
            {
                var resultData = await _studentService.Verify(PhoneKey, Phone, vcode);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Is Invalid Code");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalide_code);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData.Enabled == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }
                string Password = PhoneNumberWithKey;
                string email = PhoneNumberWithKey + "@Tollabapp.com";
                if (resultData.Phone.Equals("01064850055G"))
                {
                    resultData.UserType = "Guest";
                }
                else
                {
                    resultData.UserType = "Student";
                }
                if (!string.IsNullOrEmpty(Password))
                {
                    string code = await UserManager.GeneratePasswordResetTokenAsync(resultData.IdentityId);
                    await UserManager.ResetPasswordAsync(resultData.IdentityId, code, Password);
                }
                Tuple<bool, JArray> result = GetToken(PhoneNumberWithKey, Password);

                if (result.Item1 == true)
                {
                    //add token to data base
                    var gToken = result.Item2.FirstOrDefault();
                    var RealToken = (JObject)gToken;
                    JToken Mytoken;
                    RealToken.TryGetValue("access_token", out Mytoken);

                    TokenStore tokenStore = new TokenStore()
                    {
                        Token = "Bearer " + Mytoken.ToString(),
                        CreationDate = DateTime.UtcNow,
                        Valid = true,
                        StudentId = resultData.Id
                    };
                    await _studentService.AddTokenStoreAsync(tokenStore);
                    //

                    resultData.Token = result.Item2;
                    response.AddModel(AppConstants.User, resultData);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Successfuly verifyied");
                    return response.getResponseMessage(HttpStatusCode.OK);
                }
                else
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "invalid phone");
                    response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);

                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
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
        [Route("api/VerifyVerificationCode")]
        public async Task<HttpResponseMessage> VerifyVerificationCode(string identityId, int? vcode)
        {
            if (vcode == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "This Is Invalid Code");
                response.AddError(AppConstants.Code, AppConstants.Result_Invalide_code);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }


            try
            {
                var resultData = await _studentService.Verify(identityId, vcode);
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Successfuly verifyied");
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
        [Route("api/VerifyWeb")]
        public async Task<HttpResponseMessage> VerifyWeb(string PhoneKey, string Phone, int? vcode, string password)
        {
            if (string.IsNullOrEmpty(Phone))
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "Invalide PhoneNumber");
                response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            if (vcode == null)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "This Is Invalid Code");
                response.AddError(AppConstants.Code, AppConstants.Result_Invalide_code);
                return response.getResponseMessage(HttpStatusCode.BadRequest);
            }
            string PhoneNumberWithKey = PhoneKey + Phone;
            PhoneNumberWithKey = MobileNumberChecker.handelMobileNumber(PhoneNumberWithKey);
            try
            {
                var resultData = await _studentService.Verify(PhoneKey, Phone, vcode);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "This Is Invalid Code");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalide_code);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData.Enabled == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                if (resultData.Phone.Equals("01064850055G"))
                {
                    resultData.UserType = "Guest";
                }
                else
                {
                    resultData.UserType = "Student";
                }

                Tuple<bool, JArray> result = GetToken(PhoneNumberWithKey, password);

                if (result.Item1)
                {
                    //add token to data base
                    var gToken = result.Item2.FirstOrDefault();
                    var RealToken = (JObject)gToken;
                    JToken Mytoken;
                    RealToken.TryGetValue("access_token", out Mytoken);

                    TokenStore tokenStore = new TokenStore()
                    {
                        Token = "Bearer " + Mytoken.ToString(),
                        CreationDate = DateTime.UtcNow,
                        Valid = true,
                        StudentId = resultData.Id
                    };
                    await _studentService.AddTokenStoreAsync(tokenStore);
                    //

                    resultData.Token = result.Item2;
                    response.AddModel(AppConstants.User, resultData);
                    response.AddMeta(AppConstants.Result, AppConstants.Success);
                    response.AddMeta(AppConstants.Message, "Successfuly verifyied");
                    return response.getResponseMessage(HttpStatusCode.OK);
                }
                else
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "invalid phone");
                    response.AddError(AppConstants.Code, AppConstants.Invalide_PhoneNumber);

                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
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
        [Route("api/GetStudentProfile")]
        public async Task<HttpResponseMessage> GetStudentProfile(string AppVersion = null, string DeviceName = null, string OS = null)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();

                var myToken = Request.Headers.GetValues("Authorization").FirstOrDefault();
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
                if (StudentData.Verified != true)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not verified");
                    response.AddError(AppConstants.Code, AppConstants.Unauthorized);
                    return response.getResponseMessage(HttpStatusCode.Unauthorized);
                }
                var resultData = await _studentService.GetStudentProfile(StudentData.Id);
                var tokenStore = await _studentService.GetTokenStoreAsync(myToken);
                if (tokenStore != null)
                {
                    if (tokenStore.Valid == false)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Student not found");
                        response.AddError(AppConstants.Code, AppConstants.Unauthorized);
                        return response.getResponseMessage(HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    TokenStore newtokenStore = new TokenStore()
                    {
                        StudentId = StudentData.Id,
                        Token = myToken,
                        Valid = true,
                        CreationDate = DateTime.UtcNow
                    };
                    await _studentService.AddTokenStoreAsync(newtokenStore);
                }
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData.Enabled == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                String UserIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(UserIP))
                {
                    UserIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
                var UserDeviceLog = new UserDeviceLog()
                {
                    AppVersion = AppVersion,
                    CreationDate = DateTime.UtcNow,
                    DeviceName = DeviceName,
                    IPAddress = UserIP,
                    OS = OS,
                    StudentId = resultData.Id
                };
                var addlog = await _studentService.AddUserDeviceLogAsync(UserDeviceLog);

                if (resultData.Phone.Equals("01064850055G"))
                {
                    resultData.UserType = "Guest";
                }
                else
                {
                    resultData.UserType = "Student";
                }
                response.AddModel(AppConstants.User, resultData);
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

        [HttpPost]
        [Route("api/GetStudentProfileForAndroid")]
        public async Task<HttpResponseMessage> GetStudentProfileForAndroid(GetprofileModel getprofileModel)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();

                var myToken = Request.Headers.GetValues("Authorization").FirstOrDefault();
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
                if (StudentData.Verified != true)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Unauthorized);
                    return response.getResponseMessage(HttpStatusCode.Unauthorized);
                }
                //-----------------------------------------------------------------------//
                if (!string.IsNullOrEmpty(getprofileModel.CheckToken))
                {
                    var statement = OfflineVerify.ParseAndVerify(getprofileModel.CheckToken);
                    if ((statement.BasicIntegrity == false || statement.CtsProfileMatch == false) && statement.ApkPackageName == "co.xapps.tollab")
                    {
                        StudentData.Enabled = false;
                        await _studentService.UpdateProfile(StudentData);
                        DisableReason disableReason = new DisableReason
                        {
                            CreationDate = DateTime.UtcNow,
                            Reason = "BasicIntegrity is " + statement.BasicIntegrity + "" + " CtsProfileMatch is " + statement.CtsProfileMatch + "",
                            StudentId = StudentData.Id
                        };
                        await _studentService.AddDisableReason(disableReason);

                        response.clearBody();
                        response.AddError(AppConstants.Message, "Your Account Is Disabled");
                        response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                        return response.getResponseMessage(HttpStatusCode.NotFound);
                    }
                }
                if (string.IsNullOrEmpty(getprofileModel.CheckToken) && !getprofileModel.State.StartsWith("1W7E8I6X9U3S11A3D"))
                {
                    StudentData.Enabled = false;
                    await _studentService.UpdateProfile(StudentData);
                    DisableReason disableReason = new DisableReason
                    {
                        CreationDate = DateTime.UtcNow,
                        Reason = "CheckToken is null and  State value is false ",
                        StudentId = StudentData.Id
                    };
                    await _studentService.AddDisableReason(disableReason);

                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                //-----------------------------------------------------------------------//
                var resultData = await _studentService.GetStudentProfile(StudentData.Id);
                var tokenStore = await _studentService.GetTokenStoreAsync(myToken);
                if (tokenStore != null)
                {
                    if (tokenStore.Valid == false)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Student not found");
                        response.AddError(AppConstants.Code, AppConstants.Unauthorized);
                        return response.getResponseMessage(HttpStatusCode.Unauthorized);
                    }
                }
                else
                {
                    TokenStore newtokenStore = new TokenStore()
                    {
                        StudentId = StudentData.Id,
                        Token = myToken,
                        Valid = true,
                        CreationDate = DateTime.UtcNow
                    };
                    await _studentService.AddTokenStoreAsync(newtokenStore);
                }
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                if (resultData.Enabled == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Your Account Is Disabled");
                    response.AddError(AppConstants.Code, AppConstants.Your_Account_Is_Disabled);
                    return response.getResponseMessage(HttpStatusCode.NotFound);
                }

                String UserIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(UserIP))
                {
                    UserIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
                var UserDeviceLog = new UserDeviceLog()
                {
                    AppVersion = getprofileModel.AppVersion,
                    CreationDate = DateTime.UtcNow,
                    DeviceName = getprofileModel.DeviceName,
                    IPAddress = UserIP,
                    OS = getprofileModel.OS,
                    StudentId = resultData.Id
                };
                var addlog = await _studentService.AddUserDeviceLogAsync(UserDeviceLog);

                if (resultData.Phone.Equals("01064850055G"))
                {
                    resultData.UserType = "Guest";
                }
                else
                {
                    resultData.UserType = "Student";
                }
                response.AddModel(AppConstants.User, resultData);
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

        [HttpPost]
        [Route("api/ChangePhoto")]
        public async Task<HttpResponseMessage> ChangePhoto(Student student)
        {
            string Photo = student.Photo;

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
                string Image = null;
                if (string.IsNullOrEmpty(Photo))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Result Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                Image = SetPhoto(Photo);
                StudentData.Photo = Image;
                var resultData = await _studentService.UpdateProfile(StudentData);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "operation not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Updated Successfuly");
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

        [HttpPost]
        [Route("api/EditProfile")]
        public async Task<HttpResponseMessage> EditProfile(Student student)
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
                string Image = null;
                if (string.IsNullOrEmpty(student.Name) || string.IsNullOrEmpty(student.Bio) || string.IsNullOrEmpty(student.Email))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Result Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                StudentData.Name = student.Name;
                StudentData.Email = student.Email;
                StudentData.Gender = student.Gender;
                StudentData.Bio = student.Bio;
                StudentData.ParentName = student.ParentName;
                StudentData.ParentPhone = student.ParentPhone;
                StudentData.ParentName2 = student.ParentName2;
                StudentData.ParentPhone2 = student.ParentPhone2;
                var resultData = await _studentService.UpdateProfile(StudentData);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "operation not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Updated Successfuly");
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

        [HttpPost]
        [Route("api/UpdateStudentParentData")]
        public async Task<HttpResponseMessage> UpdateStudentParentData(Student student)
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
                StudentData.ParentName = student.ParentName;
                StudentData.ParentPhone = student.ParentPhone;
                StudentData.ParentName2 = student.ParentName2;
                StudentData.ParentPhone2 = student.ParentPhone2;
                var resultData = await _studentService.UpdateProfile(StudentData);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "operation not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Updated Successfuly");
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
        [Route("api/UpdateScreenShootCount")]
        public async Task<HttpResponseMessage> UpdateScreenShootCount()
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
                if (StudentData.ScreenShootCount == null)
                {
                    StudentData.ScreenShootCount = 0;
                }
                StudentData.ScreenShootCount = StudentData.ScreenShootCount + 1;
                StudentData.LastTakenScreenshootDate = DateTime.UtcNow;
                var resultData = await _studentService.UpdateProfile(StudentData);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "operation not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Updated Successfuly");
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

        [HttpPost]
        [Route("api/AddStudentPushToken")]
        public async Task<HttpResponseMessage> AddStudentPushToken(StudentPushToken studentPushToken)
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

                studentPushToken.StudentId = StudentData.Id;
                var resultData = await _studentService.AddStudentPushToken(studentPushToken);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "operation not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Updated Successfuly");
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

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        [HttpGet]
        [Route("api/StudentLogout")]
        public async Task<HttpResponseMessage> StudentLogout()
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

                Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                Request.GetOwinContext().Authentication.SignOut();

                Request.GetOwinContext().Authentication.SignOut(Microsoft.AspNet.Identity.DefaultAuthenticationTypes.ApplicationCookie);

                HttpContext.Current.GetOwinContext().Authentication.SignOut(Microsoft.AspNet.Identity.DefaultAuthenticationTypes.ApplicationCookie);
                StudentData.NumberCurrentLoginCount = 0;
                var d = await _studentService.UpdateProfile(StudentData);
                var resultData = await _studentService.DeletePushTokens(StudentData.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "operation not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Updated Successfuly");
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
        [Route("api/DisableStudent")]
        public async Task<HttpResponseMessage> DisableStudent()
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

                StudentData.Enabled = false;
                var resultData = await _studentService.UpdateProfile(StudentData);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "operation not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                DisableReason disableReason = new DisableReason
                {
                    CreationDate = DateTime.UtcNow,
                    Reason = "Disabled from android code",
                    StudentId = StudentData.Id
                };
                await _studentService.AddDisableReason(disableReason);

                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Updated Successfuly");
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

        [HttpPost]
        [AllowAnonymous]
        [Route("api/SecurityLog")]
        public async Task<HttpResponseMessage> SecurityLog(SecurityLog securityLog)
        {
            try
            {
                String UserIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(UserIP))
                {
                    UserIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
                securityLog.CreationDate = DateTime.UtcNow;
                securityLog.IpAddress = UserIP;
                var resultData = await _studentService.AddSecurityLog(securityLog);

                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Updated Successfuly");
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

        public string SetPhoto(string image)
        {
            var basePath = System.Web.Hosting.HostingEnvironment.MapPath("~");
            if (basePath.EndsWith("sws"))
            {
                basePath = basePath.Remove(basePath.Length - 3);
            }

            string imagePath = "/ws/Images/StudentImages/";
            string fileName = "UI" + DateTime.Now.ToString("IMG" + "dd_MM_yyyy_HH_mm_ss") + ".png";
            byte[] fileBytes = Convert.FromBase64String(image);
            MemoryStream ms = new MemoryStream(fileBytes);
            string fullPath = basePath + imagePath + fileName;
            System.IO.Directory.CreateDirectory(basePath + imagePath);
            FileStream fs = new FileStream(fullPath, FileMode.Create);
            ms.WriteTo(fs);
            ms.Close();
            fs.Close();
            fs.Dispose();
            return fileName;
        }

        [NonAction]
        private static Tuple<bool, JArray> GetToken(string userName, string password)
        {
            var request = HttpContext.Current.Request;
            var tokenServiceUrl = request.Url.GetLeftPart(UriPartial.Authority) + request.ApplicationPath + "/token";
            using (var client = new HttpClient())
            {
                var requestParams = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", userName),
                    new KeyValuePair<string, string>("password", password)
                };
                var requestParamsFormUrlEncoded = new FormUrlEncodedContent(requestParams);
                var tokenServiceResponse = client.PostAsync(tokenServiceUrl, requestParamsFormUrlEncoded).Result;
                var loginResponse = tokenServiceResponse.Content.ReadAsStringAsync().Result;
                var loginResponseMessageContent = new JArray();
                var responseObject = JObject.Parse(loginResponse);
                loginResponseMessageContent.Add(responseObject);

                var responseCode = tokenServiceResponse.StatusCode;
                if (responseCode == HttpStatusCode.OK)
                    return Tuple.Create(true, loginResponseMessageContent);
                else
                    return Tuple.Create(false, loginResponseMessageContent);
            }
        }

        #endregion Register ,Login ,Get Profile, Update Profile ,Change Photo

        [HttpGet]
        [AllowAnonymous]
        [Route("api/GetPaymentMethodTypes")]
        public async Task<HttpResponseMessage> GetPaymentMethodTypes(string BuildNumber)
        {
            try
            {
                var CountryCode = "";
                Student StudentProfile = null;
                string ProfileCountryCode = null;
                string IdentityUserId = User.Identity.GetUserId();
                if (!string.IsNullOrEmpty(IdentityUserId))
                {
                    var StudentData = await _studentService.GetStudentByIdentityIdAsync(IdentityUserId);
                    if (StudentData == null)
                    {
                        response.clearBody();
                        response.AddError(AppConstants.Message, "Student not found");
                        response.AddError(AppConstants.Code, AppConstants.Student_Not_Found);
                        return response.getResponseMessage(HttpStatusCode.BadRequest);
                    }

                    StudentProfile = await _studentService.GetStudentProfile(StudentData.Id);
                    CountryCode = StudentProfile.CountryCode;
                }

                if (StudentProfile != null)
                {
                    ProfileCountryCode = StudentProfile.CountryCode;
                }

                String UserIP = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(UserIP))
                {
                    UserIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                }
                if (BuildNumber != "0")
                {
                    CountryCode = await GetCountryCodeByIP(UserIP);
                }
                if (CountryCode == null)
                {
                    if (ProfileCountryCode != null)
                        CountryCode = ProfileCountryCode;
                }
                // if student ip not found in system countries set it by profile country
                var CountrCodes = await _studentService.GetCountriesCodes();
                if (CountrCodes.Contains(CountryCode))
                {
                    if (ProfileCountryCode != null)
                        CountryCode = ProfileCountryCode;
                }
                var resultData = await _paymentMethodTypeService.GetPaymentMethodTypeAsync(BuildNumber, CountryCode);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, "" + UserIP + "");
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return response.getResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                response.AddError(AppConstants.Error, e);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        public async Task<string> GetCountryCodeByIP(string IP)
        {
            try
            {
                //IP = "156.209.172.105:59378";
                var IPS = IP.Split(':');
                IP = IPS[0];
                string url = "http://api.ipstack.com/" + IP.ToString() + "?access_key=ed9b625dad0d104e31406ec48377095f";

                WebClient client = new WebClient();
                string jsonstring = client.DownloadString(url);
                dynamic dynObj = JsonConvert.DeserializeObject(jsonstring);
                var UserCountryCode = dynObj.country_code;
                return UserCountryCode;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #region Section Module

        [HttpGet]
        [Route("api/GetSections")]
        public async Task<HttpResponseMessage> GetSections(bool isIncludeSubCategory = false)
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

                var resultData = await _studentService.GetSectionsByCountryId(StudentData.CountryId,isIncludeSubCategory);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetCategoriesWithSubCategoriesBySectionId")]
        public async Task<HttpResponseMessage> GetCategoriesWithSubCategoriesBySectionId(long SectionId, int Page = 0)
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

                var resultData = await _studentService.GetCategoriesWithSubCategoriesBySectionId(SectionId, Page);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetSubCategoriesByCategoryId")]
        public async Task<HttpResponseMessage> GetSubCategoriesByCategoryId(long CategoryId, int Page = 0)
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

                var resultData = await _studentService.GetSubCategoriesByCategoryId(CategoryId, Page);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetDepartmentsBySubCategoryId")]
        public async Task<HttpResponseMessage> GetDepartmentsBySubCategoryId(long SubCategoryId, int Page = 0)
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

                var resultData = await _studentService.GetDepartmentsBySubCategoryId(SubCategoryId, Page);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpPost]
        [Route("api/AddDepartmentToStudent")]
        public async Task<HttpResponseMessage> AddDepartmentToStudent(List<long> DepartmentIds)
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

                var resultData = await _studentService.AddDepartmentToStudent(DepartmentIds, StudentData.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpPost]
        [Route("api/AddDepartmentToStudentForWeb")]
        public async Task<HttpResponseMessage> AddDepartmentToStudentForWeb(List<long> DepartmentIds)
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

                var resultData = await _studentService.AddDepartmentToStudentForWeb(DepartmentIds, StudentData.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpDelete]
        [Route("api/DeleteDepartmentByIds")]
        public async Task<HttpResponseMessage> DeleteDepartmentByIds(List<long> DepartmentIds)
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

                var resultData = await _studentService.DeleteStudentDepartments(DepartmentIds, StudentData.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetCoursesByDepartmentId")]
        public async Task<HttpResponseMessage> GetCoursesByDepartmentId(long DepartmentId, int Page = 0)
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

                var resultData = await _studentService.GetCoursesByDepartmentId(DepartmentId, Page);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetSubjectsWithTracksByDepartmentId")]
        public async Task<HttpResponseMessage> GetSubjectsWithTracksByDepartmentId(long DepartmentId, int Page = 0)
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

                var resultData = await _studentService.GetSubjectsWithTracksByDepartmentId(StudentData.Id, DepartmentId, Page);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        #endregion Section Module

        #region Home

        [HttpGet]
        [Route("api/GetHomeCourses")]
        public async Task<HttpResponseMessage> GetHomeCourses(long Page = 0, long categoryId = 0, long subCategoryId = 0, long subjectId = 0)
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

                var resultData = await _studentService.GetHomeCoursesByStudentId(StudentData.Id, StudentData.CountryId, Page,categoryId,subCategoryId,subjectId);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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
        [HttpGet]
        [Route("api/GetHomeCoursesClassifiedBySubCategory")]
        public async Task<HttpResponseMessage> GetHomeCoursesClassifiedBySubCategory(long Page = 0, long categoryId = 0, long subCategoryId = 0, long subjectId = 0)
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

                var resultData = await _studentService.GetHomeCoursesClassifiedBySubCategoryByStudentId(StudentData.Id, StudentData.CountryId, Page,categoryId:categoryId,subCategoryId:subCategoryId,subjectId:subjectId);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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
        
        [HttpGet]
        [Route("api/GetTopLives")]
        public async Task<HttpResponseMessage> GetTopLives()
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                //string IdentityUserId = "556b1ba5-5acb-49d8-b780-93f37ce85dc9";
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

                var resultData = await _studentService.GetTopLives(StudentData.CountryId, StudentData.Id);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetLives")]
        public async Task<HttpResponseMessage> GetLives(int Page = 0)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                //string IdentityUserId = "556b1ba5-5acb-49d8-b780-93f37ce85dc9";
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

                var resultData = await _studentService.GetLives(StudentData.CountryId, StudentData.Id, Page);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetLiveDetails")]
        public async Task<HttpResponseMessage> GetLiveDetails(int id)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                //string IdentityUserId = "556b1ba5-5acb-49d8-b780-93f37ce85dc9";
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

                var resultData = await _studentService.GetLive(id, StudentData.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetInterestsBeforeEdit")]
        public async Task<HttpResponseMessage> GetInterestsBeforeEdit()
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

                var resultData = await _studentService.GetInterestsBeforeEdit(StudentData.Id);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        #endregion Home

        #region Courses Module

        [HttpGet]
        [Route("api/GetTrackById")]
        public async Task<HttpResponseMessage> GetTrackById(long TrackId)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "User not found");
                    response.AddError(AppConstants.Code, AppConstants.User_Not_Found);
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

                var resultData = await _studentService.GetTrackById(TrackId, StudentData.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetCourseWithOneContentForStudent")]
        public async Task<HttpResponseMessage> GetCourseWithOneContentForStudent(long CourseId, long ContentId, long VideoQuestionId)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "User not found");
                    response.AddError(AppConstants.Code, AppConstants.User_Not_Found);
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

                var resultData = await _studentService.GetCourseWithOneContentForStudent(CourseId, ContentId, StudentData.Id, VideoQuestionId);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetCoursesByTrackId")]
        public async Task<HttpResponseMessage> GetCoursesByTrackId(long TrackId)
        {
            try
            {
                string IdentityUserId = User.Identity.GetUserId();
                if (string.IsNullOrEmpty(IdentityUserId))
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "User not found");
                    response.AddError(AppConstants.Code, AppConstants.User_Not_Found);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }

                var resultData = await _studentService.GetCoursesByTrackId(TrackId);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetCourseByIdForCurrentStudent")]
        public async Task<HttpResponseMessage> GetCourseByIdForCurrentStudent(long CourseId)
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
                if (StudentData.Verified != true)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Unauthorized);
                    return response.getResponseMessage(HttpStatusCode.Unauthorized);
                }
                if (StudentData.Enabled != true)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Unauthorized);
                    return response.getResponseMessage(HttpStatusCode.Unauthorized);
                }
                var resultData = await _studentService.GetCourseById(CourseId, StudentData.Id);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return response.getResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                try
                {
                    LogError logError = new LogError()
                    {
                        ErrorMessage = e.Message,
                        ErrorCode = "0",
                    };
                    await _studentService.AddLog(logError);
                }
                catch
                {
                }
                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [Route("api/GetStudentCourses")]
        public async Task<HttpResponseMessage> GetStudentCourses(int Page = 0)
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

                var resultData = await _studentService.GetMyCourses(StudentData.Id, Page);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetGroupsWithContentsByCourseIdForCurrentStudent")]
        public async Task<HttpResponseMessage> GetGroupsWithContentsByCourseIdForCurrentStudent(long CourseId, int Page = 0, long ContentId = 0)
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
                if (StudentData.Verified != true)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Unauthorized);
                    return response.getResponseMessage(HttpStatusCode.Unauthorized);
                }
                if (StudentData.Enabled != true)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Student not found");
                    response.AddError(AppConstants.Code, AppConstants.Unauthorized);
                    return response.getResponseMessage(HttpStatusCode.Unauthorized);
                }
                var resultData = await _studentService.GetGroupsWithContentsByCourseId(CourseId, StudentData.Id, Page, ContentId);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
                    return response.getResponseMessage(HttpStatusCode.OK);
                }

                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Returned Successfuly");
                return response.getResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                try
                {
                    LogError logError = new LogError()
                    {
                        ErrorMessage = e.Message,
                        ErrorCode = "0",
                    };
                    await _studentService.AddLog(logError);
                }
                catch
                {
                }

                response.clearBody();
                response.AddError(AppConstants.Message, "An Error Occurred");
                response.AddError(AppConstants.Code, AppConstants.Result_error_try_later);
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [Route("api/ViewThisContent")]
        public async Task<HttpResponseMessage> ViewThisContent(long ContentId)
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
                if (ContentId < 1)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Invalid Parametrs");
                    response.AddError(AppConstants.Code, AppConstants.Result_Invalid_Parametrs);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                var resultData = await _studentService.ViewThisContent(ContentId, StudentData.Id);
                if (resultData == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Operation Not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
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

        [HttpGet]
        [Route("api/AddCourseToFavourite")]
        public async Task<HttpResponseMessage> AddCourseToFavourite(long CourseId)
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
                Favourite favourite = new Favourite
                {
                    CourseId = CourseId,
                    StudentId = StudentData.Id
                };
                var resultData = await _studentService.AddFavourite(favourite);
                if (resultData == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Operation Not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
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

        [HttpGet]
        [Route("api/DeleteCourseFromFavourite")]
        public async Task<HttpResponseMessage> DeleteCourseFromFavourite(long CourseId)
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
                Favourite favourite = new Favourite
                {
                    CourseId = CourseId,
                    StudentId = StudentData.Id
                };
                var resultData = await _studentService.DeleteFavourite(favourite);
                if (resultData == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Operation Not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
                }
                response.AddModel(AppConstants.User, resultData);
                response.AddMeta(AppConstants.Result, AppConstants.Success);
                response.AddMeta(AppConstants.Message, "Deleted Successfuly");
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
        [Route("api/GetMyFavourites")]
        public async Task<HttpResponseMessage> GetMyFavourites(int Page = 0)
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

                var resultData = await _studentService.GetAllFavourite(StudentData.Id, Page);
                if (resultData.Count() == 0)
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

        #endregion Courses Module

        #region Search module

        [HttpGet]
        [Route("api/Search")]
        public async Task<HttpResponseMessage> Search(string Word, int Page = 0)
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
                if (string.IsNullOrEmpty(Word))
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
                var studentCountry = StudentData.CountryId;
                var resultData = await _studentService.Serach(Word, StudentData.Id, studentCountry, Page);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/GetSavedWords")]
        public async Task<HttpResponseMessage> GetSavedWords()
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

                var resultData = await _studentService.GetSavedWords(StudentData.Id);
                if (resultData.Count() == 0)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "No Data");
                    response.AddError(AppConstants.Code, AppConstants.Result_No_Data_returned);
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

        [HttpGet]
        [Route("api/DeleteWord")]
        public async Task<HttpResponseMessage> DeleteWord(string Word)
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
                if (string.IsNullOrEmpty(Word))
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

                var resultData = await _studentService.DeleteWord(StudentData.Id, Word);
                if (resultData == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Operation Not Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadGateway);
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

        #endregion Search module

        #region Notification Module

        [HttpGet]
        [Route("api/GetAllStudentNotification")]
        public async Task<HttpResponseMessage> GetAllStudentNotification(int Page = 0)
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

                var resultData = await _studentService.GetAllStudentNotification(StudentData.Id, Page);
                if (resultData.Count() == 0)
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

        [HttpGet]
        [Route("api/SeenStudentNotification")]
        public async Task<HttpResponseMessage> SeenStudentNotification(long NotificationId)
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

                var resultData = await _studentService.SeenStudentNotification(StudentData.Id, NotificationId);
                if (resultData == false)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "Operation_Not_Completed");
                    response.AddError(AppConstants.Code, AppConstants.Operation_Not_Completed);
                    return response.getResponseMessage(HttpStatusCode.BadRequest);
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

        [HttpGet]
        [Route("api/GetStudentNotificationNotSeenCount")]
        public async Task<HttpResponseMessage> GetStudentNotificationNotSeenCount()
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

                var resultData = await _studentService.GetStudentNotificationNotSeenCount(StudentData.Id);

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

        #endregion Notification Module

        //paymentId=100201923790872553

        public void directpayment()
        {
            try
            {
                //####### Direct Payment ######

                //string token = @"i6PbcDUyfgtlAa0dWwW4N5FEsDpKaPfFeLox4He0Y-ylMQ3BT629H1EkuoOi55cITROETH_BmS8_u2-qX4hIKPc4l4ImL4EJV2FQIg8psAf-u-JsA4MJFk1VJc5u5K-hmzDTnUi9QEgcZ3sa8XVc9dYx3M_ZxoYYZUTgeZPte_5WcvmeNyqWUyU69d5v5d7EMBDnSZErlPQUhO-k0fvRKuaxTYh7IfFr3gAvvi5k3p3bdUl9XY6_kTrkAGyUuZuF390kGGcExhCs1yKhNpkNy_aspWRatTdN6FSU7ewqyR_1CwJRVanVHYn-hyzEhtH887YKQW75cFpxFnkBDTtm_z95s9AsuQJ1yIb1ZfetnKk8K-GIRwygftlFSNVMAmPQxhiVajQi4sgVQFjVMIb6edxgiP0UQjU9_L-Ra3Glv2WhRl9xUG0v8twXNovaD-d6LHKs8Nt0GhjjVRHzg9tgZawIguw5h5WqAlVayshI5GZibg0qfUB0fhhZyoqC757ugyevkMRTNIt30hkuJ79MmQPqDf7ZKa_cUA9zsos_HNia7125cIb19QhRqyGejOcL6ZYmf3dMoJPQUlxVGD3h70xtz4BDoEdOxy0-CU1KGCX5lzCyUpGK4tkJEP_ovLuL6wy71JnNfZ_2seXT3Acwg6UV42L8h985gxd98AtdEZILVHv-"; //token value to be placed here
                string token = @"TXLrkmSj-VlRTOOC2GCkpLbg2fWXIgcucpP6p0T94ZXcd3uqdg-YI7IUjCbaU1DsdsAGjIW3gnczqjv2CLFKfsiZ3GcD0H6zo5BxFCiAwK45lFGBDdmIw91QRPOtudpxuPJvdkjV_GVVyg5tfndVMc46CuSoNBqfLuzUWiSE51sy-EgboaIZHpFU8xl4fGRFzAwPprwFinftAq3cWTHDEb5dKcxrqIlVxpJM9gqdFo5S3-BsapiEBaVc69QEg2WXVSSf00giFXGiiCiXdD6LZQKn1iE3wQaJttbdDdNjPuLtH0KxNdqC24ONZEh6UKPDKWmOItbyDp-eA5lPJEsAo6BaLUQ5bcFQZXV7k0fk1Dnq4Wj0Rv9SmM7uyC58YFv6b2vxkcgbV1tu8D1bXPSgq7DlvpMn4mh-H1gBisp4xPjYzpfP91n3gvHuizUp4vd70VIuuGY1-cvOGeUs59RfrP4wk_X4UI_qjwNkVF0fS1Of02cIi4AFWNwGkT-ZZhz7Bg-9lyhrOQYrNiO1mIGgxv-OiG5Cc3y5arR7ZpSYl4K8A2TwQNCXZChoIdXwSDMYvHZTZHdmnNlTM2u7lXro9YDluR0vyE5rNacAI9ubEh-iCH7WeJF2xr32Pp_APn22BVyd-4gNpS5XUOIEK21xBxg2NAkuO2ukYC6CoyAAGeGRDBWOQjvm1gdzSjQ-AKrWNJiKwQ"; //token value to be placed here

                //  string baseURL = "https://api.myfatoorah.com";
                string baseURL = "https://apitest.myfatoorah.com";
                var url = baseURL + "/v2/ExecutePayment";

                var client = new HttpClient();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);

                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                string json = "{\"PaymentMethodId\":\"2\",\"CustomerName\": \"Ahmed\",\"DisplayCurrencyIso\": \"KWD\", \"MobileCountryCode\":\"+965\",\"CustomerMobile\": \"92249038\",\"CustomerEmail\": \"aramadan@myfatoorah.com\",\"InvoiceValue\": 100,\"CallBackUrl\": \"https://google.com\",\"ErrorUrl\": \"https://google.com\",\"Language\": \"en\",\"CustomerReference\" :\"ref1\",\"CustomerCivilId\":12345678,\"UserDefinedField\": \"Custom field\",\"ExpireDate\": \"\",\"CustomerAddress\" :{\"Block\":\"\",\"Street\":\"\",\"HouseBuildingNo\":\"\",\"Address\":\"\",\"AddressInstructions\":\"\"},\"InvoiceItems\": [{\"ItemName\": \"Product 01\",\"Quantity\": 1,\"UnitPrice\": 100}]}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var messge = client.PostAsync(url, content).Result;
                string result = messge.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                var tempResponse = JObject.Parse(result);
                result = tempResponse.ToString();
                // context.Response.WriteAsync("\n" + result + "\n");
                JObject obj = JObject.Parse(result);
                string paymentURL = (string)obj["Data"]["PaymentURL"];

                url = paymentURL;
                client = new HttpClient(); client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                json = "{\"paymentType\": \"card\",\"card\": {\"Number\":\"000000000000001\",\"expiryMonth\":\"09\",\"expiryYear\":\"21\",\"securityCode\":\"100\"},\"saveToken\": false}";
                content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                messge = client.PostAsync(url, content).Result;
                result = messge.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                //context.Response.WriteAsync("\n" + result + "\n");
                tempResponse = JObject.Parse(result);
                result = tempResponse.ToString();
            }
            catch (Exception e)
            {
                throw;
            }
        }


        [AllowAnonymous]
        [HttpGet]
        [Route("api/get-student-by-Id/{userId}")]
        public async Task<HttpResponseMessage> GetSTudentById(long userId)
        {
            try
            {
                var resultData = await _studentService.GetStudentById(userId);
                if (resultData == null)
                {
                    response.clearBody();
                    response.AddError(AppConstants.Message, "NotFount");
                    return response.getResponseMessage(HttpStatusCode.NotFound);
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
                return response.getResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }
}