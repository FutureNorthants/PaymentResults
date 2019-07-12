using System;
using System.Collections.Generic;
using System.Net;
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
            String reference = Request.QueryStringParameters["CallingApplicationTransactionReference"];
            String receipt = Request.QueryStringParameters["IncomeManagementReceiptNumber"];
            String amount = Request.QueryStringParameters["PaymentAmount"];
            String responseCode = Request.QueryStringParameters["ResponseCode"];
            String responseDescription = Request.QueryStringParameters["ResponseDescription"];
            String caseType = Request.QueryStringParameters["caseType"];

            String cboPayment = Request.QueryStringParameters["cboPayment"];
            String caseRef = Request.QueryStringParameters["caseRef"];
            String txtAddress = Request.QueryStringParameters["txtAddress"];

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

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Redirect,
                //Body = JsonConvert.SerializeObject(temp),
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Location", "https://mycouncil.northampton.digital/intranet/parkingPaymentConfirmation.html?"+queryString } }
            };
            return response;
        }
    }
}
