using System;

namespace IoTSharp.Models
{
    public class UserProfile
    {
        public string[] Roles { get; set; }
        public Guid Id { get; set; }
        public string Email { get; set; }
        public Guid Customer { get; set; }
        public Guid Tenant { get; set; }
        public string Name { get; set; }
    }
}