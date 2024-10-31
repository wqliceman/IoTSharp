using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IoTSharp.Interpreter
{
    public static class ScriptEnginesExtensions
    {
        public static IServiceCollection AddScriptEngines(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<JavaScriptEngine>();
            services.AddTransient<PythonScriptEngine>();
            services.AddTransient<SQLEngine>();
            services.AddTransient<LuaScriptEngine>();
            services.AddTransient<CSharpScriptEngine>();
            services.Configure<EngineSetting>(configuration);
            return services;
        }
    }
}