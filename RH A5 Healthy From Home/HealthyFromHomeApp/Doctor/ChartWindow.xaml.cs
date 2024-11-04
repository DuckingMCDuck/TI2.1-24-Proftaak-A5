using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.Integration;
//using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Drawing;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;



namespace HealthyFromHomeApp.Doctor
{
    /// <summary>
    /// Interaction logic for ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window, INotifyPropertyChanged
    {

        //private Chart chart;
        //private Series heartRateSeries;
        //private Series speedSeries;
        //private Series distanceSeries;

        public Dictionary<string, ChartValues<double>> ClientSpeedValues { get; set; }
        public Dictionary<string, ChartValues<double>> ClientDistanceValues { get; set; }
        public SeriesCollection seriesCollection { get; set; }
        public ObservableCollection<string> labels { get; set; }
        public string selectedClient;
        public string SelectedClient
        {
            get
            {
                return selectedClient;
            }
            set
            {
                selectedClient = value;
                OnPropertyChanged(nameof(SelectedClient)); 
                UpdateChart(selectedClient);
            }
        }
        public ChartWindow()
        {
            InitializeComponent();
            //ClientSpeedValues = new Dictionary<string, ChartValues<double>>();
            //ClientDistanceValues = new Dictionary<string, ChartValues<double>>();
            //speedValues = new ChartValues<double>();
            //distanceValues = new ChartValues<double>();
            //DataContext = this;
            seriesCollection = new SeriesCollection() {
                new LineSeries
                {
                    Title = "Speed",
                    Values = new ChartValues<double>{ }
                },
                new LineSeries
                {
                    Title = "Distance",
                    Values = new ChartValues<double>{ }
                }
            };
            labels = new ObservableCollection<string>();
            ClientSpeedValues = new Dictionary<string, ChartValues<double>>(); 
            ClientDistanceValues = new Dictionary<string, ChartValues<double>>();
            DataContext = this;
        }

        public void testChart()
        {
            //    speedSeries.Points.AddXY(1, 10);
            //    speedSeries.Points.AddXY(2, 20);
            //    speedSeries.Points.AddXY(3, 30);
            //    speedSeries.Points.AddXY(4, 40);
            //    speedSeries.Points.AddXY(5, 50);
            //    distanceSeries.Points.AddXY(1, 5);
            //    distanceSeries.Points.AddXY(2, 10);
            //    distanceSeries.Points.AddXY(3, 15);
            //    distanceSeries.Points.AddXY(4, 20);
            //    distanceSeries.Points.AddXY(5, 25);
        }


        public void AppendBikeData(string clientName, string bikeData)
        {
            double speed = 0; double distance = 0; string elapsedTime = "";
            var dataParts = bikeData.Split('\n'); foreach (var part in dataParts)
            {
                if (part.Contains("speed")) 
                { 
                    speed = double.Parse(part.Split(' ')[1]); 
                }
                else if (part.Contains("distance")) 
                { 
                    distance = double.Parse(part.Split(' ')[1]); 
                }
                else if (part.Contains("elapsed time"))
                {
                    elapsedTime = part.Split(' ')[2];
                }
            }
            seriesCollection[0].Values.Add(speed); seriesCollection[1].Values.Add(distance);
            labels.Add(elapsedTime);

            if (!ClientSpeedValues.ContainsKey(clientName)) 
            { 
                ClientSpeedValues[clientName] = new ChartValues<double>(); 
            }
            if (!ClientDistanceValues.ContainsKey(clientName)) 
            { 
                ClientDistanceValues[clientName] = new ChartValues<double>(); 
            }
                ClientSpeedValues[clientName].Add(speed);
                ClientDistanceValues[clientName].Add(distance); 

            if (selectedClient == clientName)
            {
                seriesCollection[0].Values = ClientSpeedValues[clientName]; seriesCollection[1].Values = ClientDistanceValues[clientName];
                labels.Add(elapsedTime);
            }
        }



        //public void AppendBikeData(string clientName, string bikeData)
        //{

        //    var dataPart = bikeData.Split(',');
        //    foreach (var part in dataPart)
        //    {
        //        var keyValue = part.Split(':');
        //        if (keyValue.Length == 2)
        //        {
        //            var key = keyValue[0].Trim();
        //            Console.WriteLine("KEY:" + key);
        //            var value = keyValue[1].Trim().Split(' ')[0];
        //            if (double.TryParse(value, out double parsedValue))
        //            {
        //                if (key.Equals("speed", StringComparison.OrdinalIgnoreCase))
        //                {
        //                    if (!ClientSpeedValues.ContainsKey(clientName))
        //                    {
        //                        ClientSpeedValues[clientName] = new ChartValues<double>();
        //                    }
        //                    ClientSpeedValues[clientName].Add(parsedValue);
        //                }
        //                else if (key.Equals("distance", StringComparison.OrdinalIgnoreCase))
        //                {
        //                    if (!ClientDistanceValues.ContainsKey(clientName))
        //                    {
        //                        ClientDistanceValues[clientName] = new ChartValues<double>();
        //                    }
        //                    ClientDistanceValues[clientName].Add(parsedValue);
        //                }
        //            }
        //        }
        //    }


        //}

        public void UpdateChart(string clientName)
        {
            if (ClientSpeedValues.ContainsKey(clientName) && ClientDistanceValues.ContainsKey(clientName))
            {
                seriesCollection[0].Values = ClientSpeedValues[clientName];
                seriesCollection[1].Values = ClientDistanceValues[clientName];
                DataContext = this;
                Console.WriteLine("Chart updated for client: " + clientName);
            }
        }
        public event PropertyChangedEventHandler PropertyChanged; 
        protected void OnPropertyChanged(string propertyName) 
        { 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); 
        }
        public void ClearChart()
        {

        }

        private void ResetChartButton_Click(object sender, RoutedEventArgs e)
        {
            ClearChart();
        }

    }
}

