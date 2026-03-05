namespace EpiAttendance.Api.DTOs;

public class MonthSummaryDTO
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalAttendedDays  { get; set; }
    public int RequiredDays { get; set; } = 12;
    public bool RequirementMet { get; set; }
    public int RemainingDays { get; set; }
    public List<AttendanceResponseDTO> AttendanceRecords { get; set; } = new List<AttendanceResponseDTO>();
}