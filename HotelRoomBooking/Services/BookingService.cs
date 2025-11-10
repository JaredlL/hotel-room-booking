using HotelRoomBooking.Data;
using HotelRoomBooking.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelRoomBooking.Services;

public static class BookingService
{
    public static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/bookings/{bookingReference:long}",
            async (long bookingReference, HotelRoomDbContext dbContext) =>
            {
                var booking = await dbContext.Bookings
                    .Include(x => x.Hotel)
                    .Include(x => x.BookedRoom)
                    .Include(x => x.BookedNights)
                    .FirstOrDefaultAsync(x => x.BookingReference == bookingReference);

                return booking is not null
                    ? Results.Ok(new BookingDto(booking))
                    : Results.NotFound();
            })
            .WithName("GetBookingById")
            .WithDescription("Returns a booking by its reference");
    }
}