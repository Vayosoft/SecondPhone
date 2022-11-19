using System.Linq.Expressions;
using EmulatorHub.Entities;
using EmulatorHub.Infrastructure;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vayosoft.Identity;
using Vayosoft.Identity.Tokens;
using Vayosoft.Persistence;
using Vayosoft.Persistence.Criterias;
using Vayosoft.Persistence.Specifications;
using Vayosoft.Utilities;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddHubDataContext(builder.Configuration);
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();

    app.MapControllers();

    app.MapGroup("/api").MapApiV1();
}

app.Run();

public static class V1ApiGroup
{
    public static IEndpointRouteBuilder MapApiV1(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/v1/items", GetAllUsers);
        routes.MapGet("/v1/items/{id}", GetItem);
        routes.MapPost("/v1/update", UpdateItem);

        return routes;
    }

    public static async Task<Ok<List<UserEntity>>> GetAllItems(HubDbContext db)
    {
        return TypedResults.Ok(await db.Users.ToListAsync());
    }

    public static async Task<Ok<List<UserEntity>>> GetAllUsers(IDataProvider db)
    {
        var userSpec = new GetAllUsersSpec("ff79465d-0f75-4995-a121-574f292e9406", "su");

        var user = await db.ListAsync(userSpec);
        return TypedResults.Ok(user);
    }

    public class UserByTokenCriteria : Criteria<UserEntity>
    {
        public UserByTokenCriteria(string token)
        {
            Include(u => u.RefreshTokens);
            Where(u => u.RefreshTokens.Any(t => t.Token == token));
        }
    }

    public class UserByNameCriteria : Criteria<UserEntity>
    {
        public UserByNameCriteria(string name)
        {
            Where(u => u.Username == name);
        }
    }

    public class GetAllUsersSpec : Specification<UserEntity>
    {
        public GetAllUsersSpec(string token, string username)
        {
            //Where(new UserByTokenCriteria(token) && new UserByNameCriteria(username));

            Include(u => u.RefreshTokens);
            Where(u => u.Username == username && u.RefreshTokens.Any(t => t.Token == token));
            
            OrderBy(u => u.Username);
        }
    }

    public static async Task<Results<Ok<UserEntity>, NotFound>> GetItem(long id, IUnitOfWork db)
    {
        var test = await db.FindAsync<TestEntity>((3, DateTime.Parse("2022-11-18 10:25:15.959794000")));
        return await db.FindAsync<UserEntity>(id) is UserEntity item
            ? TypedResults.Ok(item)
            : TypedResults.NotFound();
    }

    public static async Task<Results<Ok<TestEntity>, NotFound>> UpdateItem(UserEntity entity, IUnitOfWork db, [FromServices]ILogger<TestEntity> logger)
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
                TestProperty = "0"
            };
            //db.Add(testEntity);

            user.Email = "1";
            await db.CommitAsync();

            user.Email = "2";

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
