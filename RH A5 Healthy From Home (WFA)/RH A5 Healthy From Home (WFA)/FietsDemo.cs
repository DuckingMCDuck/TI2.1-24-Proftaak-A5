//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Avans.TI.BLE;

//namespace RH_A5_Healthy_From_Home
//{
//    class FietsDemo
//    {

//        public static async Task Main(string[] args)
//        {
//            int errorCode = 0;
//            BLE bleBike = new BLE();
//            BLE bleHeart = new BLE();
//            Thread.Sleep(1000); // We need some time to list available devices

//            // List available devices
//            List<String> bleBikeList = bleBike.ListDevices();
//            Console.WriteLine("Devices found: ");
//            foreach (var name in bleBikeList)
//            {
//                Console.WriteLine($"Device: {name}");
//            }

//            // Connecting
//            errorCode = errorCode = await bleBike.OpenDevice("Tacx Flux 00438");
//            // __TODO__ Error check

//            var services = bleBike.GetServices;
//            foreach (var service in services)
//            {
//                Console.WriteLine($"Service: {service}");
//            }

//            // Set service
//            errorCode = await bleBike.SetService("6e40fec1-b5a3-f393-e0a9-e50e24dcca9e");
//            // __TODO__ error check

//            // Subscribe
//            bleBike.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
//            errorCode = await bleBike.SubscribeToCharacteristic("6e40fec2-b5a3-f393-e0a9-e50e24dcca9e");

//            // Heart rate
//            errorCode = await bleHeart.OpenDevice("Decathlon Dual HR");

//            await bleHeart.SetService("HeartRate");

//            bleHeart.SubscriptionValueChanged += BleBike_SubscriptionValueChanged;
//            await bleHeart.SubscribeToCharacteristic("HeartRateMeasurement");


//            Console.Read();
//        }

//        public static void BleBike_SubscriptionValueChanged(object sender, BLESubscriptionValueChangedEventArgs e)
//        {
//            Console.WriteLine("Received from {0}: {1}, {2}", e.ServiceName,
//                BitConverter.ToString(e.Data).Replace("-", " "),
//                Encoding.UTF8.GetString(e.Data));
//            DataDecode.Decode(Encoding.UTF8.GetString(e.Data));
//        }

//    }
//}

