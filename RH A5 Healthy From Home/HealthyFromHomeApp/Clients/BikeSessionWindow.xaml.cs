
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BikeLibrary;
using Client.Virtual_Reality;
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

        public string speed = String.Empty;
        public string distance_Traveled = String.Empty;
        public string elapsed_Time = String.Empty;
        public string accumulated_Power = String.Empty;
        public string instantaneous_Power = String.Empty;
        private bool isReceivingHeartRateData = false;

        private bool isConnectedToVRServer = false;
        private int sendToVRCounter = 0;
        private bool sendToVRServerOnce = false; 

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
            isConnectedToVRServer = VRServer.IsConnected();
            if (isReceivingData)
            {
                sendToVRServerOnce = true; // If we stop the session, update the VRServer speed once
                try
                {
                    // Decode the incoming bike data
                    List<(string, int)> decodedData = DataDecoder.Decode(bikeData);
                    if (decodedData[4].Item2 == 16)
                    {
                        int elapsed_TimeInt = decodedData[6].Item2 / 4;
                        elapsed_Time = elapsed_TimeInt.ToString();
                        distance_Traveled = decodedData[7].Item2.ToString();
                        speed = decodedData[10].Item2.ToString();

                        sendToVRCounter++;
                        if (sendToVRCounter == 4)
                        {
                            double newSpeed = double.Parse(speed);
                            if (isConnectedToVRServer)
                            {
                                VRServer.UpdateSpeed(newSpeed);
                            }
                            sendToVRCounter = 0;
                        }
                    }
                    if (decodedData[4].Item2 == 25)
                    {
                        accumulated_Power = decodedData[9].Item2.ToString();
                        instantaneous_Power = decodedData[13].Item2.ToString();
                    }
                } catch (Exception e)
                {

                }

                string decodedString = $"Current Measurements:\n" +
                    $"Speed: {speed} km/h \n" +
                    $"Distance Traveled: {distance_Traveled} m\n" +
                    $"Elapsed Time: {elapsed_Time} sec\n" +
                    $"Accumulated Power: {accumulated_Power} Watt\n" +
                    $"Instantanious Power: {instantaneous_Power} Watt";
                //string decodedString = DataDecoder.MakeString(decodedData);

                // Update the UI with the decoded data, running on the UI thread using the Dispatcher
                Dispatcher.Invoke(() =>
                {
                    TxtBikeData.Clear();
                    TxtBikeData.AppendText($"{decodedString}"); // Append the decoded data to the TextBox
                    TxtBikeData.ScrollToEnd();
                });

                string prefixedData = $"bike_data:{clientName}:{decodedString}";
                SendDataToServer(prefixedData);
            } 
            else
            {
                if (sendToVRServerOnce) // Stops movement in the NetworkEngine
                {
                    VRServer.UpdateSpeed(0.0);
                    sendToVRServerOnce = false; // When we start the session we can update the speed once again
                }
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

            if (isReceivingHeartRateData)
            {
                List<(string, int)> decodedData = DataDecoder.Decode(data);
                for (int i = 0; i < decodedData.Count; i++)
                {
                    if (decodedData[i].Item1 == "HeartRate")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            TxtHeartrateData.Clear();
                            TxtHeartrateData.Text = decodedData[i].Item2.ToString();
                        });
                    }
                }
            }
        }

        private async void HeartRateButton_Click(object sender, RoutedEventArgs e)
        {
            await bikeHelper.ConnectToHeartRateMonitor("Decathlon Dual HR");
            isReceivingHeartRateData = true;

        }
    }
}