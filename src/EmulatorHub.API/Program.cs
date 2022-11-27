using EmulatorHub.API.Services;
using EmulatorHub.API.Testing;
using EmulatorHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();
    builder.Services.AddGrpc();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddHubDataContext(builder.Configuration);
    builder.Services.AddHubServices(builder.Configuration);

    builder.Services.AddSingleton<IntercomHub>();
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();
    app.MapGrpcService<IntercomService>();
    app.MapControllers();
    app.MapGroup("/test")
        .MapTestApiV1();
}

app.Run();
