using EpiAttendance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using EpiAttendance.Api.DTOs;

namespace EpiAttendance.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDTO>> Register([FromBody] RegisterRequestDTO dto)
    {
        try
        {
            var (success, message, response) = await _authService.RegisterAsync(dto);
            if (!success) return BadRequest(new { message });

            _logger.LogInformation("User registered successfully: {Email}", dto.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration.");
            return StatusCode(500, new { message = "An error ocurred during registration" });
        }
    }

    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginRequestDTO dto)
    {
        try
        {
            var (success, message, response) = await _authService.LoginAsync(dto);

            if (!success)
                return Unauthorized(new { message });

            _logger.LogInformation("User logged in successfully: {Email}", dto.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login.");
            return StatusCode(500, new { message = "An error ocurred during login" });
        }
    }
    
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDTO>> GetProfile()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            var profile = await _authService.GetUserProfileAsync(userId);
            if (profile == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return StatusCode(500, new { message = "An error occurred while getting profile" });
        }
    }
}
