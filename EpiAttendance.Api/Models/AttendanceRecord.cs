using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EpiAttendance.Api.Models;

public class AttendanceRecord
{
    [Key]
    public int Id { get; set; }
    [Required]
    public DateOnly Date { get; set; }
    [Required]
    public AttendanceType Type { get; set; }
    [Required]
    public string UserId { get; set; }
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}