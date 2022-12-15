namespace EmulatorHub.Commons.Application.Services.Authorization
{
    //IAuthorizationPolicyProvider
    //https://learn.microsoft.com/ru-ru/aspnet/core/security/authorization/iauthorizationpolicyprovider?view=aspnetcore-6.0

    //services.AddSingleton<IAuthorizationHandler, TokenHandler>();
    //services.AddAuthorization(options =>
    //{
    //    options.AddPolicy("Token", policy =>
    //        policy.Requirements.Add(new TokenRequirement()));
    //});

    //[Authorize(Policy = "Token")]

    //public class TokenRequirement : IAuthorizationRequirement
    //{ }

    //public class TokenHandler : AuthorizationHandler<TokenRequirement>
    //{
    //    private readonly TokenValidator _tokenValidator;

    //    public TokenHandler(TokenValidator tokenValidator)
    //    {
    //        _tokenValidator = tokenValidator;
    //    }
    //    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TokenRequirement requirement)
    //    {
    //        if (context.Resource is AuthorizationFilterContext authContext)
    //        {

    //            if (userIsAuthenticated == false)
    //            {
    //                context.Fail();
    //            }
    //            if (userIsAuthenticated == true)
    //            {
    //                context.Succeed(requirement);
    //            }
    //        }
    //        return Task.CompletedTask;
    //    }
    //}
}
