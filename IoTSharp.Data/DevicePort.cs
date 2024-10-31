﻿using System;
using System.ComponentModel.DataAnnotations;

namespace IoTSharp.Data
{
    public class DevicePort
    {
        [Key]
        public Guid PortId { get; set; }

        public string PortName { get; set; }
        public string PortDesc { get; set; }
        public string PortPic { get; set; }
        public int PortType { get; set; }
        public int PortPhyType { get; set; }
        public int PortStatus { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime? CreateDate { get; set; }
        public long Creator { get; set; }
        public string PortElementId { get; set; }
    }
}