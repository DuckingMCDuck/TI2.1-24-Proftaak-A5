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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //is de read only box, hierin moet de chats weergegeven worden, chat logica moet nog wordnen toegevoegd.
            ChatReadOnly.ScrollToEnd();
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            //dit is de chatbar, hierin wordt wat getypt is verstuurd hoeft dus niks mee te gebeuren
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            //button voor het versturen van de tekst wat in de chatbar staat, en daarna cleared hij wat er in staat
            string message = ChatBar.Text;
            if (!string.IsNullOrEmpty(message)) { 
                ChatReadOnly.AppendText(message);// wissel deze statement om voor chat logica
                ChatBar.Clear();
            }
        }
    }
}
