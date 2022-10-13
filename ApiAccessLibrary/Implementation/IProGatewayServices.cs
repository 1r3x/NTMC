using ApiAccessLibrary.ApiModels;
using ApiAccessLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using EntityModelLibrary.ViewModels;
using Microsoft.Extensions.Options;

namespace ApiAccessLibrary.Implementation
{
    public class IProGatewayServices : IiProGatewayServices
    {
        private readonly IOptions<CentralizeVariablesModel> _centralizeVariablesModel;

        public IProGatewayServices(IOptions<CentralizeVariablesModel> centralizeVariablesModel)
        {
            _centralizeVariablesModel = centralizeVariablesModel;
        }

        public Task<string> PostSaleIProGateway(SaleRequestModel model)
        {
            //setup variables
            string ccNumber = model.Card.CardNumber;
            string ccExp = model.Card.Expiration;
            string cvv = model.Card.CVN;
            string amount = Convert.ToString(model.Amount);
            String security_key = _centralizeVariablesModel.Value.IClassProCredentials.security_key;
            String firstname = model.Patient.FirstName;
            String lastname = model.Patient.LastName;


            String strPost = "security_key=" + security_key
               + "&firstname=" + firstname + "&lastname=" + lastname
               + "&payment=creditcard&type=sale"
               + "&amount=" + amount + "&ccnumber=" + ccNumber + "&ccexp=" + ccExp + "&cvv=" + cvv;



            return ReadHtmlPageAsync(_centralizeVariablesModel.Value.IClassProCredentials.BaseAddress, strPost);
        }

        public Task<string> PostInstallmentIProGateway(SaleRequestModel model)
        {

            //setup variables
            string ccNumber = model.Card.CardNumber;
            string ccExp = model.Card.Expiration;
            string cvv = model.Card.CVN;
            string amount = Convert.ToString(model.Amount);
            String security_key = _centralizeVariablesModel.Value.IClassProCredentials.security_key;
            String firstname = model.Patient.FirstName;
            String lastname = model.Patient.LastName;


            //String strPost = "security_key=" + security_key
            //   + "&firstname=" + firstname + "&lastname=" + lastname
            //   + "&payment=creditcard&type=sale&initiated_by=customer&stored_credential_indicator=stored"
            //   + "&amount=" + amount + "&ccnumber=" + ccNumber + "&ccexp=" + ccExp + "&cvv=" + cvv;
            string strPost = "security_key=6457Thfj624V5r7WUwc5v6a68Zsd6YEm&" +
                "type=validate&" +
                //"billing_method=recurring&" +
                //"payment=creditcard&"+
                "initiated_by=customer&" +
                "stored_credential_indicator=stored&" +
                //"amount=132&" +
                "ccnumber=4111111111111111&" +
                "ccexp=04/25&" +
                "cvv=999";



            return ReadHtmlPageAsync(_centralizeVariablesModel.Value.IClassProCredentials.BaseAddress, strPost);
        }

        public Task<string> PostAuthIProGateway(SaleRequestModel model)
        {
            //setup variables
            string ccNumber = model.Card.CardNumber;
            string ccExp = model.Card.Expiration;
            string cvv = model.Card.CVN;
            string amount = Convert.ToString(model.Amount);
            String security_key = _centralizeVariablesModel.Value.IClassProCredentials.security_key;
            String firstname = model.Patient.FirstName;
            String lastname = model.Patient.LastName;


            String strPost = "security_key=" + security_key
               + "&firstname=" + firstname + "&lastname=" + lastname
               + "&payment=creditcard&type=auth"
               + "&amount=" + 1 + "&ccnumber=" + ccNumber + "&ccexp=" + ccExp + "&cvv=" + cvv;



            return ReadHtmlPageAsync(_centralizeVariablesModel.Value.IClassProCredentials.BaseAddress, strPost);
        }

        private static async Task<String> ReadHtmlPageAsync(string url, string post)
        {
            String result = "";
            String strPost = post;
            StreamWriter myWriter = null;

            HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(url);
            objRequest.Method = "POST";
            objRequest.ContentLength = strPost.Length;
            objRequest.ContentType = "application/x-www-form-urlencoded";

            try
            {
                myWriter = new StreamWriter(objRequest.GetRequestStream());
                myWriter.Write(strPost);
            }
            catch (Exception e)
            {
                return e.Message;
            }
            finally
            {
                myWriter.Close();
            }

            HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();
            using (StreamReader sr =
               new StreamReader(objResponse.GetResponseStream()))
            {
                result = await sr.ReadToEndAsync();

                // Close and clean up the StreamReader
                sr.Close();
            }
            return result;
        }

        public Task<string> PostSaleByTransactionIdIProGateway(SaleRequestModel model)
        {
            //setup variables
            string amount = Convert.ToString(model.Amount);
            String security_key = _centralizeVariablesModel.Value.IClassProCredentials.security_key;
            String firstname = model.Patient.FirstName;
            String lastname = model.Patient.LastName;
            string transactionId = model.PaymentMethodID;


            //String strPost = "security_key=" + security_key
            //   + "&type=sale" + "&initiated_by=merchant&stored_credential_indicator=used"
            //   + "&amount=" + amount + "&initial_transaction_id=" + transactionId;

            string strPost = "security_key=6457Thfj624V5r7WUwc5v6a68Zsd6YEm&" +
               "type=sale&" +
               //"billing_method=recurring&" +
               "initiated_by=merchant&" +
               "stored_credential_indicator=used&" +
               "initial_transaction_id="+transactionId;




            return ReadHtmlPageAsync(_centralizeVariablesModel.Value.IClassProCredentials.BaseAddress, strPost);
        }
    }
}
