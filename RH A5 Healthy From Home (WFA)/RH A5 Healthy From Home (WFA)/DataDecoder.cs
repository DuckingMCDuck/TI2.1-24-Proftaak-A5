using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RH_A5_Healthy_From_Home
{
    public class DataDecode
    {

        public static List<(string, int)> listForToString = new List<(string, int)>();

        public static List<(string, int)> Decode(string data)
        {


            //try
            //{
                List<(string, int)> dataWithNames = new List<(string, int)>();
                List<int> difDataInt = new List<int>();
                string[] substrings1 = data.Split(':', ' ');
                //string[] substrings2 = data.Split(',', ' ');
                string chopedData = substrings1[1];

                string[] splitData = chopedData.Split(' ');
                for (int i = 0; i < splitData.Length; i++)
                {
                int decValue = Convert.ToInt32(splitData[i], 16);
                difDataInt.Add(decValue);
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
                Console.WriteLine(MakeString(dataWithNames));
                return dataWithNames;
            //catch (Exception e)
            //{
            //    Console.WriteLine("Not able to decode data");
            //    return null;
            //}
        }

        public static string MakeString(List<(string, int)> list)
        {
            try
            {
                string result = String.Empty;
            
            
            for (int i = 0; i < listForToString.Count; i++)
                {
                    result += $"{listForToString[i].Item1}: {listForToString[i].Item2} \n";
                }

                return result;
            }
            catch (Exception e)
            {
                return "Not able to make string";
            }
        }
    }

}
