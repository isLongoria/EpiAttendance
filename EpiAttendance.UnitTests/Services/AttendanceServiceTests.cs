using System.Security.Claims;
using EpiAttendance.Api.Data;
using EpiAttendance.Api.DTOs;
using EpiAttendance.Api.Models;
using EpiAttendance.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EpiAttendance.UnitTests.Services;

public class AttendanceServiceTests
{
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    private static (AppDbContext context, AttendanceService service) BuildService(string userId = TestUserId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId)
        ], "test"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new AttendanceService(context, accessor);

        return (context, service);
    }

    // --- GetMonthSummaryAsync ---

    [Fact]
    public async Task GetMonthSummary_ZeroAttendedDays_RequirementNotMet()
    {
        var (_, service) = BuildService();

        var summary = await service.GetMonthSummaryAsync(2026, 1);

        summary.RequirementMet.Should().BeFalse();
        summary.RemainingDays.Should().Be(12);
        summary.TotalAttendedDays.Should().Be(0);
    }

    [Fact]
    public async Task GetMonthSummary_TwelveAttendedDays_RequirementMet()
    {
        var (context, service) = BuildService();
        for (var day = 1; day <= 12; day++)
            context.AttendanceRecords.Add(new AttendanceRecord
            {
                Date = new DateOnly(2026, 2, day),
                Type = AttendanceType.Attended,
                UserId = TestUserId
            });
        await context.SaveChangesAsync();

        var summary = await service.GetMonthSummaryAsync(2026, 2);

        summary.RequirementMet.Should().BeTrue();
        summary.RemainingDays.Should().Be(0);
        summary.TotalAttendedDays.Should().Be(12);
    }

    [Fact]
    public async Task GetMonthSummary_NARecordsNotCounted()
    {
        var (context, service) = BuildService();
        for (var day = 1; day <= 5; day++)
            context.AttendanceRecords.Add(new AttendanceRecord
            {
                Date = new DateOnly(2026, 3, day),
                Type = AttendanceType.NA,
                UserId = TestUserId
            });
        await context.SaveChangesAsync();

        var summary = await service.GetMonthSummaryAsync(2026, 3);

        summary.TotalAttendedDays.Should().Be(0);
        summary.RequirementMet.Should().BeFalse();
    }

    // --- GetAttendedDaysCountAsync ---

    [Fact]
    public async Task GetAttendedDaysCount_ReturnsCorrectCount()
    {
        var (context, service) = BuildService();
        context.AttendanceRecords.AddRange(
            new AttendanceRecord { Date = new DateOnly(2026, 4, 1), Type = AttendanceType.Attended, UserId = TestUserId },
            new AttendanceRecord { Date = new DateOnly(2026, 4, 2), Type = AttendanceType.PTO, UserId = TestUserId },
            new AttendanceRecord { Date = new DateOnly(2026, 4, 3), Type = AttendanceType.NA, UserId = TestUserId }
        );
        await context.SaveChangesAsync();

        var count = await service.GetAttendedDaysCountAsync(2026, 4);

        count.Should().Be(2);
    }

    // --- GetAttendanceByMonthAsync ---

    [Fact]
    public async Task GetAttendanceByMonth_ReturnsOnlyRequestedMonth()
    {
        var (context, service) = BuildService();
        context.AttendanceRecords.AddRange(
            new AttendanceRecord { Date = new DateOnly(2026, 5, 1), Type = AttendanceType.Attended, UserId = TestUserId },
            new AttendanceRecord { Date = new DateOnly(2026, 5, 15), Type = AttendanceType.Attended, UserId = TestUserId },
            new AttendanceRecord { Date = new DateOnly(2026, 6, 1), Type = AttendanceType.Attended, UserId = TestUserId }
        );
        await context.SaveChangesAsync();

        var records = await service.GetAttendanceByMonthAsync(2026, 5);

        records.Should().HaveCount(2);
        records.Should().AllSatisfy(r => r.Date.Month.Should().Be(5));
    }

    // --- DeleteAttendanceAsync ---

    [Fact]
    public async Task DeleteAttendance_NonExistentId_ReturnsFalse()
    {
        var (_, service) = BuildService();

        var result = await service.DeleteAttendanceAsync(9999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAttendance_OtherUsersRecord_ReturnsFalse()
    {
        var (context, service) = BuildService(); // TestUserId service
        var record = new AttendanceRecord
        {
            Date = new DateOnly(2026, 6, 10),
            Type = AttendanceType.Attended,
            UserId = OtherUserId
        };
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync();

        var result = await service.DeleteAttendanceAsync(record.Id);

        result.Should().BeFalse();
    }

    // --- CreateOrUpdateAttendanceAsync ---

    [Fact]
    public async Task CreateAttendance_NewRecord_SetsCreatedAt()
    {
        var (_, service) = BuildService();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await service.CreateOrUpdateAttendanceAsync(new AttendanceRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Type = AttendanceType.Attended
        });

        result.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateAttendance_ExistingDate_SetsUpdatedAt()
    {
        var (context, service) = BuildService();
        var date = new DateOnly(2026, 8, 1);
        context.AttendanceRecords.Add(new AttendanceRecord
        {
            Date = date,
            Type = AttendanceType.Attended,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        });
        await context.SaveChangesAsync();

        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await service.CreateOrUpdateAttendanceAsync(new AttendanceRequestDTO
        {
            Date = date,
            Type = AttendanceType.PTO,
            Notes = "Updated"
        });

        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt!.Value.Should().BeAfter(before);
        result.Notes.Should().Be("Updated");
    }
}
