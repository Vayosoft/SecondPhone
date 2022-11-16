using EmulatorHub.Entities;
using EmulatorHub.Infrastructure;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vayosoft.Persistence;

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
        routes.MapGet("/v1/items", GetAllItems);
        routes.MapGet("/v1/items/{id}", GetItem);
        routes.MapPost("/v1/update", UpdateItem);

        return routes;
    }

    public static async Task<Ok<List<UserEntity>>> GetAllItems(HubDbContext db)
    {
        return TypedResults.Ok(await db.Users.ToListAsync());
    }
    
    public static async Task<Results<Ok<UserEntity>, NotFound>> GetItem(long id, IUnitOfWork db)
    {
        return await db.FindAsync<UserEntity>(id) is UserEntity item
            ? TypedResults.Ok(item)
            : TypedResults.NotFound();
    }

    public static async Task<Results<Ok, NotFound>> UpdateItem(UserEntity entity, IUnitOfWork db)
    {
        var user = await db.FindAsync<UserEntity>(entity.Id);
        if (user is UserEntity item)
        {
            item.Email = entity.Email;
            db.Update(item);
            await db.CommitAsync();

            return TypedResults.Ok();
        }
        else
        {
            return TypedResults.NotFound();
        }
    }
}
