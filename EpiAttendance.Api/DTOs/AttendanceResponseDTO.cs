using EpiAttendance.Api.Models;

namespace EpiAttendance.Api.DTOs;

public class AttendanceResponseDTO
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceType Type { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}