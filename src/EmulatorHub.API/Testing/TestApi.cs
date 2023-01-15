using App.Metrics;
using App.Metrics.Counter;
using EmulatorHub.Application.Commons.Services.IdentityProvider;
using EmulatorHub.Domain.Commons.Entities;
using EmulatorHub.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Vayosoft.Identity;
using Vayosoft.Persistence;

namespace EmulatorHub.API.Testing
{
    public static class TestApi
    {
        public static IEndpointRouteBuilder MapTestApiV1(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/get_token", GetToken);
            routes.MapGet("/increment", Increment);

            return routes;
        }

        public static Ok Increment(IMetrics metrics, string tag = null)
        {
            var tags = new MetricTags("userTag", string.IsNullOrEmpty(tag) ? "undefined" : tag);
            var counterOptions = new CounterOptions
            {
                MeasurementUnit = Unit.Calls,
                Name = "Counter",
                ResetOnReporting = true
            };
            metrics.Measure.Counter.Increment(counterOptions, tags);
            return TypedResults.Ok();
        }

        public static Ok<TokenResult> GetToken()
        {
            return TypedResults.Ok(TokenUtils.GenerateToken("qwertyuiopasdfghjklzxcvbnm123456", TimeSpan.FromMinutes(60)));
        }

        public static async Task<Ok<List<UserEntity>>> GetAllUsers(IDataProvider db)
        {
            var userSpec = new GetAllUsersSpec();
            //var userSpec = new GetAllUsersSpec("ff79465d-0f75-4995-a121-574f292e9406", "su");

            var user = await db.ListAsync<UserEntity>(userSpec);
            return TypedResults.Ok(user);
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
