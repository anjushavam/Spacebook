namespace SpaceBook.Application.DTOs.Reports;
 
public class ReportFilterDto
{
    public string? ReportType { get; set; }
 
    public string? Module { get; set; }
 
    public int? RoomTypeId { get; set; }
 
    public string? Status { get; set; }
}