using EmulatorHub.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Persistence;

namespace EmulatorHub.API.Testing
{
    public static class TestApi
    {
        public static IEndpointRouteBuilder MapTestApiV1(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/users", GetAllUsers);
            routes.MapGet("/devices", GetAllDevices);
            routes.MapPost("/register", RegisterClient);

            return routes;
        }

        public static async Task<Ok<List<Emulator>>> GetAllItems(HubDbContext db)
        {
            return TypedResults.Ok(await db.Set<Emulator>().ToListAsync());
        }

        public static async Task<Ok<List<UserEntity>>> GetAllUsers(IDataProvider db)
        {
            var userSpec = new GetAllUsersSpec();
            //var userSpec = new GetAllUsersSpec("ff79465d-0f75-4995-a121-574f292e9406", "su");

            var user = await db.ListAsync<UserEntity>(userSpec);
            return TypedResults.Ok(user);
        }


        public static async Task<Ok<List<Emulator>>> GetAllDevices(HubDbContext db)
        {

            var devices = await db.Devices.ToListAsync();
            return TypedResults.Ok(devices);
        }
        public static async Task<Results<Ok<Emulator>, NotFound>> GetItem(long id, IUnitOfWork db)
        {
            return await db.FindAsync<Emulator>(1) is Emulator item
                ? TypedResults.Ok(item)
                : TypedResults.NotFound();
        }


        public static async Task<Results<Ok<Emulator>, NotFound>> RegisterClient(string clientId, string deviceId, IUnitOfWork db, [FromServices] ILogger<Emulator> logger)
        {
            var user = await db.FindAsync<UserEntity>(1);
            if (user is { } item)
            {
                var client = new MobileClient
                {
                    Id = clientId,
                    User = user,
                    ProviderId = user.ProviderId
                };
                db.Add(client);

                var device = new Emulator
                {
                    Id = deviceId,
                    Client = client,
                    ProviderId = user.ProviderId,
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
