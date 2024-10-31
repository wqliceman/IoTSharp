using System.Collections.Generic;

namespace IoTSharp.Data
{
    public class Gateway : Device
    {
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public List<Device> Children { get; set; }
    }
}