using System;
using Common.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using WebServices.Interfaces;
using WebServices.Models;

namespace WebServices
{
    public class TransloadWS : ITransloadWS
    {
        private readonly WebServiceSettings settings;
        private static string baseUrl;
        private static string user;
        private static string pass;

        public TransloadWS(IOptions<WebServiceSettings> config)
        {
            settings = config.Value;
        }

        public void Connect()
        {
            baseUrl = settings.WebServiceURL;
            user = settings.WSUserName;
            pass = settings.WSPassword;
            Console.WriteLine($"Base URL: {baseUrl}");
            Console.WriteLine($"UserName: {user}");
            Console.WriteLine($"Password: {pass}");
        }

        private RestClient getUrl(int transferOrderId)
        {
            try
            {
                var client = new RestClient(baseUrl + transferOrderId)
                {
                    Authenticator = new HttpBasicAuthenticator(user, pass)
                };

                return client;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool CheckAuditNumber(int transferOrderId)
        {
            var client = getUrl(transferOrderId);
            var request = new RestRequest(Method.GET);
            request.RequestFormat = DataFormat.Json;

            try
            {
                var response = client.Execute(request);
                if (response.ErrorMessage == null)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public TransferOrderModel getTransferOrder(int Id)
        {
            var client = getUrl(Id);
            var request = new RestRequest(Method.GET);
            request.RequestFormat = DataFormat.Json;


            try
            {
                var response = client.Execute(request);

                if (response.ErrorMessage == null)
                {
                    var model = JsonConvert.DeserializeObject<TransferOrderModel>(response.Content);

                    return model;
                }
                else
                {
                    //scaleServiceEvents.WriteEntry($"There was an error returning the transfer order from the web service: {response.ErrorMessage}", EventLogEntryType.Error);
                    return null;
                }



            }
            catch (Exception e)
            {
                //scaleServiceEvents.WriteEntry($"Exception occurred retrieving transfer order id {Id}", EventLogEntryType.Error);
                return null;
            }

        }
    }
}
