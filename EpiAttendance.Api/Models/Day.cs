namespace EpiAttendance.Api.Models;

public class Day
{
    public DateOnly Date { get; set; }
    public AttendanceType  Attended { get; set; }
}