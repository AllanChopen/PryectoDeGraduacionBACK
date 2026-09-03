using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "bandadmin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;

        if (!int.TryParse(bandaIdClaim, out var bandaId) || bandaId <= 0)
        {
            return Unauthorized("No se encontró la banda del usuario.");
        }

        var dashboard = await _context.BandaStats
            .Where(stats => stats.Id == bandaId)
            .Select(stats => new DashboardDto
            {
                Banda = stats.Nombre,
                Productos = stats.TotalProductos,
                Shows = stats.TotalEventos,
                Noticias = stats.TotalPublicaciones,
                Suscriptores = stats.TotalSubscriptores,
                Mensajes = stats.TotalMensajes
            })
            .FirstOrDefaultAsync();

        if (dashboard is null)
        {
            return NotFound("No se encontraron estadísticas para la banda.");
        }

        return Ok(dashboard);
    }
}
