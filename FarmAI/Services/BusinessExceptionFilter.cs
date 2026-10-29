using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FarmAI.Services;

// Business/domain failures in GET actions are user-facing, not internal server errors.
public class BusinessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if(context.Exception is not BusinessException business)return;
        context.Result=new ContentResult{StatusCode=403,ContentType="text/plain; charset=utf-8",Content=business.Message};
        context.ExceptionHandled=true;
    }
}
