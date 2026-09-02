using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
    public async Task<ActionResult<IEnumerable<Usuario>>> GetAll()
    {
        return Ok(await _context.Usuarios.AsNoTracking().ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Usuario>> GetById(int id)
    {
        var item = await _context.Usuarios.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Usuario>> Create([FromBody] CreateUsuarioDto dto)
    {
        var usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Email = dto.Email,
            Rol = dto.Rol,
            Uuid = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            usuario.Password = PasswordHasher.Hash(dto.Password);
        }

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioDto dto)
    {
        var existing = await _context.Usuarios.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }
        // Update allowed fields from DTO
        existing.Nombre = dto.Nombre;
        existing.Email = dto.Email;
        existing.Rol = dto.Rol;
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

    // Login moved to AuthController (POST /api/auth/login)
}
