﻿using System;
using System.ComponentModel.DataAnnotations;

namespace IoTSharp.Data
{
    public class SubscriptionTask
    {
        [Key]
        public Guid BindId { get; set; }

        public Guid EventId { get; set; }
        public SubscriptionEvent Subscription { get; set; }

        public RuleTaskExecutor RuleTaskExecutor { get; set; }

        public int Status { get; set; }

        public string TaskConfig { get; set; }
    }
}