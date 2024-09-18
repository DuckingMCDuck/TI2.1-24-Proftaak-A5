using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RH_A5_Healthy_From_Home
{
    public class DataDecode{

        public static List<(string, int)> listForToString = new List<(string, int)>();

        public static List<(string, int)> Decode(string data)
        {
            List<(string, int)> dataWithNames = new List<(string, int)>();
            List<int> difDataInt = new List<int>();
            string substring = "";
            for (int i = 0; i < data.Length; i++)
            {
                string charData = data.Substring(i, 1);

                if (charData != " ")
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
            if (difDataInt[4] == 16)
            {
                for (int i = 0; i < difDataInt.Count; i++)
                {
                    string name = Enum.GetName(typeof(DataNames16), i);
                    (string, int) tuple = (name, difDataInt[i]);
                    dataWithNames.Add(tuple);
                }
            }
            if (difDataInt[4] == 25)
            {
                for (int i = 0; i < difDataInt.Count; i++)
                {
                    string name = Enum.GetName(typeof(DataNames25), i);
                    (string, int) tuple = (name, difDataInt[i]);
                    dataWithNames.Add(tuple);
                }
            }
            listForToString = dataWithNames;
            return dataWithNames;
        }

        public static string MakeString(List<(string, int)> list)
        {
            string result = String.Empty;

            for (int i = 0; i < listForToString.Count; i++)
            {
                result += $"{listForToString[i].Item1}: {listForToString[i].Item2} \n";
            }

            return result;
        }
    }
}
