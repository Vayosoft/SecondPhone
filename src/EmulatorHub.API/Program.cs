using System.Linq.Expressions;
using EmulatorHub.Entities;
using EmulatorHub.Infrastructure;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Identity;
using Vayosoft.Identity.Tokens;
using Vayosoft.Persistence;
using Vayosoft.Specifications;

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
        var userSpec = new UserByTokenSpec("ff79465d-0f75-4995-a121-574f292e9406");

        var user = await db.ListAsync(userSpec);
        return TypedResults.Ok(user);
    }


    public class UserByTokenSpec : Specification<UserEntity>
    {
        public UserByTokenSpec(string token)
        {
            Include(u => u.RefreshTokens);
            Where(u => u.RefreshTokens.Any(t => t.Token == token));
        }
    }

    public class UserByNameSpec : Specification<UserEntity>
    {
        public UserByNameSpec(string name)
        {
            Where(u => u.Username == name);
        }
    }

    public static async Task<Results<Ok<UserEntity>, NotFound>> GetItem(long id, IUnitOfWork db)
    {
        return await db.FindAsync<UserEntity>(id) is UserEntity item
            ? TypedResults.Ok(item)
            : TypedResults.NotFound();
    }

    public static async Task<Results<Ok, NotFound>> UpdateItem(UserEntity entity, IUnitOfWork db)
    {
        var user = await db.FindAsync<UserEntity>(1);
        if (user is UserEntity item)
        {
            item.Email = "su@vayosoft.com";

            
            //db.Update(item);
            await db.CommitAsync();

            return TypedResults.Ok();
        }
        else
        {
            return TypedResults.NotFound();
        }
    }
}
