using System.Diagnostics;
using System.Net;
using EmulatorRC.API.Hubs;
using EmulatorRC.API.Services;
using EmulatorRC.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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
            }

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

