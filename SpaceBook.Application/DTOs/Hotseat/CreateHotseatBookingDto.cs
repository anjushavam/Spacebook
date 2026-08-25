using System.Text.Json.Serialization;
using SpaceBook.Application.Common.JsonConverters;

namespace SpaceBook.Application.DTOs.Hotseat;

public class CreateHotseatBookingDto
{
    public int SeatId { get; set; }

    public string? SeatNumber { get; set; }

    public string? ModuleName { get; set; }

    public string? Module { get; set; }

    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateOnly BookingDate { get; set; }

    [JsonConverter(typeof(NullableTimeOnlyJsonConverter))]
    public TimeOnly? ExpectedCheckInTime { get; set; }
}