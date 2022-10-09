using IoTClient.Enums;
using System.IO.BACnet;

namespace IoTClient.Tool
{
    public class BacProperty
    {
        public BacnetObjectId ObjectId { get; set; }
        public string Prop_Object_Name { get; set; }
        public object Prop_Present_Value { get; set; }
        public DataTypeEnum Prop_DataType { get; set; }
        public string Prop_Description { get; set; }
    }
}
