using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace CounterApp
{
    class SignalRConnection
    {
        public HubConnection connection;

        public SignalRConnection()
        {
            connection = new HubConnectionBuilder()
            .WithUrl("https://gimfunctions.azurewebsites.net/api").Build();
        }

        public void initiateConnection()
        {
            //handle closed connection
            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
 //           connection.StartAsync();

        }

        public async void signalR_UpdateCounter(int counter)
        {
            int incrementedValue = counter + 1;
            try
            {
                await connection.InvokeAsync("UpdateCounter", incrementedValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error: "+ ex.Message);
            }
        }

        //public static async void signalR_GetCounter()
        //{
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("error: " + ex.Message);
        //    }
        //}
    }
}
