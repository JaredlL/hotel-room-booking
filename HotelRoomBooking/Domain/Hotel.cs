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
            Href = $"hotels/{Name}",
            Rel = "self",
            Method = "get"
        },
        new()
        {
            Href = $"hotels/{Name}/available-rooms",
            Rel = "rooms",
            Method = "get"
        },
        new()
        {
            Href = $"hotels/{Name}/bookings",
            Rel = "bookings",
            Method = "post"
        },
    ];

}