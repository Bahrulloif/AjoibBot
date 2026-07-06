using AjoibBot.Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AjoibBot.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _tokenService;

    public AuthController(JwtTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        // Простая проверка — в реальном проекте идёт в БД
        if (dto.Username != "admin" || dto.Password != "admin123")
            return Unauthorized("Неверный логин или пароль");

        var token = _tokenService.GenerateToken(dto.Username);

        return Ok(new { token });
    }
}

// DTO для входных данных
public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}