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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //string data = "Value changed for 6e40fec2-b5a3-f393-e0a9-e50e24dcca9e: A4 09 4E 05 10 19 F6 2E 7A 0C FF 34 8A";
            //DataDecode.Decode(data);
            Simulator simulator = new Simulator();
            simulator.StartSimulation();
            //while (true)
            //{

            //}
        }
    }
}
