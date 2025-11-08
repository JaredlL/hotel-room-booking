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
                dbContext.Hotels.Add(hotel);
                await dbContext.SaveChangesAsync();
                return Results.Created($"/hotels/{hotel.Name}", hotel);
            })
            .WithName("CreateHotel")
            .AddEndpointFilter<ValidationFilter<Hotel>>();

        app.MapGet("/hotels/{hotelName}", async (string hotelName, HotelRoomDbContext dbContext) =>
            {
                var hotel = await dbContext.Hotels
                    .Include(x => x.Rooms)
                    .FirstOrDefaultAsync(x => x.Name == hotelName);

                return hotel is not null ? Results.Ok(hotel) : Results.NotFound();
            })
            .WithName("GetHotelById");

        app.MapGet("/hotels", async (HotelRoomDbContext dbContext) =>
            {
                var hotels = await dbContext.Hotels
                    .Include(x => x.Rooms)
                    .ToArrayAsync();

                return Results.Ok(hotels);
            })
            .WithName("GetAllHotels");

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
            .WithName("AvailableRooms");

        app.MapPost("/hotels/{hotelName}/bookings",
                async (string hotelName, BookingRequest bookingRequest, HotelRoomDbContext dbContext) =>
                {
                    if (bookingRequest.CheckInDate >= bookingRequest.CheckOutDate)
                    {
                        return Results.BadRequest("Check-in date must be before check-out date");
                    }

                    var hotel = await dbContext.Hotels
                        .Include(x => x.Rooms)
                        .FirstOrDefaultAsync(x => x.Name == hotelName);

                    if (hotel is null)
                    {
                        return Results.NotFound();
                    }

                    var roomIds = hotel.Rooms.Select(r => r.Id).ToList();

                    var alreadyBookedRoomIds = await dbContext.BookedNights
                        .Where(x =>
                            roomIds.Contains(x.RoomId)
                            && x.Date >= bookingRequest.CheckInDate
                            && x.Date < bookingRequest.CheckOutDate)
                        .Select(x => x.RoomId)
                        .Distinct()
                        .ToArrayAsync();

                    var firstFreeRoom = hotel.Rooms
                        .Where(r => r.Matches(bookingRequest))
                        .FirstOrDefault(room => !alreadyBookedRoomIds.Contains(room.Id));

                    if (firstFreeRoom is not null)
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
                        await dbContext.SaveChangesAsync();

                        return Results.Created($"/bookings/{booking.BookingReference}", booking);
                    }

                    return Results.Conflict("No room found for the given criteria");
                })
            .WithName("CreateBooking")
            .AddEndpointFilter<ValidationFilter<BookingRequest>>();
    }
}