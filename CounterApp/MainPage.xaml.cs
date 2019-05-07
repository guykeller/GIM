using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using System.Windows.Input;

namespace CounterApp
{
    public partial class MainPage : ContentPage
    {
        public static MainPage Current;
        SignalRConnection SignalRCon_object;

        public MainPage()
        {
            InitializeComponent();
            Current = this;
            SignalRConnection SignalRCon_object = new SignalRConnection();
            SignalRCon_object.initiateConnection();
            //when "updateCounter" hub function will be used, the Counter property will be updated accordingly.
            SignalRCon_object.connection.On<int>("UpdateCounter", (countValue) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                { 
                ((MainViewModel)this.BindingContext).Counters[0].Counter = countValue;
                });
            });

            SignalRCon_object.connection.StartAsync();
            int x = -1;
            if (SignalRCon_object.connection.State.Equals(HubConnectionState.Connected))
                x = 1;
            else
                x = 0;
            //    SignalRConnection.signalR_GetCounter();
        }

        public async void signalR_UpdateCounter(int counter)
        {
            int incrementedValue = counter + 1;
            try
            {
                await SignalRCon_object.connection.InvokeAsync("UpdateCounter", incrementedValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error: " + ex.Message);
            }
        }
    }
}
