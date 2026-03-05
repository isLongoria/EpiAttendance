using EpiAttendance.Api.DTOs;
using EpiAttendance.Api.Models;
using EpiAttendance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EpiAttendance.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/attendance")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        [HttpGet("date/{date}")]
        public async Task<ActionResult<AttendanceResponseDTO>> GetByDate(DateOnly date)
        {
            var record = await _attendanceService.GetAttendanceByDateAsync(date);
            
            if(record == null)
                return NotFound(new {message  = "No attendance record found for this date."});
            
            return Ok(record);
        }

        [HttpGet("month/{year}/{month}")]
        public async Task<ActionResult<List<AttendanceResponseDTO>>> GetByMonth(int year, int month)
        {
            if (month < 1 || month > 12)
                return BadRequest(new { message = "Month must be between 1 and 12" });
            
            var records = await _attendanceService.GetAttendanceByMonthAsync(year, month);
            return Ok(records);
        }

        [HttpGet("summary/{year}/{month}")]
        public async Task<ActionResult<MonthSummaryDTO>> GetMonthSummary(int year, int month)
        {
            if (month < 1 || month > 12)
                return BadRequest(new { message = "Month must be between 1 and 12" });
            
            var summary = await _attendanceService.GetMonthSummaryAsync(year, month);
            return Ok(summary);
        }

        [HttpGet("count/{year}/{month}")]
        public async Task<ActionResult<int>> GetAttendedDaysCount(int year, int month)
        {
            if (month < 1 || month > 12)
                return BadRequest(new { message = "Month must be between 1 and 12" });

            var count = await _attendanceService.GetAttendedDaysCountAsync(year, month);
            return Ok(new { year, month, attendedDays = count });
        }

        [HttpPost]
        public async Task<ActionResult<AttendanceResponseDTO>> CreateOrUpdate([FromBody] AttendanceRequestDTO dto)
        {
            try
            {
                var result = await _attendanceService.CreateOrUpdateAttendanceAsync(dto);
                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e,  e.Message, "Error creating or updating attendance record");
                return StatusCode(500, new { message = "An error ocurred while saving the record." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _attendanceService.DeleteAttendanceAsync(id);
            
            if(!deleted)
                return NotFound(new {message = "Record  not found."});
            
            return NoContent();
        }
    }    
}
