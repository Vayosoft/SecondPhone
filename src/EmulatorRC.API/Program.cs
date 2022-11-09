using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using EmulatorRC.API.Hubs;
using EmulatorRC.API.Services;
using EmulatorRC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
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
                var configuration = builder.Configuration;

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

                builder.Services.AddControllers();

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                builder.Services.AddSignalR();

                builder.Services.Configure<FormOptions>(o =>
                {
                    o.ValueLengthLimit = int.MaxValue;
                    o.MultipartBodyLengthLimit = int.MaxValue;
                    o.MemoryBufferThreshold = int.MaxValue;
                });
                builder.Services.AddMemoryCache();
                builder.Services.AddSingleton<IEmulatorDataRepository, EmulatorDataRepository>();

                builder.Services.AddGrpc(options =>
                    {
                        options.EnableDetailedErrors = true;
                        options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
                        options.MaxSendMessageSize = 5 * 1024 * 1024; // 5 MB
                    })
                    .AddServiceOptions<UploaderService>(options =>
                    {
                        options.EnableDetailedErrors = true;
                        options.MaxReceiveMessageSize = 5 * 1024 * 1024; // 2 MB
                    });

                //Authentication && Authorization
                var symmetricKey = "qwertyuiopasdfghjklzxcvbnm123456"; //configuration["Jwt:Symmetric:Key"];
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(symmetricKey));
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.IncludeErrorDetails = true; // <- for debugging

                        options.TokenValidationParameters =
                            new TokenValidationParameters
                            {
                                ValidateActor = false,
                                ValidateAudience = false,
                                ValidAudience = "Vayosoft",
                                ValidateIssuer = false,
                                ValidIssuer = "Vayosoft",
                                RequireExpirationTime = true, // <- JWTs are required to have "exp" property set
                                ValidateLifetime = true, // <- the "exp" will be validated
                                RequireSignedTokens = true,
                                IssuerSigningKey = signingKey,
                            };
                    });
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
                    {
                        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                        policy.RequireClaim(ClaimTypes.Name);
                    });
                });

                builder.Services.AddSingleton<ScreenChannel>();
            }

            var app = builder.Build();
            {
                app.UseExceptionHandler("/error");

                app.UseStaticFiles();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSerilogRequestLogging();

                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
                else
                {
                    //app.UseHttpsRedirection();
                }

                // Authenticate, then Authorize
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapGrpcService<ScreenService>();
                app.MapGrpcService<UploaderService>();
                app.MapControllers();
                app.MapHub<TouchEventsHub>("/chathub");
                app.MapHub<ImagesHub>("/zub");
                app.MapFallbackToFile("/index.html");

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

