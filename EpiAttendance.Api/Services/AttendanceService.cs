using System.Security.Claims;
using EpiAttendance.Api.Data;
using EpiAttendance.Api.DTOs;
using EpiAttendance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EpiAttendance.Api.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const int REQUIRED_DAYS_PER_MONTH = 12;

    public AttendanceService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetCurrentUserId() =>
        _httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<AttendanceResponseDTO?> GetAttendanceByDateAsync(DateOnly date)
    {
        var userId = GetCurrentUserId();
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.Date == date && a.UserId == userId);

        return record == null ? null : MapToResponseDto(record);
    }

    public async Task<List<AttendanceResponseDTO>> GetAttendanceByMonthAsync(int year, int month)
    {
        var userId = GetCurrentUserId();
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var records = await _context.AttendanceRecords
            .Where(a => a.Date >= startDate && a.Date <= endDate && a.UserId == userId)
            .OrderBy(a => a.Date)
            .ToListAsync();

        return records.Select(MapToResponseDto).ToList();
    }

    public async Task<AttendanceResponseDTO> CreateOrUpdateAttendanceAsync(AttendanceRequestDTO dto)
    {
        var userId = GetCurrentUserId();
        var existingRecord = await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.Date == dto.Date && a.UserId == userId);
        if (existingRecord != null)
        {
            existingRecord.Type = dto.Type;
            existingRecord.Notes = dto.Notes;
            existingRecord.UpdatedAt = DateTime.UtcNow;
            _context.AttendanceRecords.Update(existingRecord);
        }
        else
        {
            existingRecord = new AttendanceRecord
            {
                Date = dto.Date,
                Type = dto.Type,
                Notes = dto.Notes,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _context.AttendanceRecords.AddAsync(existingRecord);
        }

        await _context.SaveChangesAsync();
        return MapToResponseDto(existingRecord);
    }

    public async Task<bool> DeleteAttendanceAsync(int id)
    {
        var userId = GetCurrentUserId();
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (record == null)
            return false;

        _context.AttendanceRecords.Remove(record);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MonthSummaryDTO> GetMonthSummaryAsync(int year, int month)
    {
        var records = await GetAttendanceByMonthAsync(year, month);
        var attendedDays = records.Count(a => a.Type != AttendanceType.NA);
        var remainingDays = Math.Max(0, REQUIRED_DAYS_PER_MONTH - attendedDays);

        return new MonthSummaryDTO
        {
            Year = year,
            Month = month,
            TotalAttendedDays = attendedDays,
            RequiredDays = REQUIRED_DAYS_PER_MONTH,
            RequirementMet = attendedDays >= REQUIRED_DAYS_PER_MONTH,
            RemainingDays = remainingDays,
            AttendanceRecords = records
        };
    }

    public async Task<int> GetAttendedDaysCountAsync(int year, int month)
    {
        var userId = GetCurrentUserId();
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _context.AttendanceRecords
            .Where(a => a.Date >= startDate && a.Date <= endDate && a.UserId == userId && a.Type != AttendanceType.NA)
            .CountAsync();
    }
    
    private AttendanceResponseDTO? MapToResponseDto(AttendanceRecord record)
    {
        return new AttendanceResponseDTO
        {
            Id = record.Id,
            Date = record.Date,
            Type = record.Type,
            Notes = record.Notes,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}