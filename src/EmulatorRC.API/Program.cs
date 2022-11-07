﻿using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using EmulatorRC.API.Hubs;
using EmulatorRC.API.Services;
using EmulatorRC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Vayosoft.Identity;

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
                //builder.Services.AddGrpcClient<Screener.ScreenerBase>((sp, o) =>
                //    {
                //        o.Address = new Uri("");
                //    })
                //    .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
                //    {
                //        SslOptions = new SslClientAuthenticationOptions
                //        {
                //            ClientCertificates = new X509CertificateCollection { TryGetSertificate() }
                //        },
                //        PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                //        KeepAlivePingDelay = TimeSpan.FromSeconds(10),
                //        KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
                //        ConnectTimeout = TimeSpan.FromSeconds(30),
                //        EnableMultipleHttp2Connections = true
                //    });
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
                //    options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(10);
                //    options.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromSeconds(15);
                //    options.Listen(IPAddress.Any, 5004, listenOptions =>
                //    {
                //        listenOptions.Protocols = HttpProtocols.Http2;
                //        //listenOptions.UseHttps("<path to .pfx file>", "<certificate password>");
                //    });
                //});

                //Authentication && Authorization
                var symmetricKey = "qwertyuiopasdfghjklzxcvbnm123456"; //configuration["Jwt:Symmetric:Key"];
                builder.Services
                    .AddTokenAuthentication(symmetricKey)
                    .AddTokenAuthorization();
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

