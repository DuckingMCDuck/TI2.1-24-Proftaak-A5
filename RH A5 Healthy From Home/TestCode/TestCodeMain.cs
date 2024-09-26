using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TestCodeMain
    {

        public static void Main(string[] args)
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
