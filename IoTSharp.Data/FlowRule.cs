using IoTSharp.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace IoTSharp.Data
{
    public class FlowRule : IJustMy
    {
        [Key] public Guid RuleId { get; set; }
        public RuleType RuleType { get; set; }

        [Required]
        public string Name { get; set; }

        public string Describes { get; set; }
        public string Runner { get; set; }
        public string ExecutableCode { get; set; }
        public string Creator { get; set; }
        public string RuleDesc { get; set; }
        public int? RuleStatus { get; set; }
        public DateTime? CreatTime { get; set; }
        public string DefinitionsXml { get; set; }
        public Guid ParentRuleId { get; set; }
        public double SubVersion { get; set; }

        public double Version { get; set; }
        public Guid CreateId { get; set; }
        public EventType MountType { get; set; }
        public Tenant Tenant { get; set; }

        public Customer Customer { get; set; }
    }
}