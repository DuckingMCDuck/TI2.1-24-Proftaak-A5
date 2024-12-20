﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TestCode;

namespace HealthyFromHomeApp.Clients
{
    public class Simulator
    {
        private ClientMainWindow clientWindow;

        private int elapsedTime = 0;
        private int distanceTraveled = 0;
        private int updateEventCount = 0;
        private int heartRate = 80;
        private int instantaneousPower = 150;
        private int cadence = 80;
        private int dataPagePrintCount = 0;
        private int energyExpended = 0;

        public Simulator(ClientMainWindow clientWindow)
        {
            this.clientWindow = clientWindow;
        }

        public async Task SimulateData()
        {
            Random rand = new Random();

            string fixedPrefix = "A4 09 4E 05";
            string randomHexPart16;
            string randomHexPart25;

            randomHexPart16 = await GenerateDataPage16(rand);
            string simulatedMessage16 = $"{fixedPrefix} {randomHexPart16}";
            string result16 = $"{simulatedMessage16}";
            Debug.WriteLine("RESULT 16: " + result16);
            clientWindow.debugText = $"\n{result16}\n";
            List<(string, int)> list16 = await DataDecoder.DecodeAndSend(simulatedMessage16, clientWindow);
            WriteDataToFile(list16);

            randomHexPart25 = await GenerateDataPage25(rand);
            string simulatedMessage25 = $"{fixedPrefix} {randomHexPart25}";
            string result25 = $"{simulatedMessage25}";
            Debug.WriteLine("RESULT 25: " + result25);
            clientWindow.debugText = $"\n{result25}\n";
            List<(string, int)> list25 = await DataDecoder.DecodeAndSend(simulatedMessage25, clientWindow);
            WriteDataToFile(list25);

            if (dataPagePrintCount == 2)
            {
                string heartRateString = GenerateHeartRateString(rand);
                await DataDecoder.DecodeAndSend(heartRateString, clientWindow);
                dataPagePrintCount = 0;
            }

            dataPagePrintCount++;
        }

        public static void WriteDataToFile(List<(string, int)> list)
        {
            if (list.Count == 0)
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                switch (list[i].Item1)
                {
                    case "Elapsed_Time":
                        File.AppendAllText("simulatedData.txt", $"Elapsed Time: {(list[i].Item2 / 4).ToString()} sec\n");
                        break;
                    case "Distance_Traveled":
                        File.AppendAllText("simulatedData.txt", $"Distance Traveled: {list[i].Item2} m\n");
                        break;
                    case "Speed":
                        File.AppendAllText("simulatedData.txt", $"Speed: {list[i].Item2} km/h\n");
                        break;
                    case "Accumulated_Power":
                        File.AppendAllText("simulatedData.txt", $"Accumulated Power: {list[i].Item2} Watt\n");
                        break;
                    case "Instantaneous_Power":
                        File.AppendAllText("simulatedData.txt", $"Instantaneous Power: {list[i].Item2} Watt\n");
                        break;
                    default:
                        break;
                }
            }
        }

        public async Task<string> GenerateDataPage()
        {
            Random rand = new Random();
            StringBuilder sb = new StringBuilder();

            string fixedPrefix = "A4 09 4E 05";
            string randomHexPart = dataPagePrintCount % 2 == 0 ? await GenerateDataPage16(rand) : await GenerateDataPage25(rand);
            string simulatedMessage = $"{fixedPrefix} {randomHexPart}";

            dataPagePrintCount++;
            return simulatedMessage;
        }

