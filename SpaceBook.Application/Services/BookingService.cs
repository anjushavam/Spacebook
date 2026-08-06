using SpaceBook.Application.DTOs.Booking;
using SpaceBook.Application.Interfaces;
 
namespace SpaceBook.Application.Services;
 
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
 
 
    public BookingService(
        IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }
 
 
    public async Task<BookingDashboardDto> GetDashboardAsync()
    {
        return await _bookingRepository
            .GetDashboardAsync();
    }
 
 
    public async Task<IEnumerable<BookingDto>> GetAllAsync(
        BookingFilterDto filter)
    {
        return await _bookingRepository
            .GetAllAsync(filter);
    }
 
 
    public async Task<BookingDetailsDto?> GetByIdAsync(
        int bookingId)
    {
        return await _bookingRepository
            .GetByIdAsync(bookingId);
    }
 
 
    public async Task ApproveAsync(
        int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception(
                "Booking not found.");
        }
 
 
        await _bookingRepository
            .ApproveAsync(bookingId);
    }
 
 
    public async Task RejectAsync(
        int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception(
                "Booking not found.");
        }
 
 
        await _bookingRepository
            .RejectAsync(bookingId);
    }
 
 
    public async Task DeleteAsync(
        int bookingId)
    {
        if (!await _bookingRepository.ExistsAsync(bookingId))
        {
            throw new Exception(
                "Booking not found.");
        }
 
 
        await _bookingRepository
            .DeleteAsync(bookingId);
    }
}
