using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserRepository users, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await users.GetByUsernameAsync(request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Benutzername oder Passwort ungültig.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims:
            [
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username));
    }
}
