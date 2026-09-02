using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Proyecto.DTOs;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Usuarios.SingleOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null)
        {
            return Unauthorized("Credenciales inválidas");
        }

        if (!PasswordHasher.Verify(user.Password, dto.Password))
        {
            return Unauthorized("Credenciales inválidas");
        }

        // determine banda id for bandadmin (if any)
        var bandaId = await _context.Bandas
            .Where(b => b.UsuarioId == user.Id)
            .Select(b => b.Id)
            .FirstOrDefaultAsync();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Rol ?? string.Empty),
            new Claim("BandaId", bandaId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var userDto = new UsuarioDto
        {
            Id = user.Id,
            Uuid = user.Uuid,
            Nombre = user.Nombre,
            Email = user.Email,
            Rol = user.Rol,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Ok(new { token = tokenString, user = userDto });
    }
}
