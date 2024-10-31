using IoTSharp.Contracts;
using System;

namespace IoTSharp.Dtos
{
    public class DevicePutDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public int Timeout { get; set; }
        public Guid? DeviceModelId { get; set; }
        public IdentityType IdentityType { get; set; }
    }
}