using IoTClient.Enums;
using Newtonsoft.Json;
using System.IO;
using System.IO.Ports;

namespace IoTClient.Tool.Common
{
    public class ConnectionConfig
    {
        public string ModBusTcp_IP;
        public string ModBusTcp_Port;
        public string ModBusTcp_StationNumber;
        public EndianFormat ModBusTcp_EndianFormat = EndianFormat.ABCD;
        public string ModBusTcp_Address;
        public string ModBusTcp_Value;
        public bool ModBusTcp_ShowPackage;
        public string ModBusTcp_Datatype;


        public static ConnectionConfig GetConfig()
        {
            var dataString = string.Empty;
            var path = @"C:\IoTClient";
            var filePath = path + @"\ConnectionConfig.Data";
            if (File.Exists(filePath))
                dataString = File.ReadAllText(filePath);
            else
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.SetAttributes(path, FileAttributes.Hidden);
            }
            return JsonConvert.DeserializeObject<ConnectionConfig>(dataString) ?? new ConnectionConfig();
        }

        public void SaveConfig()
        {
            var dataString = JsonConvert.SerializeObject(this);
            var path = @"C:\IoTClient";
            var filePath = path + @"\ConnectionConfig.Data";
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fileStream))
                {
                    sw.Write(dataString);
                }
            }
        }
    }
}