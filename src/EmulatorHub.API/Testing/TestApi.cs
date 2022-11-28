using EmulatorHub.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Vayosoft.Persistence;
using Vayosoft.PushBrokers;

namespace EmulatorHub.API.Testing
{
    public static class TestApi
    {
        public static IEndpointRouteBuilder MapTestApiV1(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/users", GetAllUsers);
            routes.MapGet("/devices", GetAllDevices);
            routes.MapPost("/devices/add", RegisterDevice);
            routes.MapPost("/devices/sendPush", SendPush);

            return routes;
        }

        public static async Task<Ok<List<DeviceEntity>>> GetAllItems(HubDbContext db)
        {
            return TypedResults.Ok(await db.Set<DeviceEntity>().ToListAsync());
        }

        public static async Task<Ok<List<UserEntity>>> GetAllUsers(IDataProvider db)
        {
            var userSpec = new GetAllUsersSpec();
            //var userSpec = new GetAllUsersSpec("ff79465d-0f75-4995-a121-574f292e9406", "su");

            var user = await db.ListAsync<UserEntity>(userSpec);
            return TypedResults.Ok(user);
        }


        public static async Task<Ok<List<DeviceEntity>>> GetAllDevices(HubDbContext db)
        {

            var devices = await db.Devices.ToListAsync();
            return TypedResults.Ok(devices);
        }
        public static async Task<Results<Ok<DeviceEntity>, NotFound>> GetItem(long id, IUnitOfWork db)
        {
            return await db.FindAsync<DeviceEntity>(1) is DeviceEntity item
                ? TypedResults.Ok(item)
                : TypedResults.NotFound();
        }
        
        public static async Task<Results<Ok, NotFound>> SendPush([FromBody] dynamic data, IUnitOfWork db, PushBrokerFactory pushFactory)
        {
            var user = await db.FindAsync<UserEntity>(1);
            if (user != null && !string.IsNullOrEmpty(user.PushToken))
            {
                pushFactory
                    .GetFor("Android")
                    .Send(user.PushToken, JObject.FromObject(data));
                return TypedResults.Ok();
            }

            return TypedResults.NotFound();
        }

        public static async Task<Results<Ok<DeviceEntity>, NotFound>> RegisterDevice(string deviceId, IUnitOfWork db, [FromServices] ILogger<DeviceEntity> logger)
        {
            var user = await db.FindAsync<UserEntity>(1);
            if (user is { } item)
            {
                var device = new DeviceEntity
                {
                    Id = deviceId,
                    User = user,
                    ProviderId = user.ProviderId,
                    Registered = DateTime.UtcNow,
                };

                db.Add(device);
                await db.CommitAsync();


                //logger.LogInformation($"commit: {testEntity.ToJson()}");

                return TypedResults.Ok(device);
            }
            else
            {
                return TypedResults.NotFound();
            }
        }
    }
}
