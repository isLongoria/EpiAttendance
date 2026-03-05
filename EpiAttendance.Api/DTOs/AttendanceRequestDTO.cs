using System.ComponentModel.DataAnnotations;
using EpiAttendance.Api.Models;

namespace EpiAttendance.Api.DTOs;

public class AttendanceRequestDTO
{
    public int? Id { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    [Range(0, 4, ErrorMessage = "Invalid attendance type.")]
    public AttendanceType Type { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
