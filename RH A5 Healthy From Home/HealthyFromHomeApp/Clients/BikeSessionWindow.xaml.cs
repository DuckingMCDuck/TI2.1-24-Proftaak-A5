using System.Threading.Tasks;
using System.Windows;

namespace HealthyFromHomeApp.Clients
{
    public partial class BikeSessionWindow : Window
    {
        private Simulator simulator;
        private bool isReceivingData;

        public BikeSessionWindow(Simulator simulator)
        {
            InitializeComponent();
            this.simulator = simulator;
        }

        private void BtnStartSession_Click(object sender, RoutedEventArgs e)
        {
            isReceivingData = true; 
            Task.Run(() => StartReceivingBikeData());
        }

        private void BtnStopSession_Click(object sender, RoutedEventArgs e)
        {
            isReceivingData = false;  
        }

        private async void StartReceivingBikeData()
        {
            while (isReceivingData)
            {
                string simulatedData = simulator.GenerateDataPage();

                var decodedData = DataDecoder.Decode(simulatedData);
                string decodedString = DataDecoder.MakeString(decodedData);

                await Dispatcher.InvokeAsync(() =>
                {
                    TxtBikeData.AppendText($"{decodedString}\n");
                    TxtBikeData.ScrollToEnd();
                });

                await Task.Delay(500);
            }

            await Dispatcher.InvokeAsync(() =>
            {
                TxtBikeData.AppendText("Session stopped.\n");
                TxtBikeData.ScrollToEnd();
            });
        }
    }
}
