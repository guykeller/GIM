using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace GIMFunctions
{
    public static class Function1
    {
        private const string c_eventHubConnectionString = "eventHubConnectionString";
        private const string c_tableName = "GIMTable";
        private const string c_eventHubName = "gimeventhubinstance";
        private const string c_signalRConnectionString = "signalRConnectionString";

        private static readonly AzureSignalR SignalR = new AzureSignalR(
            Environment.GetEnvironmentVariable(c_signalRConnectionString));

        [FunctionName("negotiate")]
        public static async Task<SignalRConnectionInfo> NegotiateConnection(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage request,
            ILogger log)
        {
            try
            {
                ConnectionRequest connectionRequest = await ExtractContent<ConnectionRequest>(request);
                log.LogInformation($"Negotiating connection for user: <{connectionRequest.UserId}>.");

                string clientHubUrl = SignalR.GetClientHubUrl(c_eventHubName);
                string accessToken = SignalR.GenerateAccessToken(clientHubUrl, connectionRequest.UserId);
                return new SignalRConnectionInfo { AccessToken = accessToken, Url = clientHubUrl };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to negotiate connection.");
                throw;
            }
        }

        [FunctionName("MessageReceiver")]
        public static async Task MessageReceiver(
            [EventHubTrigger(c_eventHubName, Connection = c_eventHubConnectionString)]EventData message,
            [Table(c_tableName)] CloudTable cloudTable,
            [SignalR(HubName = c_eventHubName, ConnectionStringSetting = c_signalRConnectionString)] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Incoming request to {nameof(MessageReceiver)}");

                var counterProvider = new CounterProvider();

                // get counter
                var counter = await counterProvider.GetCounter(cloudTable, "counter");

                // update counter value
                counter.Count++;

                log.LogInformation($"New counter value {counter.Count}");

                // write counter to table
                var updateTask = counterProvider.UpdateCounter(cloudTable, counter);

                // update signal r
                var sendMessageTask = SendSignlRMessage(signalRMessages, counter.ToDTO());

                await Task.WhenAll(updateTask, sendMessageTask);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Exception in {nameof(MessageReceiver)}: {ex.Message}");
            }
        }

        [FunctionName("UpdateCounter")]
        public static async Task<HttpResponseMessage> UpdateCounter(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequestMessage req,
            [Table(c_tableName)] CloudTable cloudTable,
            [SignalR(HubName = c_eventHubName, ConnectionStringSetting = c_signalRConnectionString)] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Incoming request to {nameof(UpdateCounter)}");

                var dto = await ExtractContent<CounterDTO>(req);
                var counter = (Counter)dto;

                var counterProvider = new CounterProvider();

                // write counter to table
                var updateTask = counterProvider.UpdateCounter(cloudTable, counter);

                // update signal r
                var sendMessageTask = SendSignlRMessage(signalRMessages, counter.ToDTO());

                await Task.WhenAll(updateTask, sendMessageTask);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Exception in {nameof(UpdateCounter)}: {ex.Message}");

                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [FunctionName("GetCounter")]
        public static async Task<HttpResponseMessage> GetCounter(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequestMessage req,
            [Table(c_tableName)] CloudTable cloudTable,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Incoming request to {nameof(GetCounter)}");

                var counterProvider = new CounterProvider();

                var id = req.RequestUri.ParseQueryString()["id"];
                // write counter to table
                var counter = await counterProvider.GetCounter(cloudTable, id);

                return req.CreateResponse(HttpStatusCode.OK, counter.ToDTO());
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Exception in {nameof(GetCounter)}: {ex.Message}");

                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<T> ExtractContent<T>(HttpRequestMessage request)
        {
            string connectionRequestJson = await request.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(connectionRequestJson);
        }

        private static async Task SendSignlRMessage<T>(IAsyncCollector<SignalRMessage> signalRMessages, T data)
        {
            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "CounterUpdate",
                    Arguments = new object[] { data }
                });

        }
    }
}
