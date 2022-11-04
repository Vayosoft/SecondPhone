using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text;
using EmulatorRC.API.Hubs;
using EmulatorRC.API.Services;
using EmulatorRC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace EmulatorRC.API;

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
                builder.Host.UseSerilog((context, services, configuration) => configuration
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

                builder.Services.AddGrpc();
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Listen(IPAddress.Any, 5003, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                    });
                    options.Listen(IPAddress.Any, 5004, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        //listenOptions.UseHttps("<path to .pfx file>", "<certificate password>");
                    });
                });

                //Authentication
                //var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Symmetric:Key"]));
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("qwertyuiopasdfghjklzxcvbnm123456"));
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.IncludeErrorDetails = true; // <- great for debugging

                        // Configure the actual Bearer validation
                        options.TokenValidationParameters = 
                            new TokenValidationParameters
                        {
                            IssuerSigningKey = signingKey,
                            ValidAudience = "jwt-test",
                            ValidIssuer = "jwt-test",
                            RequireSignedTokens = true,
                            RequireExpirationTime = true, // <- JWTs are required to have "exp" property set
                            ValidateLifetime = true, // <- the "exp" will be validated
                            ValidateAudience = true,
                            ValidateIssuer = true,
                        };

                        //options.TokenValidationParameters =
                        //    new TokenValidationParameters
                        //    {
                        //        ValidateAudience = false,
                        //        ValidateIssuer = false,
                        //        ValidateActor = false,
                        //        ValidateLifetime = true,
                        //        IssuerSigningKey = signingKey
                        //    };
                    });
                //Authorization
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
                    {
                        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                        policy.RequireClaim(ClaimTypes.Name);
                    });
                });

            }

            var app = builder.Build();
            {
                //if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseExceptionHandler("/error");

                app.UseStaticFiles();
                if (app.Environment.IsDevelopment())
                {
                    app.UseSerilogRequestLogging();
                }
                //app.UseHttpsRedirection();

                // Authenticate, then Authorize
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapGrpcService<ScreenService>();
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