        private Task<string> GenerateDataPage16(Random rand)
        {
            StringBuilder sb = new StringBuilder();

            // Byte 0: Data Page Number 
            sb.Append("10").Append(" ");

            // Byte 1: Equipment Type Bit Field 
            sb.Append("19").Append(" ");

            // Byte 2: Elapsed Time in increments of 0.25 seconds, rolls over at 64 seconds (256 * 0.25)
            elapsedTime = (elapsedTime + 1) % 256;
            sb.Append(elapsedTime.ToString("X2")).Append(" ");

            // Byte 3: Distance Traveled 
            distanceTraveled = (distanceTraveled + rand.Next(1, 5)) % 256;
            sb.Append(distanceTraveled.ToString("X2")).Append(" ");

            // Bytes 4-5: Speed LSB and MSB (speed in meters/second, 0.001 to 65.534 m/s)
            int speed = rand.Next(1000, 65535);
            int speedLSB = speed & 0xFF;
            int speedMSB = (speed >> 8) & 0xFF;
            sb.Append(speedLSB.ToString("X2")).Append(" ");
            sb.Append(speedMSB.ToString("X2")).Append(" ");

            // Byte 6: Heart Rate (0 to 254 bpm; 0xFF is invalid)
            int heartRate = rand.Next(60, 181);
            sb.Append(heartRate.ToString("X2")).Append(" ");

            // Byte 7: Random bits
            int capabilitiesAndState = rand.Next(0, 256);
            sb.Append(capabilitiesAndState.ToString("X2"));

            return Task.FromResult(sb.ToString());
        }

        private Task<string> GenerateDataPage25(Random rand)
        {
            StringBuilder sb = new StringBuilder();

            // Byte 0: Data Page Number 
            sb.Append("19").Append(" ");

            // Byte 1: Update Event Count (increments with each information update)
            updateEventCount = (updateEventCount + 1) % 256;
            sb.Append(updateEventCount.ToString("X2")).Append(" ");

            // Byte 2: Instantaneous Cadence 
            int cadence = rand.Next(60, 101);
            sb.Append(cadence.ToString("X2")).Append(" ");

            // Bytes 3-4: Accumulated Power in Watts
            int accumulatedPower = rand.Next(100, 65536);
            int accumulatedPowerLSB = accumulatedPower & 0xFF;
            int accumulatedPowerMSB = (accumulatedPower >> 8) & 0xFF;
            sb.Append(accumulatedPowerLSB.ToString("X2")).Append(" ");
            sb.Append(accumulatedPowerMSB.ToString("X2")).Append(" ");

            // Bytes 5-6.5: Instantaneous Power in Watts
            int instantaneousPower = rand.Next(0, 4095);
            int instantaneousPowerLSB = instantaneousPower & 0xFF;
            int instantaneousPowerMSN = (instantaneousPower >> 8) & 0x0F;
            sb.Append(instantaneousPowerLSB.ToString("X2")).Append(" ");
            sb.Append(instantaneousPowerMSN.ToString("X1")).Append("0 ");

            // Byte 7: Trainer Status Bit Field + Flags Bit Field + FE State Bit Field (Random in this case)
            int trainerStatus = rand.Next(0, 16);
            int targetPowerLimit = rand.Next(0, 4);
            int flagsAndStatus = (trainerStatus << 4) | targetPowerLimit;
            sb.Append(flagsAndStatus.ToString("X2"));

            return Task.FromResult(sb.ToString());
        }

        private string GenerateHeartRateString(Random rand)
        {
            // Gradual change
            heartRate += rand.Next(-1, 2);

            heartRate = Clamp(heartRate, 60, 180);

            // Increase energy expended over time
            energyExpended += rand.Next(1, 5);

            // Generate 1 or 2 RR intervals
            int rrInterval1 = rand.Next(600, 1000);
            int rrInterval2 = rand.Next(600, 1000);

            StringBuilder sb = new StringBuilder();
            sb.Append("16 ");
            sb.Append(heartRate.ToString("X2")).Append(" ");

            // Add Energy Expended (2 bytes)
            sb.Append((energyExpended & 0xFF).ToString("X2")).Append(" ");
            sb.Append(((energyExpended >> 8) & 0xFF).ToString("X2")).Append(" ");

            // Add RR-Interval 1 (2 bytes)
            sb.Append((rrInterval1 & 0xFF).ToString("X2")).Append(" ");
            sb.Append(((rrInterval1 >> 8) & 0xFF).ToString("X2")).Append(" ");

            // Add RR-Interval 2 (2 bytes)
            sb.Append((rrInterval2 & 0xFF).ToString("X2")).Append(" ");
            sb.Append(((rrInterval2 >> 8) & 0xFF).ToString("X2"));

            return sb.ToString();
        }

        /// <summary>
        /// Custom Clamp method, because Math.Clamp doesn't work
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
