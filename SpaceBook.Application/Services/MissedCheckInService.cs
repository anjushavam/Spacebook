using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;

namespace SpaceBook.Application.Services;

public class MissedCheckInService
{
    private readonly IMissedCheckInRepository _repository;
    private readonly INotificationRepository _notificationRepository;

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

    public MissedCheckInService(
        IMissedCheckInRepository repository,
        INotificationRepository notificationRepository)
    {
        _repository = repository;
        _notificationRepository = notificationRepository;
    }


    public async Task CheckMissedCheckInsAsync()
    {
        var bookings =
            await _repository.GetTodayApprovedBookingsAsync();


        foreach(var booking in bookings)
        {
            var checkedIn =
                await _repository.HasCheckInAsync(
                    booking.BookingId);


            var bookingStartTime =
                booking.BookingDate.ToDateTime(
                    booking.StartTime);


            if(!checkedIn &&
               GetIndiaNow() >
               bookingStartTime.AddMinutes(10))
            {

                var notification = new Notification
                {
                    BookingId = booking.BookingId,

                    Message =
                    $"Missed check-in for booking {booking.MeetingTitle}",

                    IsRead = false,

                    CreatedAt =
                    DateTime.UtcNow
                };


                await _notificationRepository
                    .AddAsync(notification);
            }
        }
    }
}