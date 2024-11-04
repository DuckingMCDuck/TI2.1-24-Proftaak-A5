
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
            this.bikeHelper.OnBikeDataReceived += async (data) => await OnBikeDataReceivedAsync(data);
            this.decoder = new DataDecoder();
            this.tcpClient = tcpClient;
            this.stream = tcpClient.GetStream();
            this.clientName = clientName;
            this.Closed += BikeSessionWindow_Closed;
            Dispatcher.Invoke(() =>
            {
                BtnStartSession.IsEnabled = true;
                BtnStopSession.IsEnabled = false;
            });
        }

        private void BikeSessionWindow_Closed(object sender, EventArgs e)
        {
            bikeHelper.BLE.CloseDevice();
        }

        public void StartSession()
        {
            isReceivingData = true;
            Dispatcher.Invoke(() =>
            {
                BtnStartSession.IsEnabled = false;
                BtnStopSession.IsEnabled = true;
            });
        }

        public void StopSession()
        {
            isReceivingData = false;
            Dispatcher.Invoke(() =>
            {
                BtnStartSession.IsEnabled = true;
                BtnStopSession.IsEnabled = false;
            });
        }

        // Event handler for the Start Session button click event
        private void BtnStartSession_Click(object sender, RoutedEventArgs e)
        {
            StartSession();
        }

        // Event handler for the Stop Session button click event
        private void BtnStopSession_Click(object sender, RoutedEventArgs e)
        {
            StopSession();
        }

        // Method that handles receiving real bike data from the bike
        private async Task OnBikeDataReceivedAsync(string bikeData)
        {
            isConnectedToVRServer = VRServer.IsConnected();
            if (isReceivingData)
            {
                sendToVRServerOnce = true; // If we stop the session, update the VRServer speed once
                try
                {
                    // Decode the incoming bike data
                    List<(string, int)> decodedData = await DataDecoder.Decode(bikeData);
                    if (decodedData != null)
                    {
                        // Process heart rate data if flag is set
                        if (isReceivingHeartRateData)
                        {
                            for (int i = 0; i < decodedData.Count; i++)
                            {
                                {
                                if (decodedData[i].Item1 == "HeartRateFromMonitor")
                                    Dispatcher.Invoke(() =>
                                    {
                                        TxtHeartrateData.Clear();
                                        TxtHeartrateData.Text = decodedData[i].Item2.ToString();
                                    });
                                    break;
                                }
                            }
                        }
                        // Process specific bike data parameters
                        if (decodedData[4].Item2 == 16) //Datapage 16
                        {
                            int elapsed_TimeInt = decodedData[6].Item2 / 4;
                            elapsed_Time = elapsed_TimeInt.ToString();
                            distance_Traveled = decodedData[7].Item2.ToString();
                            speed = decodedData[10].Item2.ToString();

                            // Update VR server speed periodically
                            sendToVRCounter++;
                            if (sendToVRCounter == 5)
                            {
                                double newSpeed = double.Parse(speed);
                                if (isConnectedToVRServer)
                                {
                                    VRServer.UpdateSpeed(newSpeed);
                                }
                                sendToVRCounter = 0;
                            }
                        }
                        else if (decodedData[4].Item2 == 25) //Datapage 25
                        {
                            accumulated_Power = decodedData[9].Item2.ToString();
                            instantaneous_Power = decodedData[13].Item2.ToString();
                        }
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

                Dispatcher.Invoke(() =>
                {
                    TxtBikeData.Clear();
                    TxtBikeData.AppendText($"{decodedString}"); // Append the decoded data to the TextBox
                    TxtBikeData.ScrollToEnd();
                });

                string prefixedData = $"bike_data:{clientName}:{decodedString}";
                SendDataToServer(prefixedData);
            }
            else if (isReceivingHeartRateData)
            {
                try
                {
                    // Decode the incoming bike data
                    List<(string, int)> decodedData = await DataDecoder.Decode(bikeData);
                    if (decodedData != null)
                    {
                        for (int i = 0; i < decodedData.Count; i++)
                        {
                            if (decodedData[i].Item1 == "HeartRateFromMonitor")
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    TxtHeartrateData.Clear();
                                    TxtHeartrateData.Text = decodedData[i].Item2.ToString();
                                });
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }
            else
            {
                if (sendToVRServerOnce) // Stops movement in the NetworkEngine & sends stop data
                {
                    VRServer.UpdateSpeed(0.0);
                    sendToVRServerOnce = false; // When we start the session we can update the speed once again
                }
            }
        }

        // Method to send data to the server securely
        private async void SendDataToServer(string data)
        {
            if (tcpClient.Connected)
            {
                try
                {
                    // Encrypt data before sending
                    string encryptedData = EncryptHelper.Encrypt(data);
                    byte[] dataBytes = Encoding.UTF8.GetBytes(encryptedData);
                    await stream.WriteAsync(dataBytes, 0, dataBytes.Length);
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    // Display error message in UI if data send fails
                    Dispatcher.Invoke(() =>
                    {
                        TxtBikeData.AppendText($"Error sending data to server: {ex.Message}\n");
                    });
                }
            }
        }

        // Event handler for the Heart Rate button to connect to the heart rate monitor
        private async void HeartRateButton_Click(object sender, RoutedEventArgs e)
        {
            isReceivingHeartRateData = await bikeHelper.ConnectToHeartRateMonitor("Decathlon Dual HR");
        }

        public async void startHeartRateMonitor()
        {
            isReceivingHeartRateData = await bikeHelper.ConnectToHeartRateMonitor("Decathlon Dual HR");
        }
    }
}