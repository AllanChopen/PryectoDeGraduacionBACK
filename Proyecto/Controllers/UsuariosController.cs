using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public UsuariosController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetAll()
    {
        var usuarios = await _context.Usuarios
            .AsNoTracking()
            .Select(UserDtoProjection)
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> GetById(int id)
    {
        var item = await _context.Usuarios.FindAsync(id);
        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Create([FromBody] CreateUsuarioDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email))
        {
            return Conflict(new { message = "El email ya está registrado." });
        }

        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Email = email,
            Rol = "bandadmin",
            Uuid = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        usuario.Password = PasswordHasher.Hash(dto.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, ToDto(usuario));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioDto dto)
    {
        var existing = await _context.Usuarios.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(u => u.Id != id && u.Email.ToLower() == email))
        {
            return Conflict(new { message = "El email ya está registrado." });
        }

        // Update allowed fields from DTO
        existing.Nombre = dto.Nombre.Trim();
        existing.Email = email;
        existing.UpdatedAt = DateTime.UtcNow;

        // If password provided, hash and update
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            existing.Password = PasswordHasher.Hash(dto.Password);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Usuarios.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Usuarios.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static UsuarioDto ToDto(Usuario usuario) => new()
    {
        Id = usuario.Id,
        Uuid = usuario.Uuid,
        Nombre = usuario.Nombre,
        Email = usuario.Email,
        Rol = usuario.Rol,
        CreatedAt = usuario.CreatedAt,
        UpdatedAt = usuario.UpdatedAt
    };

    private static readonly Expression<Func<Usuario, UsuarioDto>> UserDtoProjection = usuario => new()
    {
        Id = usuario.Id,
        Uuid = usuario.Uuid,
        Nombre = usuario.Nombre,
        Email = usuario.Email,
        Rol = usuario.Rol,
        CreatedAt = usuario.CreatedAt,
        UpdatedAt = usuario.UpdatedAt
    };

    // Login moved to AuthController (POST /api/auth/login)
}
