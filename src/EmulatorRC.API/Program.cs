using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;
using Commons.Cache;
using Commons.Core.Application;
using Commons.Core.Cache;
using EmulatorRC.API.Channels;
using EmulatorRC.API.Hubs;
using EmulatorRC.API.Model;
using EmulatorRC.API.Model.Bridge;
using EmulatorRC.API.Services;
using EmulatorRC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace EmulatorRC.API;

//https://learn.microsoft.com/ru-ru/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-6.0
public class Program
{
    public static void Main(string[] args)
    {
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

                builder.Services.AddSingleton<IEmulatorDataRepository, EmulatorDataRepository>();
                builder.Services.AddSingleton<ScreenChannel>();
                builder.Services.AddSingleton<TouchChannel>();
                builder.Services.AddSingleton<TcpStreamChannel>();
                builder.Services.AddSingleton<IMemoryCacheProvider, MemoryCacheProvider>();
                builder.Services.AddSingleton<IPubSubCacheProvider, RedisProvider>();
                builder.Services.AddSingleton<ApplicationCache>();

                builder.Services.AddGrpc(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
                    options.MaxSendMessageSize = 5 * 1024 * 1024; // 5 MB
                    // Small performance benefit to not add catch-all routes to handle UNIMPLEMENTED for unknown services
                    options.IgnoreUnknownServices = true;
                })
                .AddServiceOptions<InternalService>(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.MaxReceiveMessageSize = 5 * 1024 * 1024; // 2 MB
                });

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

                // Bridge services
                builder.Services.AddHostedService<BridgeLifetimeEventsService>();
            }
            
            var app = builder.Build();
            {
                app.UseExceptionHandler("/error");

                if (app.Environment.IsDevelopment())
                {
                    app.UseSerilogRequestLogging();
                }
                else
                {
                    //app.UseHttpsRedirection();
                }

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapGrpcService<OuterService>();
                app.MapGrpcService<InternalService>();

                app.MapHub<TouchEventsHub>("/chathub");

                app.MapGet("/", () => "👍");
                app.MapGet("/error", (HttpContext httpContext) =>
                {
                    var exceptionFeature = httpContext.Features.Get<IExceptionHandlerFeature>();
                    return TypedResults.Problem(
                        statusCode: (int) (exceptionFeature?.Error.ToHttpStatusCode() ?? HttpStatusCode.InternalServerError),
                        title: "An error occurred while processing your request.",
                        detail: exceptionFeature?.Error.Message ?? string.Empty
                    );
                }).ExcludeFromDescription();
                app.Run();
            }

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
    }
}

