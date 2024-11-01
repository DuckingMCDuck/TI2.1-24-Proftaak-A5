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
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Drawing;


namespace HealthyFromHomeApp.Doctor
{
    /// <summary>
    /// Interaction logic for ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
       
        private Chart chart;
        private Series heartRateSeries;
        private Series speedSeries;
        private Series distanceSeries;

        public ChartWindow()
        {
            InitializeComponent();
            chart = new Chart();
            ChartArea chartArea = new ChartArea();
            chart.ChartAreas.Add(chartArea);
            windowsFormsHost.Child = chart;

            heartRateSeries = new Series("Heartrate");
            heartRateSeries.ChartType = SeriesChartType.Line;
            heartRateSeries.Color =System.Drawing.Color.Red;

            speedSeries = new Series("Speed");
            speedSeries.ChartType = SeriesChartType.Line;
            speedSeries.Color = System.Drawing.Color.Blue;

            distanceSeries = new Series("Afstand"); 
            distanceSeries.ChartType = SeriesChartType.Line; 
            distanceSeries.Color = System.Drawing.Color.Purple;

            chart.Series.Add(heartRateSeries); 
            chart.Series.Add(speedSeries);
            chart.Series.Add(distanceSeries);

        }
     
        public void AppendBikeData(string clientName, string bikeData)
        {
            //string[] dataParts = bikeData.Split(',');
            //foreach (string dataPart in dataParts)
            //{
            //    string[] keyValue = dataPart.Split(':');
            //    string tijd = keyValue[0];
            //    double waarde = double.Parse(keyValue[1]);
            //    if (dataPart.Contains("Hartslag"))
            //    {
            //        heartRateSeries.Points.AddXY(tijd, waarde);
            //    }
            //    else if (dataPart.Contains("Snelheid"))
            //    {
            //        speedSeries.Points.AddXY(tijd, waarde);
            //    }

            //} 

            string[] dataParts = bikeData.Split('\n');
            string elapsedTime = string.Empty;
            foreach (string dataPart in dataParts)
            {
                if (dataPart.Contains("Elapsed Time"))
                {
                    string[] keyValue = dataPart.Split(':');
                    elapsedTime = keyValue.Length > 1 ? keyValue[1].Trim() : string.Empty;
                }
                else if (dataPart.Contains("Speed"))
                {
                    string[] keyValue = dataPart.Split(':');
                    if (keyValue.Length > 1)
                    {
                        double waarde = double.Parse(keyValue[1].Trim());
                        speedSeries.Points.AddXY(elapsedTime, waarde);
                    }
                }
                else if (dataPart.Contains("Distance Traveled"))
                {
                    string[] keyValue = dataPart.Split(':');
                    if (keyValue.Length > 1)
                    {
                        double waarde = double.Parse(keyValue[1].Trim());
                        distanceSeries.Points.AddXY(elapsedTime, waarde);
                    }
                }

            }

        }
        public async Task LoadDataFromFileAsync(string clientName)
        {
            string filePath = $"{clientName}_data.txt";
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        AppendBikeData(clientName, line);
                    }
                }
            }
        }


        public void ClearChart()
        {
            heartRateSeries.Points.Clear();
            speedSeries.Points.Clear();
            distanceSeries.Points.Clear();
        }

        private void ResetChartButton_Click(object sender, RoutedEventArgs e)
        {
            ClearChart();
        }

    }
}
