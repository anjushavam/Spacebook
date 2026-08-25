using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Services;

public class EmployeeCheckInService 
    : IEmployeeCheckInService
{
    private readonly IEmployeeCheckInRepository _repository;


    public EmployeeCheckInService(
        IEmployeeCheckInRepository repository)
    {
        _repository = repository;
    }



    private static readonly TimeZoneInfo IndiaTimeZone = GetIndiaTimeZone();

    private static TimeZoneInfo GetIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
        catch (InvalidTimeZoneException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private static DateTime GetIndiaNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
    }

    public async Task<CheckInDto> CheckInAsync(
        int bookingId,
        int employeeId)
    {

        var booking =
            await _repository.GetBookingAsync(
                bookingId,
                employeeId);


        if (booking == null)
        {
            throw new Exception(
                "Booking not found.");
        }



        var bookingDateTime =
            booking.BookingDate
            .ToDateTime(
                booking.StartTime);



        var currentTime =
            GetIndiaNow();



        // 10 minutes grace period
        if(currentTime >
            bookingDateTime.AddMinutes(10))
        {
            throw new Exception(
                "Check-in time expired.");
        }



        var checkIn = new CheckIn
        {
            BookingId = bookingId,

            CheckedInAt =
                DateTime.UtcNow,

            Status =
                "Checked-In"
        };



        await _repository
            .AddAsync(checkIn);



        return new CheckInDto
        {
            BookingId = bookingId,

            CheckedInAt =
                checkIn.CheckedInAt,

            Status =
                checkIn.Status
        };
    }
}