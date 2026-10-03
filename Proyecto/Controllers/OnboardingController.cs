using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto.DTOs;
using Proyecto.Models;
using Proyecto.Services;
using PryectoDeGraduacionBACK.Services.S3;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize(Roles = "bandadmin")]
public class OnboardingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _tokenService;
    private readonly IS3Service _s3Service;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        AppDbContext context,
        JwtTokenService tokenService,
        IS3Service s3Service,
        ILogger<OnboardingController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _s3Service = s3Service;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] RegistroBandaDto request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Las contraseñas no coinciden." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == email))
        {
            return Conflict(new { message = "El correo ya está registrado." });
        }

        var now = DateTime.UtcNow;
        var user = new Usuario
        {
            Email = email,
            Nombre = email.Split('@')[0],
            Password = PasswordHasher.Hash(request.Password),
            Rol = "bandadmin",
            Uuid = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now,
            Timestamp = now
        };

        _context.Usuarios.Add(user);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return Conflict(new { message = "El correo ya está registrado." });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            token = _tokenService.CreateToken(user, 0),
            user = new { user.Id, user.Uuid, user.Email, user.Rol },
            onboardingStep = 2
        });
    }

    [HttpPost("identidad")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Identidad([FromForm] IdentidadBandaDto request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var nombre = request.Nombre.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (nombre.Length == 0 || !Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return BadRequest(new { message = "Nombre o slug inválido. El slug admite letras minúsculas, números y guiones." });
        }

        var logoError = ImageFileValidator.Validate(request.Logo);
        if (logoError is not null)
        {
            return BadRequest(new { message = logoError });
        }

        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Unauthorized();
        }

        if (await _context.Bandas.AnyAsync(b => b.UsuarioId == userId))
        {
            return Conflict(new { message = "Esta cuenta ya tiene una banda." });
        }

        if (await _context.Bandas.AnyAsync(b => b.Slug.ToLower() == slug))
        {
            return Conflict(new { message = "El slug ya está en uso." });
        }

        string? logoKey = null;
        try
        {
            await using var stream = request.Logo.OpenReadStream();
            logoKey = await _s3Service.UploadAsync(
                stream, Path.GetFileName(request.Logo.FileName), request.Logo.ContentType, "bandas");

            var now = DateTime.UtcNow;
            var banda = new Banda
            {
                Nombre = nombre,
                Slug = slug,
                UsuarioId = userId,
                LogoUrl = _s3Service.GetPublicUrl(logoKey),
                OnboardingStep = 3,
                CreatedAt = now,
                UpdatedAt = now,
                Timestamp = now
            };
            _context.Bandas.Add(banda);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                token = _tokenService.CreateToken(user, banda.Id),
                banda = new { banda.Id, banda.Uuid, banda.Nombre, banda.Slug, banda.LogoUrl },
                publicPath = $"/{banda.Slug}",
                onboardingStep = banda.OnboardingStep
            });
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            if (logoKey is not null) await _s3Service.DeleteAsync(logoKey);
            return Conflict(new { message = "La banda o el slug ya están registrados." });
        }
        catch
        {
            if (logoKey is not null) await _s3Service.DeleteAsync(logoKey);
            throw;
        }
    }

    [HttpPut("identidad")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> EditarIdentidad([FromForm] EditarIdentidadBandaDto request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var banda = await _context.Bandas.FirstOrDefaultAsync(b => b.UsuarioId == userId);
        if (banda is null)
        {
            return NotFound(new { message = "Primero registra la identidad de la banda." });
        }

        var nombre = request.Nombre.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (nombre.Length == 0 || !Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            return BadRequest(new { message = "Nombre o slug inválido. El slug admite letras minúsculas, números y guiones." });
        }

        var logoError = ImageFileValidator.Validate(request.Logo);
        if (logoError is not null)
        {
            return BadRequest(new { message = logoError });
        }

        if (await _context.Bandas.AnyAsync(b => b.Id != banda.Id && b.Slug.ToLower() == slug))
        {
            return Conflict(new { message = "El slug ya está en uso." });
        }

        var oldLogoUrl = banda.LogoUrl;
        string? newLogoKey = null;
        try
        {
            if (request.Logo is not null)
            {
                await using var stream = request.Logo.OpenReadStream();
                newLogoKey = await _s3Service.UploadAsync(
                    stream, Path.GetFileName(request.Logo.FileName), request.Logo.ContentType, "bandas");
                banda.LogoUrl = _s3Service.GetPublicUrl(newLogoKey);
            }

            banda.Nombre = nombre;
            banda.Slug = slug;
            banda.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            if (newLogoKey is not null) await _s3Service.DeleteAsync(newLogoKey);
            return Conflict(new { message = "El slug ya está en uso." });
        }
        catch
        {
            if (newLogoKey is not null) await _s3Service.DeleteAsync(newLogoKey);
            throw;
        }

        if (newLogoKey is not null) await DeleteOldImageAsync(oldLogoUrl);

        return Ok(new
        {
            banda.Id, banda.Nombre, banda.Slug, banda.LogoUrl,
            publicPath = $"/{banda.Slug}",
            onboardingStep = banda.OnboardingStep
        });
    }

    [HttpPut("personalizacion")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Personalizacion([FromForm] PersonalizacionBandaDto request)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var banda = await _context.Bandas.FirstOrDefaultAsync(b => b.UsuarioId == userId);
        if (banda is null)
        {
            return NotFound(new { message = "Primero registra la identidad de la banda." });
        }

        if (request.PrecioEnvio < 0)
        {
            return BadRequest(new { message = "El precio de envío no puede ser negativo." });
        }

        var imageError = ImageFileValidator.Validate(request.Imagen);
        var bioImageError = ImageFileValidator.Validate(request.BiografiaImagen);
        if (imageError is not null || bioImageError is not null)
        {
            return BadRequest(new { message = imageError ?? bioImageError });
        }

        var oldImageUrl = banda.ImagenUrl;
        var oldBioImageUrl = banda.BiografiaImagenUrl;
        var uploadedKeys = new List<string>();
        try
        {
            if (request.Imagen is not null)
            {
                await using var stream = request.Imagen.OpenReadStream();
                var key = await _s3Service.UploadAsync(
                    stream, Path.GetFileName(request.Imagen.FileName), request.Imagen.ContentType, "bandas");
                uploadedKeys.Add(key);
                banda.ImagenUrl = _s3Service.GetPublicUrl(key);
            }

            if (request.BiografiaImagen is not null)
            {
                await using var stream = request.BiografiaImagen.OpenReadStream();
                var key = await _s3Service.UploadAsync(
                    stream, Path.GetFileName(request.BiografiaImagen.FileName), request.BiografiaImagen.ContentType, "bandas");
                uploadedKeys.Add(key);
                banda.BiografiaImagenUrl = _s3Service.GetPublicUrl(key);
            }

            if (request.Descripcion is not null) banda.Descripcion = request.Descripcion.Trim();
            if (request.Biografia is not null) banda.Biografia = request.Biografia.Trim();
            if (request.Genero is not null) banda.Genero = request.Genero.Trim();
            if (request.PrecioEnvio is not null) banda.PrecioEnvio = request.PrecioEnvio;
            banda.OnboardingStep = 4;
            banda.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch
        {
            foreach (var key in uploadedKeys) await _s3Service.DeleteAsync(key);
            throw;
        }

        if (request.Imagen is not null) await DeleteOldImageAsync(oldImageUrl);
        if (request.BiografiaImagen is not null) await DeleteOldImageAsync(oldBioImageUrl);

        return Ok(new
        {
            banda.Slug,
            banda.LogoUrl,
            banda.ImagenUrl,
            banda.BiografiaImagenUrl,
            banda.Descripcion,
            banda.Biografia,
            banda.Genero,
            banda.PrecioEnvio,
            publicPath = $"/{banda.Slug}",
            onboardingStep = banda.OnboardingStep
        });
    }

    [HttpGet("estado")]
    public async Task<IActionResult> Estado()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var banda = await _context.Bandas
            .AsNoTracking()
            .Where(b => b.UsuarioId == userId)
            .Select(b => new
            {
                b.Id, b.Nombre, b.Slug, b.LogoUrl, b.ImagenUrl,
                b.BiografiaImagenUrl, b.Descripcion, b.Biografia, b.Genero,
                b.PrecioEnvio, b.OnboardingStep
            })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            onboardingStep = banda?.OnboardingStep ?? 2,
            banda,
            publicPath = banda is null ? null : $"/{banda.Slug}"
        });
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out userId) && userId > 0;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private async Task DeleteOldImageAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            await _s3Service.DeleteByUrlAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo borrar una imagen anterior de la banda.");
        }
    }
}
