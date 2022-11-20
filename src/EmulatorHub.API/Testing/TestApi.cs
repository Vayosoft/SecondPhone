using EmulatorHub.Domain.Entities;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Persistence;

namespace EmulatorHub.API.Testing
{
    public static class TestApi
    {
        public static IEndpointRouteBuilder MapTestApiV1(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("/v1/items", GetAllUsers);
            routes.MapGet("/v1/items/{id}", GetItem);
            routes.MapPost("/v1/update", UpdateItem);

            return routes;
        }

        public static async Task<Ok<List<TestEntity>>> GetAllItems(HubDbContext db)
        {
            return TypedResults.Ok(await db.Set<TestEntity>().ToListAsync());
        }

        public static async Task<Ok<List<UserEntity>>> GetAllUsers(IDataProvider db)
        {
            var userSpec = new GetAllUsersSpec();
            //var userSpec = new GetAllUsersSpec("ff79465d-0f75-4995-a121-574f292e9406", "su");

            var user = await db.ListAsync<UserEntity>(userSpec);
            return TypedResults.Ok(user);
        }

      
        public static async Task<Results<Ok<UserEntity>, NotFound>> GetItem(long id, IUnitOfWork db)
        {
            return await db.FindAsync<UserEntity>(id) is UserEntity item
                ? TypedResults.Ok(item)
                : TypedResults.NotFound();
        }

        public static async Task<Results<Ok<TestEntity>, NotFound>> UpdateItem(UserEntity entity, IUnitOfWork db, [FromServices] ILogger<TestEntity> logger)
        {
            var user = await db.FindAsync<UserEntity>(1);
            if (user is UserEntity item)
            {
                //item.Email = "su@vayosoft.com";

                //item.RefreshTokens.Add(new RefreshToken
                //{
                //    Token = "ff79465d-0f75-4995-a121-574f292e9406",
                //    Created = DateTime.UtcNow,
                //    Expires = DateTime.MaxValue,

                //});

                //db.Update(item);
                //await db.CommitAsync();

                var testEntity = new TestEntity
                {
                    Timestamp = DateTime.UtcNow,
                    Name = "yyy",
                    ProviderId = 0
                };
                db.Add(testEntity);

                await db.CommitAsync();


                //logger.LogInformation($"commit: {testEntity.ToJson()}");

                return TypedResults.Ok(testEntity);
            }
            else
            {
                return TypedResults.NotFound();
            }
        }
    }
}
