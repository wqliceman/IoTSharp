using IoTSharp.Data;
using System;

namespace IoTSharp.Dtos
{
    public class CustomerDto : Customer
    {
        public Guid TenantId { get; set; }
    }
}