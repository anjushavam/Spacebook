using SpaceBook.Application.Interfaces;using SpaceBook.Domain.Entities;
namespace SpaceBook.Application.Services;
public class MissedCheckInService{
    private readonly IMissedCheckInRepository _repository;
    private readonly INotificationRepository _notificationRepository;


    public MissedCheckInService(
        IMissedCheckInRepository repository,
        INotificationRepository notificationRepository)
    {
        _repository = repository;
        _notificationRepository = notificationRepository;
    }


    public async Task CheckMissedCheckInsAsync()
    {
        var bookings =await _repository.GetTodayApprovedBookingsAsync();


        foreach(var booking in bookings)
        {
            var checkedIn =await _repository.HasCheckInAsync(
                    booking.BookingId);


            var bookingStartTime =booking.BookingDate.ToDateTime(
                    booking.StartTime);


            if(!checkedIn &&DateTime.Now >bookingStartTime.AddMinutes(10))
            {

                var notification = new Notification                {
                    BookingId = booking.BookingId,

                    Message =$"Missed check-in for booking {booking.MeetingTitle}",

                    IsRead = false,

                    CreatedAt =DateTime.UtcNow                };


                await _notificationRepository                    .AddAsync(notification);
            }
        }
    }
}