using BikeLibrary;
using System.Threading.Tasks;
using System.Windows;

namespace HealthyFromHomeApp.Clients
{
    public partial class BikeSessionWindow : Window
    {
        private BikeHelper bikeHelper;
        private bool isReceivingData;

        // Constructor 
        public BikeSessionWindow(Simulator simulator)
        {
            InitializeComponent();
            this.bikeHelper = bikeHelper;
            this.bikeHelper.OnBikeDataReceived += OnBikeDataReceived;
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
                // Update the UI with the received data, running on the UI thread using the Dispatcher
                Dispatcher.Invoke(() =>
                {
                    TxtBikeData.AppendText($"{bikeData}"); // Append the received bike data to the TextBox
                    TxtBikeData.ScrollToEnd();
                });
            }
        }
    }
}
