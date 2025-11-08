using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelRoomBooking.Domain;

public class Room
{
    [JsonIgnore]
    public long Id { get; init; }

    /// <summary>
    /// Room name, for example, 101, Lomond
    /// </summary>
    [StringLength(100)]
    public required string RoomName { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required RoomType RoomType { get; init; }

    [Range(1, 100)]
    public int Capacity { get; init; }

    [JsonIgnore]
    public Hotel? Hotel { get; init; }

    [JsonIgnore]
    public IReadOnlyCollection<BookedNight>? BookedNights { get; init; } = null;

    public bool Matches(BookingRequest request)
    {
        return request switch
        {
            { NumberOfGuests: var numberOfGuests } when numberOfGuests > Capacity => false,
            { RoomName: { } requestRoomName } => requestRoomName == RoomName,
            { RoomType: { } requestRoomType } => requestRoomType == RoomType,
            _ => true
        };
    }
}