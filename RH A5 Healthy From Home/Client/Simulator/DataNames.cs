using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    enum DataNames16
    {
        Sync,
        Msg_Lenght,
        Msg_ID,
        Channel_ID,
        Data_Page_Number,
        Equipment_Type_Bit_Field,
        Elapsed_Time,
        Distance_Traveled,
        Speed_LSB,
        Speed_MSB,
        HeartRate,
        Capabilities_Bit_Field,
        FE_State_Bit_Field,
        Checksum
    }

    enum DataNames25
    {
        Sync,
        Msg_Lenght,
        Msg_ID,
        Channel_ID,
        Data_Page_Number,
        Update_Event_Count,
        Instantaneous_Cadence,
        Accumulated_Power_LSB,
        Accumulated_Power_MSB,
        Instantaneous_Power_LSB,
        Instantaneous_Power_MSB_and_Trainer_Status_Bit_Field,
        Flags_Bit_Field,
        FE_State_Bit_Field,
        Checksum

    }
}
