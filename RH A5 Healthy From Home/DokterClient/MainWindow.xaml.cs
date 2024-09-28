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

namespace DokterClient
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //is de read only box, hierin moet de chats weergegeven worden, chat logica moet nog wordnen toegevoegd.
            chatReadOnly.ScrollToEnd();
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            //hoeft in principe niks meee te gebeuren.
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = chatBar.Text;
            if (!string.IsNullOrEmpty(message))
            {
                chatReadOnly.AppendText(message); // wissel deze statement om voor chat logica
                chatBar.Clear();
            }
        }

        private void CmbClientsForDoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
