using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace IoTSharp.Data
{
    public class Relationship
    {
        [Key]
        public Guid Id { get; set; }

        public IdentityUser IdentityUser { get; set; }
        public virtual Tenant Tenant { get; set; }
        public virtual Customer Customer { get; set; }
    }
}