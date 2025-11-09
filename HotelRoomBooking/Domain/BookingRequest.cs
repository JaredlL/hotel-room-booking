using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelRoomBooking.Domain;

public class BookingRequest : IValidatableObject
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoomType? RoomType { get; set; }

    [StringLength(100)]
    public string? RoomName { get; set; }

    [StringLength(100)]
    public required string GuestId { get; set; }

    [Range(1, 100)]
    public required int NumberOfGuests { get; set; }
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckInDate >= CheckOutDate)
        {
            yield return new ValidationResult(
                "Check-in date must be before check-out date",
                [nameof(CheckInDate), nameof(CheckOutDate)]);
        }

        if (CheckInDate >= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1))
        {
            yield return new ValidationResult(
                "Cannot create bookings more than a year in advance",
                [nameof(CheckInDate)]);
        }

        if (CheckOutDate >= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1))
        {
            yield return new ValidationResult(
                "Cannot create bookings more than a year in advance",
                [nameof(CheckOutDate)]);
        }

        // Another possible requirement would be to prevent bookings in the past - but that will make testing
        // more difficult, so it is ignored for now.
    }
}