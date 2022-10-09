using Talk.NPOI;

namespace IoTClient.Tool.Model
{
    public class BacnetPropertyInfo
    {
        [Alias("device address")]
        public string IpAddress { get; set; }

        [Alias("address")]
        public string Address { get; set; }

        [Alias("Data Types")]
        public string DataType { get; set; }

        [Alias("value")]
        public string Value { get; set; }

        public string ReadWrite { get; set; }

        [Alias("roll call")]
        public string PropName { get; set; }

        [Alias("description")]
        public string Describe { get; set; }

        [Alias("ObjectType")]
        public string ObjectType { get; set; }
    }
}
