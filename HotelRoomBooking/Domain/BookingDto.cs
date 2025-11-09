namespace HotelRoomBooking.Domain;

/// <summary>
/// External model intended to be sent to the client
/// </summary>
public class BookingDto(Booking booking)
{
    public long BookingReference => booking.BookingReference;

    public Room BookedRoom => booking.BookedRoom;

    public BookingHotelDto Hotel => new BookingHotelDto(booking.Hotel);

    public int NumberOfGuests => booking.NumberOfGuests;

    public string GuestId => booking.GuestId;

    public DateOnly CheckInDate => booking.CheckInDate;

    public DateOnly CheckOutDate => booking.CheckOutDate;

    public IReadOnlyCollection<Link> Links  =>
    [
        new()
        {
            Href = $"bookings/{BookingReference}",
            Rel = "self",
            Method = "get"
        }
    ];

    public class BookingHotelDto(Hotel hotel)
    {
        public string Name => hotel.Name;

        public IReadOnlyCollection<Link> Links => hotel.Links;
    }
}