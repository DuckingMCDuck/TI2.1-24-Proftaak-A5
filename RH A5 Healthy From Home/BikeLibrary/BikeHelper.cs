using Avans.TI.BLE;
using BikeLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BikeLibrary
{
    public class BikeHelper
    {
        private BLE bleBike;
        private BLE bleHeart;

        public BikeHelper()
        {
            bleBike = new BLE();
            bleHeart = new BLE();
        }

        // List available devices
        public void ListAvailableDevices()
        {
            List<String> bleBikeList = bleBike.ListDevices();
            Console.WriteLine("Devices found: ");
            foreach (var name in bleBikeList)
            {
                Console.WriteLine($"Device: {name}");
            }
        }

        public List<string> GetAvailableDevices()
        {
            return bleBike.ListDevices();
        }

        // Connect to the bike
        public async Task<bool> ConnectToBike(string bikeName)
        {
            try
            {
                ListAvailableDevices();
                int errorCode = await bleBike.OpenDevice(bikeName);
                if (errorCode != 0)
                {
                    Console.WriteLine($"Can't connect to {bikeName}.");
                    return false;
                }

                // Set service for bike
                errorCode = await bleBike.SetService("6e40fec1-b5a3-f393-e0a9-e50e24dcca9e");
                if (errorCode != 0)
                {
                    Console.WriteLine("Failed to set bike service.");
                    return false;
                }

                // Subscribe to bike characteristic
                errorCode = await bleBike.SubscribeToCharacteristic("6e40fec2-b5a3-f393-e0a9-e50e24dcca9e");
                if (errorCode != 0)
                {
                    Console.WriteLine("Failed to subscribe to bike characteristic.");
                    return false;
                }

                bleBike.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while connecting to bike: {ex.Message}");
                return false;
            }
        }

        // Connect to the heart rate monitor
        public async Task<bool> ConnectToHeartRateMonitor(string hrMonitorName)
        {
            try
            {
                ListAvailableDevices();
                int errorCode = await bleHeart.OpenDevice(hrMonitorName);
                if (errorCode != 0)
                {
                    Console.WriteLine($"Can't connect to {hrMonitorName}.");
                    return false;
                }

                // Set service for heart rate monitor
                errorCode = await bleHeart.SetService("HeartRate");
                if (errorCode != 0)
                {
                    Console.WriteLine("Failed to set heart rate service.");
                    return false;
                }

                // Subscribe to heart rate characteristic
                errorCode = await bleHeart.SubscribeToCharacteristic("HeartRateMeasurement");
                if (errorCode != 0)
                {
                    Console.WriteLine("Failed to subscribe to heart rate characteristic.");
                    return false;
                }

                bleHeart.SubscriptionValueChanged += BleHeart_SubscriptionValueChanged;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while connecting to heart rate monitor: {ex.Message}");
                return false;
            }
        }

        private void BleBike_SubscriptionValueChanged(object sender, BLESubscriptionValueChangedEventArgs e)
        {
            string bikeData = BitConverter.ToString(e.Data).Replace("-", " ");
            Console.WriteLine($"Bike data received: {bikeData}");

            if (OnBikeDataReceived != null)
            {
                OnBikeDataReceived.Invoke(bikeData);
            }
            else
            {
                Console.WriteLine("Warning: No subscribers for OnBikeDataReceived event");
            }
        }

        // Event to notify subscribers when bike data is received
        public event Action<string> OnBikeDataReceived;

        private void BleHeart_SubscriptionValueChanged(object sender, BLESubscriptionValueChangedEventArgs e)
        {
            Console.WriteLine($"Heart rate data received: {BitConverter.ToString(e.Data).Replace("-", " ")}");
        }

        public static void SendDataToBike(BLE bike, int resistance)
        {
            Console.WriteLine("in methode SDTB");
            if (resistance < 1 || resistance > 200)
            {
                throw new ArgumentOutOfRangeException(nameof(resistance), "Must be between 1 and 200, (0,5% - 100%)");
            }
            byte resitanceByte = (byte)resistance;

            Console.WriteLine("updated resistance in byte: " + resitanceByte);

            byte[] bytes = { 0xA4, 0x09, 0x4E, 0x05, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, resitanceByte };

            byte checksum = 0x00;
            for (int i = 0; i < bytes.Length; i++)
            {
                checksum ^= bytes[i];
            }
            //adding checksum to end of byte array + resistance is updated to new value
            byte[] dataToSend = new byte[bytes.Length + 1];
            bytes.CopyTo(dataToSend, 0);
            dataToSend[dataToSend.Length - 1] = checksum;

            bike.WriteCharacteristic("6e40fec3-b5a3-f393-e0a9-e50e24dcca9e", dataToSend);
            Console.WriteLine("done SDTB");

        }
    }
}
