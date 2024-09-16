using System;
using System.Text;
using System.Threading;

namespace FietsDemoRH_A5_Healthy_From_Home
{
    public class HexSimulator
    {
        static void Main(string[] args)
        {
            HexSimulator simulator = new HexSimulator();

            simulator.StartSimulation();
        }

        public void StartSimulation()
        {
            Random rand = new Random();

            while (true)
            {
                string fixedPrefix = "A4 09 4E 05";

                string dataPage = rand.Next(2) == 0 ? "10" : "19";

                string randomHexPart = GenerateRandomHex(12);

                string simulatedMessage = $"{fixedPrefix} {dataPage} {randomHexPart}";
                Console.WriteLine($"Value changed for 6e40fec2-b5a3-f393-e0a9-e50e24dcca9e: {simulatedMessage}");

                Thread.Sleep(250);
            }
        }

        private string GenerateRandomHex(int length)
        {
            Random rand = new Random();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length / 2; i++)
            {
                sb.Append(rand.Next(0, 256).ToString("X2")).Append(" ");
            }

            return sb.ToString().Trim();
        }
    }
}