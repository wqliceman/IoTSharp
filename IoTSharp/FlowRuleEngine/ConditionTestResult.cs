using IoTSharp.Data;
using System.Collections.Generic;

namespace IoTSharp.FlowRuleEngine
{
    public class ConditionTestResult
    {
        public List<Flow> Passed { get; set; }

        public List<Flow> Failed { get; set; }
    }

    public class ScriptTestResult
    {
        public dynamic Data { get; set; }

        public bool IsExecuted { get; set; }
    }
}