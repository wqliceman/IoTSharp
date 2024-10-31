using IoTSharp.Contracts;
using System;

namespace IoTSharp.Dtos
{
    public class DevicePostDto
    {
        public string Name { get; set; }
        public DeviceType DeviceType { get; set; }
        public Guid? DeviceModelId { get; set; }

        public int Timeout { get; set; }
        public IdentityType IdentityType { get; set; }

        public Guid? ProductId { get; set; }
    }

    public class DevicePostProduceDto
    {
        public string Name { get; set; }
    }
}