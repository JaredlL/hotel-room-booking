using HotelRoomBooking.Data;
using HotelRoomBooking.Domain;
using HotelRoomBooking.Seeding;
using HotelRoomBooking.ServiceDefaults;
using HotelRoomBooking.Services;
using HotelRoomBooking.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace HotelRoomBooking;

/// <remarks>
/// This is declared explicitly (rather than rely on top-level statements) in order to
/// <list type="bullet">
/// <item>make the implicitly defined Program class publicly visible for reference in testing</item>
/// <item>define a namespace for the Program class, so that it can be referenced unambiguously</item>
/// </list>
/// </remarks>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.Services.AddOpenApi();
        builder.AddNpgsqlDbContext<HotelRoomDbContext>(connectionName: "hotelbooking");

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // go to /openapi/v1.json
            app.MapOpenApi();

            // go to http://localhost:5160/scalar/
            app.MapScalarApiReference();

            // In development mode, create the database if it doesn't exist.
            // In production, the database is expected to be created and migrated manually.
            await using var scope = app.Services.CreateAsyncScope();
            await using var db = scope.ServiceProvider.GetRequiredService<HotelRoomDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.UseStatusCodePages(async statusCodeContext
            => await Results.Problem(statusCode: statusCodeContext.HttpContext.Response.StatusCode)
                .ExecuteAsync(statusCodeContext.HttpContext));

        app.UseHttpsRedirection();

        HotelService.MapEndpoints(app);
        BookingService.MapEndpoints(app);
        DataSeedingService.MapEndpoints(app);

        await app.RunAsync();
        
        return 0;
    }
}

