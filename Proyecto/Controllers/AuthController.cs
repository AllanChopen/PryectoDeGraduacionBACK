using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Proyecto.DTOs;
using Proyecto.Models;
using Proyecto.Services;
using PryectoDeGraduacionBACK.Services.S3;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IS3Service _s3Service;

    public AuthController(
        AppDbContext context,
        IConfiguration config,
        IS3Service s3Service)
    {
        _context = context;
        _config = config;
        _s3Service = s3Service;
    }

    [HttpPost("signup")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Signup([FromForm] SignupDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var slug = NormalizeSlug(dto.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            return BadRequest(new { message = "El slug no es válido." });
        }

        if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email))
        {
            return Conflict(new { message = "El email ya está registrado." });
        }

        if (await _context.Bandas.AnyAsync(b => b.Slug.ToLower() == slug))
        {
            return Conflict(new { message = "El slug ya está ocupado." });
        }

        var imageError = ImageFileValidator.Validate(dto.Logo);
        if (imageError is not null)
        {
            return BadRequest(imageError);
        }

        string? logoKey = null;
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            string? logoUrl = null;
            if (dto.Logo is not null)
            {
                await using var stream = dto.Logo.OpenReadStream();
                logoKey = await _s3Service.UploadAsync(
                    stream,
                    Path.GetFileName(dto.Logo.FileName),
                    dto.Logo.ContentType,
                    "bandas");
                logoUrl = _s3Service.GetPublicUrl(logoKey);
            }

            var usuario = new Usuario
            {
                Nombre = dto.Nombre.Trim(),
                Email = email,
                Password = PasswordHasher.Hash(dto.Password),
                Rol = "bandadmin",
                Uuid = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var banda = new Banda
            {
                Uuid = Guid.NewGuid(),
                Nombre = dto.BandaNombre.Trim(),
                Slug = slug,
                ImagenUrl = logoUrl,
                UsuarioId = usuario.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };

            _context.Bandas.Add(banda);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var token = CreateToken(usuario, banda.Id);
            return StatusCode(StatusCodes.Status201Created, new
            {
                token,
                user = ToDto(usuario),
                banda = new BandaDto
                {
                    Uuid = banda.Uuid,
                    Nombre = banda.Nombre,
                    Slug = banda.Slug,
                    ImagenUrl = banda.ImagenUrl
                }
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            if (logoKey is not null)
            {
                await _s3Service.DeleteAsync(logoKey);
            }

            throw;
        }
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

        var tokenString = CreateToken(user, bandaId);

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

    private string CreateToken(Usuario user, int bandaId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Rol ?? string.Empty),
            new("BandaId", bandaId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UsuarioDto ToDto(Usuario user) => new()
    {
        Id = user.Id,
        Uuid = user.Uuid,
        Nombre = user.Nombre,
        Email = user.Email,
        Rol = user.Rol,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static string NormalizeSlug(string value)
    {
        var slug = value.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");
        return slug.Trim('-');
    }
}
