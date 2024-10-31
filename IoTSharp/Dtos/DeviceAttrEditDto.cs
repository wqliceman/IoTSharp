using System.Collections.Generic;

namespace IoTSharp.Dtos
{
    public class DeviceAttrEditDto
    {
        public Dictionary<string, object> clientside { get; set; }
        public Dictionary<string, object> serverside { get; set; }
        public Dictionary<string, object> anyside { get; set; }
    }
}