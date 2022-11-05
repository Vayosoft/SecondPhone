﻿using System.Diagnostics;
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

                builder.Services.AddGrpc();
                //builder.WebHost.ConfigureKestrel(options =>
                //{
                //    //options.AllowSynchronousIO = false;
                //    //options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                //    //options.Limits.MaxConcurrentConnections = 100;
                //    //options.Limits.MaxConcurrentUpgradedConnections = 100;
                //    //options.Limits.MaxRequestBodySize = 100_000_000; //[RequestSizeLimit(100_000_000)]
                //    options.Listen(IPAddress.Any, 5003, listenOptions =>
                //    {
                //        listenOptions.Protocols = HttpProtocols.Http1;
                //    });

                //    options.Limits.Http2.MaxStreamsPerConnection = 100;
                //    options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(30);
                //    options.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromMinutes(1);
                //    options.Listen(IPAddress.Any, 5004, listenOptions =>
                //    {
                //        listenOptions.Protocols = HttpProtocols.Http2;
                //        //listenOptions.UseHttps("<path to .pfx file>", "<certificate password>");
                //    });
                //});

                //Authentication
                //https://blog.devgenius.io/jwt-authentication-in-asp-net-core-e67dca9ae3e8
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
                                ValidateActor = false,

                                ValidateAudience = false,
                                ValidAudience = "jwt-test",

                                ValidateIssuer = false,
                                ValidIssuer = "jwt-test",

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
                        policy.RequireClaim(ClaimTypes.Name);
                    });
                });

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

