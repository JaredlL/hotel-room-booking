using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelRoomBooking.Domain;

public class Booking
{
    public long BookingReference { get; init; }
    public required Hotel Hotel { get; init; }
    public required Room BookedRoom { get; init; }

    public int NumberOfGuests { get; init; }

    [StringLength(100)]
    public required string GuestId { get; set; }

    [JsonIgnore]
    public List<BookedNight> BookedNights { get; init; } = [];

    public DateOnly CheckInDate => BookedNights.Select(x => x.Date).Min();

    /// <remarks>
    /// Check-out date is the day after the last night booked.
    /// </remarks>
    public DateOnly CheckOutDate => BookedNights.Select(x => x.Date).Max().AddDays(1);
}