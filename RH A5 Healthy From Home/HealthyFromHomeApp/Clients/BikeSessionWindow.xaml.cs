
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using BikeLibrary;

namespace HealthyFromHomeApp.Clients
{
    public partial class BikeSessionWindow : Window
    {
        private BikeHelper bikeHelper;
        private bool isReceivingData;
        private DataDecoder decoder;

        public string speed = String.Empty;
        public string distance_Traveled = String.Empty;
        public string elapsed_Time = String.Empty;
        public string accumulated_Power = String.Empty;
        public string instantaneous_Power = String.Empty;
        private bool isReceivingHeartRateData = false;

        // Constructor 
        public BikeSessionWindow(BikeHelper bikeHelper)
        {
            InitializeComponent();
            this.bikeHelper = bikeHelper;
            this.bikeHelper.OnBikeDataReceived += OnBikeDataReceived;
            this.decoder = new DataDecoder();
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
            }

            if (isReceivingHeartRateData)
            {
                List<(string, int)> decodedData = DataDecoder.Decode(bikeData);
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