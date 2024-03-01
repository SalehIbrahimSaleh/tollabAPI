﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System.IdentityModel.Tokens.Jwt;
//using System.Net;
//namespace TollabAPI.SaftyNet
//{
//    public class OnlineVerify
//    {
//        /// <summary>
//        /// Please use the Google Developers Console
//        /// (https://console.developers.google.com/) to create a project,
//        /// enable the Android Device Verification API, generate an API key
//        /// and add it here.
//        /// </summary>
//        private static string ApiKey = "AIzaSyAAdKh2flxKK_n5sksyaZiHk-6bXOTlh3o";

//        private static string ServiceUrl =
//            "https://www.googleapis.com/androidcheck/v1/attestations/verify?key=";

//        /// <summary>
//        /// Parses and verifies a SafetyNet attestation statement.
//        /// </summary>
//        /// <param name="signedAttestationStatement">A string containing the
//        /// JWT attestation statement.</param>
//        /// <returns>A parsed attestation statement. null if the statement is
//        /// invalid.</returns>
//        public static AttestationStatement ParseAndVerify(
//            string signedAttestationStatement)
//        {
//            // First parse the token.
//            JwtSecurityToken token;
//            try
//            {
//                token = new JwtSecurityToken(signedAttestationStatement);
//            }
//            catch (ArgumentException)
//            {
//                // The token is not in a valid JWS format.
//                return null;
//            }

//            string response;
//            try
//            {
//                response = VerifyRequestOnline(signedAttestationStatement);
//            }
//            catch (WebException)
//            {
//                return null;
//            }

//            try
//            {
//                if (!ContainsValidSignature(response))
//                {
//                    return null;
//                }
//            }
//            catch (KeyNotFoundException)
//            {
//                return null;
//            }

//            // Parse and use the data JSON.
//            Dictionary<string, string> claimsDictionary = token.Claims
//                .ToDictionary(x => x.Type, x => x.Value);

//            return new AttestationStatement(claimsDictionary);
//        }

//        /// <summary>
//        /// Sends a SafetyNet online validation request to Google's servers.
//        /// </summary>
//        /// <param name="signedAttestationStatement">The attestation statement
//        /// as returned by getJwsResult() on the client device.</param>
//        /// <returns>The raw server response.</returns>
//        /// <exception cref="WebException">Thrown when there was an error while
//        /// connecting to the Google service.
//        /// Ensure that you added your API key and enabled the Android Device
//        /// verification API.</exception>
//        private static string VerifyRequestOnline(
//            string signedAttestationStatement)
//        {
//            // Create the message to be sent to the validation server, as per
//            // the documentation on
//            // https://developer.android.com/training/safetynet/attestation.html#verify-compat-check
//            var message = new Dictionary<string, string>()
//            {
//                {"signedAttestation", signedAttestationStatement }
//            };
//            var messageJson = JsonConvert.SerializeObject(message,
//                Formatting.None);

//            // Perform the validation.
//            var client = new WebClient();
//            client.Headers[HttpRequestHeader.ContentType] = "application/json";

//            // Perform the validation synchronously. For high volume requests,
//            // you may want to use WebClient.UploadStringAsync() instead, and
//            // process the response asynchronously.
//            string response = "";
//            response = client.UploadString(ServiceUrl + ApiKey, "POST", messageJson);
//            return response;
//        }

//        /// <summary>
//        /// Verifies whether the signature in the server response is valid. We
//        /// expect to receive a simple JSON response from the server like this:
//        /// { "isValidSignature": true }
//        /// </summary>
//        /// <param name="serverResponse"></param>
//        /// <returns>true if the contained signature is valid.</returns>
//        /// <exception cref="KeyNotFoundException">Thrown if responseJson
//        /// doesn't include a field called "isValidSignature".</exception>
//        private static bool ContainsValidSignature(string serverResponse)
//        {
//            var responseJson = JObject.Parse(serverResponse);
//            return responseJson["isValidSignature"].Value<bool>();
//        }
//    }
//}