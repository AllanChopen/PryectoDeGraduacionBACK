using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;
using Proyecto.Services;
using Proyecto.Services.Email;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context,
        JwtTokenService tokenService,
        IEmailService emailService,
        IConfiguration config,
        ILogger<AuthController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Usuarios.SingleOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
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

        var tokenString = _tokenService.CreateToken(user, bandaId);

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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] SolicitarRecuperacionDto request)
    {
        const string responseMessage = "Si el correo está registrado, recibirás un código para restablecer tu contraseña.";
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null)
        {
            return Ok(new { message = responseMessage });
        }

        var now = DateTime.UtcNow;
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var codeHash = ComputeCodeHash(user.Id, code);

        await using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM usuario WHERE id = {user.Id} FOR UPDATE");

            var reset = await _context.PasswordResetCodes.FindAsync(user.Id);
            if (reset is not null && now - reset.RequestedAt < TimeSpan.FromSeconds(60))
            {
                return Ok(new { message = responseMessage });
            }

            if (reset is null)
            {
                reset = new PasswordResetCode { UsuarioId = user.Id };
                _context.PasswordResetCodes.Add(reset);
            }

            reset.CodeHash = codeHash;
            reset.RequestedAt = now;
            reset.ExpiresAt = now.AddMinutes(10);
            reset.Attempts = 0;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        try
        {
            await _emailService.SendAsync(
                user.Email,
                "Código para restablecer tu contraseña de Backstage",
                $"""
                <!doctype html>
                <html lang="es">
                  <body style="margin:0;padding:24px 12px;background:#0d0107;background-image:linear-gradient(140deg,#0d0107,#100418 55%,#0e213a);font-family:Inter,system-ui,sans-serif;color:#e9e4e6;">
                    <div style="max-width:640px;margin:0 auto;background:#141018;border:1px solid rgba(167,148,148,.22);border-radius:12px;padding:22px;">
                      <p style="margin:0 0 8px 0;color:#a79494;font-size:12px;letter-spacing:.1em;text-transform:uppercase;">Seguridad de la cuenta</p>
                      <h1 style="margin:0 0 14px 0;font-size:26px;line-height:1.2;font-weight:600;color:#e9e4e6;">Restablece tu contraseña</h1>
                      <div style="width:86px;height:3px;border-radius:4px;background:linear-gradient(115deg,#5a4252,#0e213a 58%,#a79494);margin:0 0 18px 0;"></div>
                      <p style="margin:0 0 14px 0;color:#e9e4e6;font-size:15px;">Ingresa este código en Backstage para crear una contraseña nueva:</p>
                      <div style="margin:18px 0;padding:18px 14px;border-radius:10px;border:1px solid rgba(167,148,148,.2);background:#1d1721;text-align:center;color:#e9e4e6;font-size:30px;font-weight:600;letter-spacing:.3em;">{code}</div>
                      <p style="margin:18px 0 0 0;color:#a79494;font-size:13px;">El código vence en 10 minutos. Si no solicitaste este cambio, puedes ignorar este correo.</p>
                    </div>
                  </body>
                </html>
                """);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el código de recuperación al usuario {UserId}.", user.Id);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"DELETE FROM password_reset_code WHERE usuario_id = {user.Id} AND code_hash = {codeHash}");
            }
            catch (Exception cleanupException)
            {
                _logger.LogWarning(cleanupException,
                    "No se pudo invalidar el código no enviado del usuario {UserId}.", user.Id);
            }
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "No se pudo enviar el código. Inténtalo de nuevo más tarde." });
        }

        return Ok(new { message = responseMessage });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ConfirmarRecuperacionDto request)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Las contraseñas no coinciden." });
        }

        const string invalidCodeMessage = "Código inválido o vencido.";
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null)
        {
            return BadRequest(new { message = invalidCodeMessage });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM usuario WHERE id = {user.Id} FOR UPDATE");

        var reset = await _context.PasswordResetCodes.FindAsync(user.Id);
        if (reset is null || reset.ExpiresAt <= DateTime.UtcNow || reset.Attempts >= 5)
        {
            return BadRequest(new { message = invalidCodeMessage });
        }

        var submittedHash = ComputeCodeHash(user.Id, request.Code);
        if (!CryptographicOperations.FixedTimeEquals(reset.CodeHash, submittedHash))
        {
            reset.Attempts++;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return BadRequest(new { message = invalidCodeMessage });
        }

        user.Password = PasswordHasher.Hash(request.NewPassword);
        user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;
        _context.PasswordResetCodes.Remove(reset);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { message = "Contraseña actualizada. Ya puedes iniciar sesión." });
    }

    private byte[] ComputeCodeHash(int userId, string code)
    {
        var secret = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
        var value = Encoding.UTF8.GetBytes($"password-reset:{userId}:{code}");
        return HMACSHA256.HashData(secret, value);
    }
}
