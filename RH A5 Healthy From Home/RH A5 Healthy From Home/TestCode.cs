using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RH_A5_Healthy_From_Home
{
    public class TestCode
    {
       public static void Main(string[] args)
        {
            string data = "A4 09 4E 05 19 19 F6 A0 7A 0C FF 34 8A";
            List<(string, int)> decodedData = DataDecode.Decode(data) ;
            Console.WriteLine(DataDecode.MakeString(decodedData));


        }
    }
}
