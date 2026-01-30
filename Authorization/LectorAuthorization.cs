using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace ControlInventario.Authorization
{
    public class LectorRequirement : IAuthorizationRequirement
    {
    }

    public class LectorAuthorizationHandler : AuthorizationHandler<LectorRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LectorRequirement requirement)
        {
            if (context.User.IsInRole("Admin") || context.User.IsInRole("Lector"))
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }

    public class LectorAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new Microsoft.AspNetCore.Mvc.RedirectResult("/Account/Login");
                return;
            }

            if (!user.IsInRole("Admin") && !user.IsInRole("Lector"))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.RedirectResult("/Home/AccessDenied");
                return;
            }
        }
    }
}
