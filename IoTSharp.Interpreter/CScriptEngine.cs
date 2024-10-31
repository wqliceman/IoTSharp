using CLanguage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace IoTSharp.Interpreter
{
    public class CScriptEngine : ScriptEngineBase
    {
        public CScriptEngine(ILogger<CScriptEngine> logger, IOptions<EngineSetting> _opt) : base(logger, _opt.Value, System.Threading.Tasks.Task.Factory.CancellationToken)
        {
        }

        public override string Do(string _source, string input)
        {
            var obj = CLanguageService.Eval(_source, $"char* input=\"{input}\";");
            var outputjson = JsonConvert.SerializeObject(obj);
            return outputjson;
        }
    }
}