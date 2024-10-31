﻿using IoTSharp.Contracts;
using System;

namespace IoTSharp.Dtos
{
    public class BaseEventDto
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; }
        public string EventDesc { get; set; }
        public int EventStaus { get; set; }
        public FlowRuleRunType Type { get; set; }
        public string MataData { get; set; }
        public Guid Creator { get; set; }

        public string Bizid { get; set; }
        public DateTime CreaterDateTime { get; set; }
        public Guid RuleId { get; set; }
        public string Name { get; set; }

        public string CreatorName { get; set; }
    }
}