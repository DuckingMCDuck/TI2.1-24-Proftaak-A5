using System.Data.SqlTypes;
using System.Globalization;

namespace RH_A5_Healthy_From_Home
{
    public class DataDecode{

       public static List<int> Decode(string data)
        {
            List<int> difDataInt = new List<int>();
            string substring = "";
            for (int i = 0; i < data.Length; i++)
            {
                string charData = data.Substring(i, 1);
                
                if (charData != " " )
                {
                    substring += charData;
                }
                else if (charData == " ")
                {
                    int decValue = Convert.ToInt32(substring, 16);
                    difDataInt.Add(decValue);
                    substring = "";
                }
            }
            return difDataInt;
        }
    }
}
