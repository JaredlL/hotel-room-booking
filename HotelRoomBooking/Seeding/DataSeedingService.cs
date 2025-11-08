using HotelRoomBooking.Data;
using HotelRoomBooking.Domain;

namespace HotelRoomBooking.Seeding;

public static class DataSeedingService
{
    private const string Prefix = "/testdata";

    public static void MapEndpoints(WebApplication app)
    {
        app.MapPost(Prefix, async (HotelRoomDbContext dbContext) =>
        {
            var hotel = CreateSeedHotel();
            dbContext.Hotels.Add(hotel);
            await dbContext.SaveChangesAsync();
            return Results.Created($"/hotels/{hotel.Name}", hotel);
        })
        .WithName("SeedDatabase");

        app.MapDelete(Prefix, async (HotelRoomDbContext dbContext) =>
        {
            dbContext.Hotels.RemoveRange(dbContext.Hotels);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("ClearDatabase");
    }

    private static Hotel CreateSeedHotel() => new Hotel()
    {
        Name = "Grand Plaza Hotel",
        Rooms =
        [
            new Room()
            {
                Capacity = 1,
                RoomName = "101",
                RoomType = RoomType.Single,
            },
            new Room()
            {
                Capacity = 2,
                RoomName = "102",
                RoomType = RoomType.Double,
            },
            new Room()
            {
                Capacity = 2,
                RoomName = "103",
                RoomType = RoomType.Deluxe,
            },
            new Room()
            {
                Capacity = 1,
                RoomName = "101",
                RoomType = RoomType.Single,
            },
            new Room()
            {
                Capacity = 2,
                RoomName = "105",
                RoomType = RoomType.Double,
            },
            new Room()
            {
                Capacity = 2,
                RoomName = "106",
                RoomType = RoomType.Deluxe,
            }
        ]
    };
}