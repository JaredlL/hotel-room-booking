using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelRoomBooking.Domain;

public class BookingRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoomType? RoomType { get; set; }

    [StringLength(100)]
    public string? RoomName { get; set; }

    [StringLength(100)]
    public required string GuestId { get; set; }

    [Range(1, 100)]
    public required int NumberOfGuests { get; init; }
    public required DateOnly CheckInDate { get; set; }
    public required DateOnly CheckOutDate { get; set; }

    public IEnumerable<DateOnly> RequiredNights
    {
        get
        {
            var days = CheckOutDate.DayNumber - CheckInDate.DayNumber;
            return Enumerable.Range(0, days)
                .Select(offset => CheckInDate.AddDays(offset));
        }
    }

}