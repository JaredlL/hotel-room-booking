namespace HotelRoomBooking.Domain;

public class BookedNight
{
    public long RoomId { get; init; }
    public long BookingId { get; init; }
    public required DateOnly Date { get; init; }
}