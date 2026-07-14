using DailyOneRosterFile.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DailyOneRosterFile.Api.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ValidateTokenAttribute : TypeFilterAttribute
{
    public ValidateTokenAttribute() : base(typeof(ValidateTokenFilter))
    {
        Order = 1;
    }
}

public class ValidateTokenFilter : IActionFilter
{
    private readonly ITokenService _tokenService;

    public ValidateTokenFilter(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var token = context.HttpContext.Request.Query["token"];

        if (string.IsNullOrEmpty(token))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!_tokenService.ValidateToken(token!, "OneRoster.zip"))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["Token"] = token;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not used
    }
}