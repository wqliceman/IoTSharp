﻿using System;
using System.ComponentModel.DataAnnotations;

namespace IoTSharp.Data
{
    public class DeviceDiagram : IJustMy
    {
        [Key]
        public Guid DiagramId { get; set; }

        public string DiagramName { get; set; }
        public string DiagramDesc { get; set; }
        public int DiagramStatus { get; set; }
        public Guid Creator { get; set; }
        public DateTimeOffset? CreateDate { get; set; }

        public string DiagramImage { get; set; }
        public bool IsDefault { get; set; }

        public Tenant Tenant { get; set; }

        public Customer Customer { get; set; }
    }
}