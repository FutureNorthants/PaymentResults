using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PaymentResults
{
    public class Function
    {
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest Request, ILambdaContext context)
        {

            String instance = Environment.GetEnvironmentVariable("instance");
            String cxmEndPoint = "";
            String cxmAPIKey = "";
            String cxmToken = "";
            String cxmUser = "";
            String cxmPassword = "";
            String paymentReference = "";
            String paymentConfirmation = "";
            switch (instance.ToLower())
            {
                case "live":
                    cxmEndPoint = Environment.GetEnvironmentVariable("cxmEndPointLive");
                    cxmAPIKey = Environment.GetEnvironmentVariable("cxmAPIKeyLive");
                    cxmUser = Environment.GetEnvironmentVariable("cxmLiveUser");
                    cxmPassword = Environment.GetEnvironmentVariable("cxmLivePassword");
                    paymentConfirmation = "https://mycouncil-stage.northampton.digital/intranet/parkingPaymentConfirmation.html";
                    break;
                default:
                    cxmEndPoint = Environment.GetEnvironmentVariable("cxmEndPointTest");
                    cxmAPIKey = Environment.GetEnvironmentVariable("cxmAPIKeyTest");
                    cxmUser = Environment.GetEnvironmentVariable("cxmTestUser");
                    cxmPassword = Environment.GetEnvironmentVariable("cxmTestPassword");
                    paymentConfirmation = "https://mycouncil-stage.northampton.digital/intranet/parkingPaymentConfirmation.html";
                    break;
            }

            context.Logger.LogLine("instance="+instance);
            context.Logger.LogLine("cxmEndPoint=" + cxmEndPoint);
            context.Logger.LogLine("cxmAPIKey=" + cxmAPIKey);
            context.Logger.LogLine("cxmUser=" + cxmUser);
            context.Logger.LogLine("cxmPassword=" + cxmPassword);

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(cxmEndPoint);
            string requestParameters = "key=" + cxmAPIKey;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/api/service-api/norbert/sign-in" + "?" + requestParameters);
            request.Content = new StringContent("{\"username\":\"" + cxmUser + "\",\"password\":\"" + cxmPassword + "\"}", Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.SendAsync(request).Result;
            if (response.IsSuccessStatusCode)
            {
                HttpContent responseContent = response.Content;
                String responseString = responseContent.ReadAsStringAsync().Result;
                cxmResponse cxmData = JsonConvert.DeserializeObject<cxmResponse>(responseString);
                cxmToken = cxmData.token;
                context.Logger.LogLine("SetToken Method retrieved CXM token : " + cxmData.token);
            }
            else
            {
                context.Logger.LogLine("SetToken Method unable to retrieve CXM token");
            }

            String reference = "";
            String receipt = "";
            String amount = "";
            String responseCode = "";
            String responseDescription = "";
            String caseType = "";

            String cboPayment = "";
            String caseRef = "";
            String txtAddress = "";

            try
            {
                reference = Request.QueryStringParameters["CallingApplicationTransactionReference"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("CallingApplicationTransactionReference not found");
            }

            try
            {
                receipt = Request.QueryStringParameters["IncomeManagementReceiptNumber"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("IncomeManagementReceiptNumber");
            }

            try
            {
                amount = Request.QueryStringParameters["PaymentAmount"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("PaymentAmount");
            }

            try
            {
                responseCode = Request.QueryStringParameters["ResponseCode"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("ResponseCode");
            }

            try
            {
                responseDescription = Request.QueryStringParameters["ResponseDescription"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("ResponseDescription");
            }

            try
            {
                caseType = Request.QueryStringParameters["caseType"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("caseType");
            }

            try
            {
                cboPayment = Request.QueryStringParameters["cboPayment"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("cboPayment");
            }
            try
            {
                caseRef = Request.QueryStringParameters["caseref"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("caseRef");
            }
            try
            {
                txtAddress = Request.QueryStringParameters["txtAddress"];
            }
            catch (Exception)
            {
                context.Logger.LogLine("txtAddress");
            }

            String transition = "";

            if (responseCode.Equals("00000"))
            {
                paymentReference = receipt;
                transition = "confirm-payment-received";
            }
            else
            {
                paymentReference = responseDescription;
                transition = "payment-not-accepted";
            }

            context.Logger.LogLine("Updating case with payment reference of "  + paymentReference);

            string data = "{\"" + "payment-reference" + "\":\"" + paymentReference + "\"}";
            string url = cxmEndPoint + "/api/service-api/norbert/case/" + reference + "/edit?key=" + cxmAPIKey + "&token=" + cxmToken;
            Encoding encoding = Encoding.Default;
            HttpWebRequest updateRequest = (HttpWebRequest)WebRequest.Create(url);
            updateRequest.Method = "PATCH";
            updateRequest.ContentType = "application/json; charset=utf-8";
            byte[] buffer = encoding.GetBytes(data);
            Stream dataStream = updateRequest.GetRequestStream();
            dataStream.Write(buffer, 0, buffer.Length);
            dataStream.Close();
            try
            {
                HttpWebResponse updateResponse = (HttpWebResponse)updateRequest.GetResponse();
                string result = "";
                using (StreamReader reader = new StreamReader(updateResponse.GetResponseStream(), System.Text.Encoding.Default))
                {
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception error)
            {
                context.Logger.LogLine(reference + " : " + error.ToString());
                context.Logger.LogLine(reference + " : Error updating CXM field " + "payment-reference" + " with message : " + paymentReference);
            }

            client = new HttpClient();
            client.BaseAddress = new Uri(cxmEndPoint);
            requestParameters = "key=" + cxmAPIKey + "&token=" + cxmToken;
            request = new HttpRequestMessage(HttpMethod.Post, "/api/service-api/norbert/case/" + reference + "/transition/" + transition + "?" + requestParameters);
            try
            {
                HttpResponseMessage cxmResponse = client.SendAsync(request).Result;
                if (!cxmResponse.IsSuccessStatusCode)
                {
                    context.Logger.LogLine(caseRef + " - Unable to transition case to : " + transition + "(" + cxmResponse.StatusCode + ")");
                }
                else
                {
                    context.Logger.LogLine(caseRef + " - Transitioned to : " + transition);
                }
            }
            catch(Exception error)
            {
                context.Logger.LogLine(caseRef + " - Unable to call transition to : " + transition);
            }

            String queryString = "CallingApplicationTransactionReference=" + reference +
                                 "&IncomeManagementReceiptNumber=" + receipt +
                                 "&PaymentAmount=" + amount +
                                 "&ResponseCode=" + responseCode +
                                 "&ResponseDescription=" + responseDescription +
                                 "&caseType=" + reference +
                                 "&cboPayment=" + cboPayment +
                                 "&caseRef=" + caseRef +
                                 "&txtAddress=" + txtAddress;
                                 ;

            APIGatewayProxyResponse apiResponse = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Redirect,
                //Body = JsonConvert.SerializeObject(temp),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Location", paymentConfirmation+"?"+queryString } }
            };
            return apiResponse;
        }
    }

    public class cxmResponse
    {
        public int id { get; set; }
        public String created_at { get; set; }
        public String token { get; set; }
    }
}
