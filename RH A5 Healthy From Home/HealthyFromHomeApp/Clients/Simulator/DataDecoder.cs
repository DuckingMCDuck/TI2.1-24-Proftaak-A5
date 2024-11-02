using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace HealthyFromHomeApp.Clients
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
                    int speedLsbValue = 0;
                    int speedMsbValue = 0;

                    Boolean calcSpeed = false;
                    for (int i = 0; i < difDataInt.Count; i++)
                    {
                        string name = Enum.GetName(typeof(DataNames16), i);
                        if (name == "Speed_LSB")
                        {
                            speedLsbValue = difDataInt[i];
                        }
                        if (name == "Speed_MSB")
                        {
                            speedMsbValue = difDataInt[i];
                            calcSpeed = true;
                        }
                        (string, int) tuple = (name, difDataInt[i]);
                        dataWithNames.Add(tuple);
                        if (calcSpeed == true)
                        {
                            double speed = ((speedMsbValue << 8) | speedLsbValue) / 1000 * 3.6;
                            (string, int) speedTuple = ("Speed", (int)Math.Round(speed));
                            dataWithNames.Add(speedTuple);
                            calcSpeed = false;
                        }
                    }
                }
                else if (difDataInt[4] == 25)
                {
                    int accumulatedLsbValue = 0;
                    int accumulatedMsbValue = 0;

                    int instantaneousLsbValue = 0;
                    int instantaneousMsbValue = 0;

                    Boolean calcAccumulatedPower = false;
                    Boolean calcInstantaneousPower = false;

                    for (int i = 0; i < difDataInt.Count; i++)
                    {
                        string name = Enum.GetName(typeof(DataNames25), i);

                        if (name == "Accumulated_Power_LSB")
                        {
                            accumulatedLsbValue = difDataInt[i];
                        }
                        if (name == "Accumulated_Power_MSB")
                        {
                            accumulatedMsbValue = difDataInt[i];
                            calcAccumulatedPower = true;
                        }


                        if (name == "Instantaneous_Power_LSB")
                        {
                            instantaneousLsbValue = difDataInt[i];
                        }

                        if (name == "Instantaneous_Power_MSB_and_Trainer_Status_Bit_Field")
                        {
                            string binaryString = Convert.ToString(difDataInt[i], 2);
                            int lengthMissing = 8 - binaryString.Length;
                            for (int x = 0; x < lengthMissing; x++)
                            {
                                binaryString = "0" + binaryString;
                            }
                            string powerPart = binaryString.Substring(4, 4);
                            instantaneousMsbValue = Convert.ToInt32(powerPart, 2);
                            string bitField = binaryString.Substring(0, 4);
                            int bitFieldInt = Convert.ToInt32(bitField, 2);

                            
                            (string, int) tuplePower = ("Instantaneous_Power_MSB", instantaneousMsbValue);
                            dataWithNames.Add(tuplePower);
                            (string, int) tupleBitField = ("Trainer_Status_Bit_Field", bitFieldInt);
                            dataWithNames.Add(tupleBitField);
                            calcInstantaneousPower = true;


                        }
                        else
                        {
                            (string, int) tuple = (name, difDataInt[i]);
                            dataWithNames.Add(tuple);
                        }
                        

                        if (calcAccumulatedPower)
                        {
                            double accumulatedPower = ((accumulatedMsbValue << 8) | accumulatedLsbValue);
                            (string, int) accumulatedPowerTuple = ("Accumulated_Power", (int)Math.Round(accumulatedPower));

                            dataWithNames.Add(accumulatedPowerTuple);

                            calcAccumulatedPower = false;
                        }


                        if (calcInstantaneousPower)
                        {
                            double instantaneousPower = ((instantaneousMsbValue << 8) | instantaneousLsbValue);
                            (string, int) instantaneousPowerTuple = ("Instantaneous_Power", (int)Math.Round(instantaneousPower));
                            dataWithNames.Add(instantaneousPowerTuple);
                            calcInstantaneousPower = false;
                        }
                        
                    }
                }
                else if (difDataInt.Count >= 1 && difDataInt.Count <= 10)
                {
                    string name = "Heartrate";
                    (string, int) tuple = (name, difDataInt[1]);
                    dataWithNames.Add(tuple);
                }
                listForToString = dataWithNames;
                return dataWithNames;
            }
            catch (Exception ex) 
            {
                return null;
            }
        }

        public static List<(string, int)> DecodeAndSend(string data, ClientMainWindow clientInstance)
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
                    int speedLsbValue = 0;
                    int speedMsbValue = 0;

                    Boolean calcSpeed = false;
                    for (int i = 0; i < difDataInt.Count; i++)
                    {
                        string name = Enum.GetName(typeof(DataNames16), i);
                        if (name == "Speed_LSB")
                        {
                            speedLsbValue = difDataInt[i];
                        }
                        if (name == "Speed_MSB")
                        {
                            speedMsbValue = difDataInt[i];
                            calcSpeed = true;
                        }
                        (string, int) tuple = (name, difDataInt[i]);
                        dataWithNames.Add(tuple);
                        if (calcSpeed == true)
                        {
                            double speed = ((speedMsbValue << 8) | speedLsbValue) / 1000 * 3.6;
                            (string, int) speedTuple = ("Speed", (int)Math.Round(speed));
                            dataWithNames.Add(speedTuple);
                            calcSpeed = false;
                        }
                    }
                }
                else if (difDataInt[4] == 25)
                {
                    int accumulatedLsbValue = 0;
                    int accumulatedMsbValue = 0;

                    int instantaneousLsbValue = 0;
                    int instantaneousValue = 0;

                    Boolean calcAccumulatedPower = false;
                    Boolean calcInstantaneousPower = false;

                    for (int i = 0; i < difDataInt.Count; i++)
                    {
                        string name = Enum.GetName(typeof(DataNames25), i);

                        if (name == "Accumulated_Power_LSB")
                        {
                            accumulatedLsbValue = difDataInt[i];
                        }
                        if (name == "Accumulated_Power_MSB")
                        {
                            accumulatedMsbValue = difDataInt[i];
                            calcAccumulatedPower = true;
                        }


                        if (name == "Instantaneous_Power_LSB")
                        {
                            instantaneousLsbValue = difDataInt[i];
                        }

                        if (name == "Instantaneous_Power_MSB_and_Trainer_Status_Bit_Field")
                        {
                            string binaryString = Convert.ToString(difDataInt[i], 2);
                            int lengthMissing = 8 - binaryString.Length;
                            for (int x = 0; x < lengthMissing; x++)
                            {
                                binaryString = "0" + binaryString;
                            }
                            string powerPart = binaryString.Substring(4, 4);
                            instantaneousValue = Convert.ToInt32(powerPart, 2);
                            string bitField = binaryString.Substring(0, 4);
                            int bitFieldInt = Convert.ToInt32(bitField, 2);


                            (string, int) tuplePower = ("Instantaneous_Power", instantaneousValue);
                            dataWithNames.Add(tuplePower);
                            (string, int) tupleBitField = ("Trainer_Status_Bit_Field", bitFieldInt);
                            dataWithNames.Add(tupleBitField);
                            calcInstantaneousPower = true;


                        }
                        else
                        {
                            (string, int) tuple = (name, difDataInt[i]);
                            dataWithNames.Add(tuple);
                        }


                        if (calcAccumulatedPower)
                        {
                            double accumulatedPower = ((accumulatedMsbValue << 8) | accumulatedLsbValue);
                            (string, int) accumulatedPowerTuple = ("Accumulated_Power", (int)Math.Round(accumulatedPower));

                            dataWithNames.Add(accumulatedPowerTuple);

                            calcAccumulatedPower = false;
                        }


                        if (calcInstantaneousPower)
                        {
                            double instantaneousPower = ((instantaneousValue << 8) | instantaneousLsbValue);
                            (string, int) instantaneousPowerTuple = ("Instantaneous_Power", (int)Math.Round(instantaneousPower));
                            dataWithNames.Add(instantaneousPowerTuple);
                            calcInstantaneousPower = false;
                        }

                    }
                }
                else if (difDataInt.Count >= 1 && difDataInt.Count <= 10)
                {
                    string name = "Heartrate";
                    (string, int) tuple = (name, difDataInt[1]);
                    dataWithNames.Add(tuple);
                }
                listForToString = dataWithNames;

                if (clientInstance != null)
                {
                    clientInstance.debugText = MakeString(dataWithNames);
                }

                return dataWithNames;
            }
            catch (Exception e)
            {
                if (clientInstance != null)
                {
                    clientInstance.debugText = "Not able to decode data\n";
                }
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
