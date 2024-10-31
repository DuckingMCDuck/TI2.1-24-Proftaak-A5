﻿
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BikeLibrary;
using HealthyFromHomeApp.Common;

namespace HealthyFromHomeApp.Clients
{
    public partial class BikeSessionWindow : Window
    {
        private BikeHelper bikeHelper;
        private bool isReceivingData;
        private DataDecoder decoder;

        private TcpClient tcpClient;
        private NetworkStream stream;

        private string clientName;

        // Constructor 
        public BikeSessionWindow(BikeHelper bikeHelper, TcpClient tcpClient, string clientName)
        {
            InitializeComponent();
            this.bikeHelper = bikeHelper;
            this.bikeHelper.OnBikeDataReceived += OnBikeDataReceived;
            this.decoder = new DataDecoder();
            this.tcpClient = tcpClient;
            this.stream = tcpClient.GetStream();
            this.clientName = clientName;
        }

        // Event handler for the Start Session button click event
        private void BtnStartSession_Click(object sender, RoutedEventArgs e)
        {
            isReceivingData = true; // Set the flag to true to start receiving data
        }

        // Event handler for the Stop Session button click event
        private void BtnStopSession_Click(object sender, RoutedEventArgs e)
        {
            isReceivingData = false; // Set the flag to false to stop receiving data
        }

        // Method that handles receiving real bike data from the bike
        private void OnBikeDataReceived(string bikeData)
        {
            if (isReceivingData)
            {
                // Decode the incoming bike data
                var decodedData = DataDecoder.Decode(bikeData);
                string decodedString = DataDecoder.MakeString(decodedData);

                // Update the UI with the decoded data, running on the UI thread using the Dispatcher
                Dispatcher.Invoke(() =>
                {
                    TxtBikeData.AppendText($"{decodedString}"); // Append the decoded data to the TextBox
                    TxtBikeData.ScrollToEnd();
                });

                string prefixedData = $"bike_data:{clientName}:{decodedString}";
                SendDataToServer(prefixedData);
            }
        }

        private async void SendDataToServer(string data)
        {
            if (tcpClient.Connected)
            {
                try
                {
                    string encryptedData = EncryptHelper.Encrypt(data);
                    byte[] dataBytes = Encoding.UTF8.GetBytes(encryptedData);
                    await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtBikeData.AppendText($"Error sending data to server: {ex.Message}\n");
                    });
                }
            }
        }
    }
}