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
            heartRateSeries.Label = "HeartRate";
            heartRateSeries.ChartType = SeriesChartType.Line;
            heartRateSeries.Color =System.Drawing.Color.Red;
            heartRateSeries.BorderWidth = 3;

            speedSeries = new Series("Speed");
            speedSeries.Label = "Speed";
            speedSeries.ChartType = SeriesChartType.Line;
            speedSeries.Color = System.Drawing.Color.Blue;
            speedSeries.BorderWidth = 3;

            distanceSeries = new Series("Afstand"); 
            distanceSeries.ChartType = SeriesChartType.Line; 
            distanceSeries.Color = System.Drawing.Color.Purple;
            distanceSeries.BorderWidth = 3;

            chart.Series.Add(heartRateSeries); 
            chart.Series.Add(speedSeries);
            chart.Series.Add(distanceSeries);

            testChart();
        }

        public void testChart() 
        {
            speedSeries.Points.AddXY(1, 10);
            speedSeries.Points.AddXY(2, 20);
            speedSeries.Points.AddXY(3, 30);
            speedSeries.Points.AddXY(4, 40);
            speedSeries.Points.AddXY(5, 50);
            distanceSeries.Points.AddXY(1, 5);
            distanceSeries.Points.AddXY(2, 10);
            distanceSeries.Points.AddXY(3, 15);
            distanceSeries.Points.AddXY(4, 20);
            distanceSeries.Points.AddXY(5, 25);
        }

        public void AppendBikeData(string clientName, string bikeData)
        {
            //foreach (string dataPart in dataParts)
            //{
            //    if (dataPart.Contains("Elapsed Time"))
            //    {
            //        string[] keyValue = dataPart.Split(':');
            //        elapsedTime = keyValue.Length > 1 ? keyValue[1].Trim() : string.Empty;
            //    }
            //    else if (dataPart.Contains("Speed"))
            //    {
            //        string[] keyValue = dataPart.Split(':');
            //        if (keyValue.Length > 1)
            //        {
            //            double waarde = double.Parse(keyValue[1].Trim());
            //            speedSeries.Points.AddXY(elapsedTime, waarde);
            //        }
            //    }
            //    else if (dataPart.Contains("Distance Traveled"))
            //    {
            //        string[] keyValue = dataPart.Split(':');
            //        if (keyValue.Length > 1)
            //        {
            //            double waarde = double.Parse(keyValue[1].Trim());
            //            distanceSeries.Points.AddXY(elapsedTime, waarde);
            //        }
            //    }
            //}
            Task.Run(() =>
            {
                string[] dataParts = bikeData.Split('\n');
                string elapsedTime = string.Empty;

                //foreach (string dataPart in dataParts)
                //{
                //if (dataPart.Contains("Elapsed Time"))
                //{
                //    string[] keyValue = dataPart.Split(':');
                //    elapsedTime = keyValue.Length > 1 ? keyValue[1].Trim() : string.Empty;
                //}
                // if (dataPart.Contains("Speed"))
                //{
                //    string[] keyValue = dataPart.Split(':');
                //    if (keyValue.Length > 1)
                //    {
                //        double waarde = double.Parse(keyValue[1].Replace(" km/h", "").Trim());
                //        speedSeries.Points.AddXY(elapsedTime, waarde);
                //    }
                //}

                //}
                for (int i = 0; i < dataParts.Length; i++)
                {
                    string dataPart = dataParts[i]; 
                    if (dataPart.Contains("Speed"))
                    {
                        string[] keyValue = dataPart.Split(':'); if (keyValue.Length > 1)
                        {
                            /*
                             "Speed: 25 km/h": After splitting by :, keyValue will be ["Speed", " 25 km/h"]. The length is 2, so keyValue[1].Trim() returns "25 km/h".
                        "Speed:": After splitting by :, keyValue will be ["Speed", ""]. The length is 2, so keyValue[1].Trim() returns an empty string.
                        "Speed": After splitting by :, keyValue will be ["Speed"]. The length is 1, so the operator returns string.Empty.
                             */
                            double waarde = double.Parse(keyValue[1].Replace(" km/h", "").Trim());
                            speedSeries.Points.AddXY(i, waarde);
                            
                        }
                    }
                }
            });

        }
        public async Task LoadDataFromFileAsync(string clientName)
        {
            string filePath = $"{clientName}_data.txt";
            string fileOfsimulation = "simulatedData.txt";

            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    if (reader.Peek() == -1)
                    {
                        if (File.Exists(fileOfsimulation))
                        {
                            using (StreamReader simReader = new StreamReader(fileOfsimulation))
                            {
                                while ((line = await simReader.ReadLineAsync()) != null)
                                {
                                    AppendBikeData(clientName, line);
                                }
                            }
                        }
                    }
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        AppendBikeData(clientName, line);
                    }
                }
            }
            else
            {
                using (StreamReader simReader = new StreamReader(fileOfsimulation))
                {
                    string line;
                    while ((line = await simReader.ReadLineAsync()) != null)
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
