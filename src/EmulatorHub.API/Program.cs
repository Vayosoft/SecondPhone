using EmulatorHub.API.Testing;
using EmulatorHub.Infrastructure;
using Serilog;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using EmulatorHub.API.Hubs;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Debug()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    {
        builder.WebHost.ConfigureKestrel(options => { options.AddServerHeader = false; });
        builder.Host.UseSerilog((context, services, config) => config
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.WithProperty("ApplicationName", typeof(Program).Assembly.GetName().Name)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
#if DEBUG
                .Enrich.WithProperty("DebuggerAttached", Debugger.IsAttached)
#endif
        );

        builder.Services.AddMemoryCache();
        builder.Services.AddSignalR();
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddHubDataContext(builder.Configuration);
        builder.Services.AddHubServices(builder.Configuration);


        //Authentication
        var symmetricKey = "qwertyuiopasdfghjklzxcvbnm123456"; //configuration["Jwt:Symmetric:Key"];
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(symmetricKey));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                //options.IncludeErrorDetails = true; // <- for debugging

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateActor = false,
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        RequireExpirationTime = true, // <- JWTs are required to have "exp" property set
                        ValidateLifetime = true, // <- the "exp" will be validated
                        RequireSignedTokens = true,
                        IssuerSigningKey = signingKey,
                    };
            });
        //Authorization
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireClaim(ClaimTypes.NameIdentifier);
            });
        });
    }

    var app = builder.Build();
    {
        app.UseExceptionHandler("/error");

        if (app.Environment.IsDevelopment())
        {
            app.UseSerilogRequestLogging();
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapHub<RemoteHub>("/rc");

        app.MapGet("/", () => "👍").ExcludeFromDescription();
        app.MapGroup("/test").MapTestApiV1();
    }

    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "An unhandled exception occurred during bootstrapping");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
