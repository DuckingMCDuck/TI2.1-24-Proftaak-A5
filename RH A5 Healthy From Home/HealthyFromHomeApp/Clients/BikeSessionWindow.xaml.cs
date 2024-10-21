using System.Threading.Tasks;
using System.Windows;

namespace HealthyFromHomeApp.Clients
{
    public partial class BikeSessionWindow : Window
    {
        private Simulator simulator;
        private bool isReceivingData;

        // Constructor 
        public BikeSessionWindow(Simulator simulator)
        {
            InitializeComponent();
            this.simulator = simulator; 
        }

        // Event handler for the Start Session button click event
        private void BtnStartSession_Click(object sender, RoutedEventArgs e)
        {
            isReceivingData = true; // Set the flag to true to start receiving data
            Task.Run(() => StartReceivingBikeData()); // Starts receiving data in seperate thread
        }

        // Event handler for the Stop Session button click event
        private void BtnStopSession_Click(object sender, RoutedEventArgs e)
        {
            isReceivingData = false; // Set the flag to false to stop receiving data
        }

        // Method that starts receiving simulated bike data from the simulator
        private async void StartReceivingBikeData()
        {
            // Loop continuously while data is being received
            while (isReceivingData)
            {
                // Generate data from the simulator (simulated bike data)
                string simulatedData = simulator.GenerateDataPage();

                // Decode the generated data to make it readable
                var decodedData = DataDecoder.Decode(simulatedData);
                string decodedString = DataDecoder.MakeString(decodedData);

                // Update the UI with the decoded data, running on the UI thread using the Dispatcher
                await Dispatcher.InvokeAsync(() =>
                {
                    TxtBikeData.AppendText($"{decodedString}\n"); // Append the decoded data to the TextBox
                    TxtBikeData.ScrollToEnd(); 
                });

                // Wait for 500 milliseconds before retrieving the next data page
                await Task.Delay(500);
            }

            // Once data receiving stops, update the UI to indicate that the session has ended
            await Dispatcher.InvokeAsync(() =>
            {
                TxtBikeData.AppendText("Session stopped.\n"); // Notify the user that the session is stopped
                TxtBikeData.ScrollToEnd(); 
            });
        }
    }
}
