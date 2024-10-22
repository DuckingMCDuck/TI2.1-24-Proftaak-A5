using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Client
{
    public class DataDecoder {

        public static List<(string, int)> listForToString = new List<(string, int)>();

        public static List<(string, int)> Decode(string data)
        {
            try
            {
                List<(string, int)> dataWithNames = new List<(string, int)>();
                List<int> difDataInt = new List<int>();

                string[] splitData = data.Split(' ');
                for (int i = 0; i < splitData.Length; i++)
                {
                    int decValue = Convert.ToInt32(splitData[i], 16);
                    difDataInt.Add(decValue);
                }
                if (difDataInt[4] == 16)
                {
                    int lsbValue = 0;
                    int msbValue = 0;
                    Boolean calcSpeed = false;
                    for (int i = 0; i < difDataInt.Count; i++)
                    {
                        string name = Enum.GetName(typeof(DataNames16), i);
                        if (name == "Speed_LSB")
                        {
                            lsbValue = difDataInt[i];
                        }
                        else if (name == "Speed_MSB" && lsbValue != 0)
                        {
                            msbValue = difDataInt[i];
                            calcSpeed = true;
                        }
                        (string, int) tuple = (name, difDataInt[i]);
                        dataWithNames.Add(tuple);
                        if (calcSpeed == true)
                        {
                            double speed = ((msbValue << 8) | lsbValue) / 1000 * 3.6;
                            (string, int) speedTuple = ("Speed", (int)Math.Round(speed));
                            dataWithNames.Add(speedTuple);
                            calcSpeed = false;
                        }
                    }
                }
                else if (difDataInt[4] == 25)
                {
                    for (int i = 0; i < difDataInt.Count; i++)
                    {
                        string name = Enum.GetName(typeof(DataNames25), i);
                        (string, int) tuple = (name, difDataInt[i]);
                        dataWithNames.Add(tuple);
                    }
                }
                else if (difDataInt.Count >= 1 && difDataInt.Count <= 10)
                {
                    string name = "Heartrate";
                    (string, int) tuple = (name, difDataInt[1]);
                    dataWithNames.Add(tuple);
                }
                listForToString = dataWithNames;
                MainWindow.client.debugText = MakeString(dataWithNames);
                return dataWithNames;
            }
            catch (Exception e)
            {
                MainWindow.client.debugText = "Not able to decode data\n";
                return null;
            }
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
