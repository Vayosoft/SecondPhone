using EmulatorHub.API.Testing;
using EmulatorHub.Infrastructure;
using Serilog;
using System.Diagnostics;
using Vayosoft.Web.Identity;
using EmulatorHub.API.Hubs;
using System.Text.Json.Serialization;
using EmulatorHub.API.Services.Monitoring;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using EmulatorHub.API;
using System;
using Vayosoft.Web.Swagger;
using Vayosoft.Web.Identity.Authentication;

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
                .Enrich.WithProperty("ApplicationName", typeof(Program).Assembly.GetName().Name!)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
#if DEBUG
                .Enrich.WithProperty("DebuggerAttached", Debugger.IsAttached)
#endif
        );

        builder.Services.AddApplicationCaching(configuration);
        //builder.Services.AddMemoryCache();
        builder.Services.AddSignalR();
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
        builder.Services.AddEndpointsApiExplorer();
        //builder.Services.AddSwaggerGen();
        builder.Services.AddSwaggerService();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddHubDataContext(builder.Configuration);
        builder.Services.AddHubServices(builder.Configuration);

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("RedisConnection");
            options.InstanceName = "SessionInstance";
        });
        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = ".second_phone.session";
            options.IdleTimeout = TimeSpan.FromSeconds(3600); //Default is 20 minutes.
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = false;
        });

        builder.Services.AddIdentityService(configuration);

        //Authentication
        //var symmetricKey = "qwertyuiopasdfghjklzxcvbnm123456"; //configuration["Jwt:Symmetric:Key"];
        //var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(symmetricKey));
        //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //    .AddJwtBearer(options =>
        //    {
        //        //options.IncludeErrorDetails = true; // <- for debugging

        //        options.TokenValidationParameters =
        //            new TokenValidationParameters
        //            {
        //                ValidateActor = false,
        //                ValidateAudience = false,
        //                ValidateIssuer = false,
        //                RequireExpirationTime = true, // <- JWTs are required to have "exp" property set
        //                ValidateLifetime = true, // <- the "exp" will be validated
        //                RequireSignedTokens = true,
        //                IssuerSigningKey = signingKey,
        //            };
        //    });
        ////Authorization
        //builder.Services.AddAuthorization(options =>
        //{
        //    options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
        //    {
        //        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        //        policy.RequireClaim(ClaimTypes.NameIdentifier);
        //    });
        //});

        // HealthCheck
        builder.Services
            .AddHealthChecks()
            .AddMySql(configuration["ConnectionStrings:DefaultConnection"] ?? string.Empty, tags: new[] {"infrastructure", "db"})
            .AddRedis(configuration["ConnectionStrings:RedisConnection"] ?? string.Empty, tags: new[] {"infrastructure", "cache"});
        //.AddMongoDb(configuration["ConnectionStrings:MongoDbConnection"], tags: new[] { "infrastructure", "db" });

        // Metrics
        builder.AddDiagnostics();
    }

    var app = builder.Build();
    {
        app.UseExceptionHandler("/error");

        if (app.Environment.IsDevelopment())
        {
            app.UseSerilogRequestLogging();
        }

        //app.UseMetricsAllMiddleware();

        // HealthCheck
        app.MapHealthChecks("/health", new HealthCheckOptions()
        {
            AllowCachingResponses = false,
            ResponseWriter = HealthCheckResponse.Write
        });

        //app.UseSwagger();
        //app.UseSwaggerUI();
        app.UseSwaggerService();

        //app.UseAuthentication();
        app.UseMiddleware<TokenAuthenticationMiddleware>();
        app.UseAuthorization();
        app.UseSession();

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
