using System.ComponentModel.DataAnnotations;

namespace HotelRoomBooking.Domain;

public class Hotel
{
    [StringLength(100)]
    public required string Name { get; init; }

    [Length(6,6)]
    public IReadOnlyCollection<Room> Rooms { get; init; } = new List<Room>();

    public IReadOnlyCollection<Link> Links  =>
    [
        new()
            {
                Href = $"{Name}/bookings",
                Rel = "bookings",
                Type = "get"
            }
    ];

}