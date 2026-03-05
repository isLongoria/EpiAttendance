using EpiAttendance.Api.DTOs;

namespace EpiAttendance.Api.Services;

public interface IAttendanceService
{
    Task<AttendanceResponseDTO?> GetAttendanceByDateAsync(DateOnly date);
    Task<List<AttendanceResponseDTO>> GetAttendanceByMonthAsync(int year, int month);
    Task<AttendanceResponseDTO> CreateOrUpdateAttendanceAsync(AttendanceRequestDTO dto);
    Task<bool> DeleteAttendanceAsync(int id);
    
    Task<MonthSummaryDTO> GetMonthSummaryAsync(int year, int month);
    Task<int> GetAttendedDaysCountAsync(int year, int month);
}