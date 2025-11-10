using HotelRoomBooking.Data;
using HotelRoomBooking.Domain;
using HotelRoomBooking.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomBooking.Services;

public class HotelService
{
    public static void MapEndpoints(WebApplication app)
    {
        app.MapPost("/hotels", async (Hotel hotel, HotelRoomDbContext dbContext) =>
            {
                // Two or more requests may race to create the same hotel.
                // The Hotel unique name (primary key) will ensure that this is not possible, but to avoid a
                // 500 error code we use a resilience strategy to retry and potentially return 409 instead.
                return await ResilienceStrategies.UniqueConstraintRaceConditionStrategy.ExecuteAsync(async ct =>
                {
                    var existing = await dbContext.Hotels.FindAsync([hotel.Name], ct);
                    if (existing is not null)
                    {
                        return Results.Conflict();
                    }

                    dbContext.Hotels.Add(hotel);
                    await dbContext.SaveChangesAsync(ct);
                    return Results.Created($"/hotels/{hotel.Name}", hotel);
                });
            })
            .WithName("CreateHotel")
            .WithDescription("Create Hotels")
            .AddEndpointFilter<ValidationFilter<Hotel>>();

        app.MapGet("/hotels/{hotelName}", async (string hotelName, HotelRoomDbContext dbContext) =>
            {
                var hotel = await dbContext.Hotels
                    .Include(x => x.Rooms)
                    .FirstOrDefaultAsync(x => x.Name == hotelName);

                return hotel is not null ? Results.Ok(hotel) : Results.NotFound();
            })
            .WithDescription("Returns a Hotel by its name")
            .WithName("GetHotelById");

        app.MapGet("/hotels", async (HotelRoomDbContext dbContext) =>
            {
                var hotels = await dbContext.Hotels
                    .Include(x => x.Rooms)
                    .ToArrayAsync();

                return Results.Ok(hotels);
            })
            .WithName("GetAllHotels")
            .WithDescription("Returns all Hotels");

        app.MapGet("/hotels/{hotelName}/available-rooms",
                async ([FromRoute] string hotelName,
                    [FromQuery(Name = "checkin")] DateOnly checkin,
                    [FromQuery(Name = "checkout")] DateOnly checkout,
                    [FromQuery(Name = "numberOfGuests")] int? numberOfGuests,
                    HotelRoomDbContext dbContext) =>
                {
                    if (checkin >= checkout)
                    {
                        return Results.BadRequest("Check-in date must be before check-out date");
                    }

                    var hotelExists = await dbContext.Hotels
                        .AnyAsync(h => h.Name == hotelName);

                    if (!hotelExists)
                    {
                        return Results.NotFound("No hotel found with the given name");
                    }

                    var availableRooms = await dbContext.Hotels
                        .Where(h => h.Name == hotelName)
                        .SelectMany(h => h.Rooms)
                        .Where(r => !numberOfGuests.HasValue || r.Capacity >= numberOfGuests.Value)
                        .Where(r => !dbContext.BookedNights.Any(bn =>
                            bn.RoomId == r.Id
                            && bn.Date >= checkin
                            && bn.Date < checkout))
                        .ToListAsync();

                    return Results.Ok(availableRooms);
                })
            .WithDescription("Returns a list of available rooms for the given dates and number of guests")
            .WithName("AvailableRooms");

        app.MapPost("/hotels/{hotelName}/bookings",
                async (string hotelName, BookingRequest bookingRequest, HotelRoomDbContext dbContext) =>
                {
                    // Two or more requests may race to create the same booking.
                    // The DB composite key constraint will ensure that this is not possible, but to avoid a
                    // 500 error code we use a resilience strategy to retry the operation so that
                    // a 409 can be returned.
                    return await ResilienceStrategies.UniqueConstraintRaceConditionStrategy.ExecuteAsync(async ct =>
                    {
                        var hotel = await dbContext.Hotels
                            .Include(x => x.Rooms)
                            .FirstOrDefaultAsync(x => x.Name == hotelName, ct);

                        if (hotel is null)
                        {
                            return Results.NotFound();
                        }

                        var firstFreeRoom = await FindFirstFreeMatchingRoomAsync(hotel, dbContext, bookingRequest, ct);
                        if (firstFreeRoom is not null)
                        {
                            return await CreateBookingAsync(bookingRequest, firstFreeRoom, hotel, dbContext, ct);
                        }

                        return Results.Conflict("All suitable rooms are fully booked");
                    });
                })
            .WithName("CreateBooking")
            .WithDescription("Attempts to book a room that matches the given criteria.")
            .AddEndpointFilter<ValidationFilter<BookingRequest>>();
    }

    private static async Task<IResult> CreateBookingAsync(
        BookingRequest bookingRequest,
        Room firstFreeRoom,
        Hotel hotel,
        HotelRoomDbContext dbContext,
        CancellationToken ct)
    {
        var nights = bookingRequest.RequiredNights.Select(date => new BookedNight()
            {
                RoomId = firstFreeRoom.Id,
                Date = date
            })
            .ToList();

        var booking = new Booking
        {
            BookedNights = nights,
            BookedRoom = firstFreeRoom,
            GuestId = bookingRequest.GuestId,
            Hotel = hotel,
            NumberOfGuests = bookingRequest.NumberOfGuests
        };

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(ct);

        var bookingDto = new BookingDto(booking);

        return Results.Created($"/bookings/{booking.BookingReference}", bookingDto);
    }

    private static async Task<Room?> FindFirstFreeMatchingRoomAsync(
        Hotel hotel,
        HotelRoomDbContext dbContext,
        BookingRequest bookingRequest,
        CancellationToken ct)
    {
        var roomIds = hotel.Rooms.Select(r => r.Id).ToList();

        var alreadyBookedRoomIds = await dbContext.BookedNights
            .Where(x =>
                roomIds.Contains(x.RoomId)
                && x.Date >= bookingRequest.CheckInDate
                && x.Date < bookingRequest.CheckOutDate)
            .Select(x => x.RoomId)
            .Distinct()
            .ToArrayAsync(ct);

        // Book the smallest room first, save bigger rooms for bigger bookings.
        // If there were large numbers (1000+) of rooms per hotel, we may consider moving some of this logic to the DB
        // to improve performance.
        // As there are only 6 rooms per hotel, iterating the list in memory should not have a performance impact.
        var firstFreeRoom = hotel.Rooms
            .Where(r => r.Matches(bookingRequest))
            .OrderBy(r => r.Capacity)
            .FirstOrDefault(room => !alreadyBookedRoomIds.Contains(room.Id));

        return firstFreeRoom;
    }
}