using EpiAttendance.Api.Models;

namespace EpiAttendance.Api.DTOs;

public class AttendanceRequestDTO
{
    public int? Id { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceType Type { get; set; }
    public string? Notes { get; set; }
}