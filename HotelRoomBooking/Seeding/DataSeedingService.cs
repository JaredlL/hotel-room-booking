using HotelRoomBooking.Data;
using HotelRoomBooking.Domain;

namespace HotelRoomBooking.Seeding;

public static class DataSeedingService
{
    public static void MapEndpoints(WebApplication app)
    {
        app.MapPost("/testdata", async (HotelRoomDbContext dbContext) =>
        {
            var hotel = CreateSeedHotel();
            dbContext.Hotels.Add(hotel);
            await dbContext.SaveChangesAsync();

            var booking = CreateSeedBooking(hotel);
            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/hotels/{hotel.Name}", new {
                Hotel = hotel,
                Booking = new BookingDto(booking),
            });
        })
        .WithDescription("Seeds the database with a single hotel and a single booking")
        .WithName("SeedDatabase");

        app.MapDelete("/testdata", async (HotelRoomDbContext dbContext) =>
        {
            dbContext.Hotels.RemoveRange(dbContext.Hotels);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithDescription("Clears the database of all data")
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
                RoomName = "104",
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

    private static Booking CreateSeedBooking(Hotel hotel)
    {
        var room = hotel.Rooms.First(r => r.RoomName == "102");

        var bookingRequest = new BookingRequest()
        {
            GuestId = "Bob",
            NumberOfGuests = 2,
            CheckInDate = new DateOnly(2025, 11, 07),
            CheckOutDate = new DateOnly(2025, 11, 09),
        };

        var nights = bookingRequest.RequiredNights.Select(date => new BookedNight()
            {
                RoomId = room.Id,
                Date = date
            })
            .ToList();

        return new Booking
        {
            BookedNights = nights,
            BookedRoom = room,
            GuestId = bookingRequest.GuestId,
            Hotel = hotel,
            NumberOfGuests = bookingRequest.NumberOfGuests
        };
    }
}