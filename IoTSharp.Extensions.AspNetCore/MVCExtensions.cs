using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc
{
    public static class MVCExtensions
    {
        public static BadRequestObjectResult ExceptionRequest(this ControllerBase @base, Exception exception)
        {
            MethodBase mb = new StackTrace(exception).GetFrame(0).GetMethod();
            MethodBase cu = new StackTrace(true).GetFrame(0).GetMethod();
            return @base.BadRequest(new
            {
                exception.Message,
                exception.StackTrace,
                ExceptionMethod = mb.DeclaringType.FullName + "." + mb.Name,
                MethodName = cu.Name
            });
        }

        public static BadRequestObjectResult ExceptionRequest<T>(this ControllerBase @base, T code, string msg, Exception exception)
        {
            MethodBase mb = new StackTrace(exception).GetFrame(0).GetMethod();
            MethodBase cu = new StackTrace(true).GetFrame(0).GetMethod();
            return @base.BadRequest(new
            {
                code,
                msg,
                data = new
                {
                    ExceptionMethod = mb.DeclaringType.FullName + "." + mb.Name,
                    MethodName = cu.Name
                }
            });
        }

        public static BadRequestObjectResult ExceptionRequest(this ControllerBase @base, int code, string msg, Exception exception)
            => ExceptionRequest<int>(@base, code, msg, exception);
    }
}