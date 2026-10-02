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

        if (!await HasActiveBackstageSubscriptionAsync(bandaId))
        {
            return StatusCode(403, "Activa tu suscripción de Backstage para entrar al dashboard.");
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

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        if (!int.TryParse(bandaIdClaim, out var bandaId) || bandaId <= 0)
        {
            return Unauthorized(new
            {
                success = false,
                message = "No se pudo identificar la banda."
            });
        }

        if (!await HasActiveBackstageSubscriptionAsync(bandaId))
        {
            return StatusCode(403, "Activa tu suscripción de Backstage para entrar al dashboard.");
        }

        var merchOrders = _context.Ordenes.AsNoTracking()
            .Where(o => o.BandaId == bandaId &&
                        o.EventoId == null &&
                        o.EstadoPago == "pagado");

        var eventOrders = _context.Ordenes.AsNoTracking()
            .Where(o => o.EventoId.HasValue &&
                        o.EstadoPago == "pagado" &&
                        (o.BandaId == bandaId || (o.BandaId == null && _context.Eventos.Any(e =>
                            e.Id == o.EventoId && e.BandaId == bandaId))));

        var ahoraUtc = DateTime.UtcNow;
        var inicioMesActual = new DateTime(
            ahoraUtc.Year, ahoraUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var inicioMesSiguiente = inicioMesActual.AddMonths(1);
        var inicioMesAnterior = inicioMesActual.AddMonths(-1);

        var ordenesNuevas = await merchOrders.CountAsync(o => o.Estado == "nueva");
        var ordenesEnviadas = await merchOrders.CountAsync(o => o.Estado == "enviada");
        var ingresosMerch = await merchOrders.SumAsync(o => (decimal?)o.Subtotal) ?? 0m;
        var ingresosMerchMesActual = await merchOrders
            .Where(o => o.CreatedAt >= inicioMesActual && o.CreatedAt < inicioMesSiguiente)
            .SumAsync(o => (decimal?)o.Subtotal) ?? 0m;
        var ingresosEventosMesActual = await eventOrders
            .Where(o => o.CreatedAt >= inicioMesActual && o.CreatedAt < inicioMesSiguiente)
            .SumAsync(o => (decimal?)o.Subtotal) ?? 0m;
        var ingresosMesAnterior = await _context.Ordenes.AsNoTracking()
            .Where(o => o.EstadoPago == "pagado" &&
                        o.CreatedAt >= inicioMesAnterior &&
                        o.CreatedAt < inicioMesActual &&
                        (o.BandaId == bandaId ||
                         (o.BandaId == null && o.EventoId.HasValue && _context.Eventos.Any(e =>
                             e.Id == o.EventoId && e.BandaId == bandaId))))
            .SumAsync(o => (decimal?)o.Subtotal) ?? 0m;
        var ingresosMesActual = ingresosMerchMesActual + ingresosEventosMesActual;
        decimal? variacionVsMesAnterior = ingresosMesAnterior == 0m
            ? null
            : Math.Round((ingresosMesActual - ingresosMesAnterior) * 100m / ingresosMesAnterior, 2);

        var productosVendidos = await (
            from detalle in _context.OrdenesDetalle.AsNoTracking()
            join orden in merchOrders on detalle.OrdenId equals orden.Id
            select (int?)detalle.Cantidad
        ).SumAsync() ?? 0;

        var topProductos = await (
            from detalle in _context.OrdenesDetalle.AsNoTracking()
            join orden in merchOrders on detalle.OrdenId equals orden.Id
            group detalle by new { detalle.ProductoId, detalle.ProductoNombre } into grupo
            orderby grupo.Sum(d => d.Cantidad) descending
            select new
            {
                productoId = grupo.Key.ProductoId,
                nombre = grupo.Key.ProductoNombre ?? "Producto",
                unidadesVendidas = grupo.Sum(d => d.Cantidad)
            }
        ).Take(3).ToListAsync();

        var entradasVendidas = await eventOrders
            .SumAsync(o => (int?)o.CantidadEntradas) ?? 0;
        var ingresosEventos = await eventOrders
            .SumAsync(o => (decimal?)o.Subtotal) ?? 0m;
        var eventosConVentas = await eventOrders
            .Select(o => o.EventoId)
            .Distinct()
            .CountAsync();

        var topEventos = await eventOrders
            .GroupBy(o => new { o.EventoId, o.EventoNombre, o.EventoUbicacion })
            .Select(grupo => new
            {
                eventoId = grupo.Key.EventoId,
                nombre = grupo.Key.EventoNombre ?? "Evento",
                ubicacion = grupo.Key.EventoUbicacion,
                entradasVendidas = grupo.Sum(o => o.CantidadEntradas ?? 0),
                ingresos = grupo.Sum(o => o.Subtotal)
            })
            .OrderByDescending(evento => evento.entradasVendidas)
            .Take(3)
            .ToListAsync();

        var merchRecent = await merchOrders
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new
            {
                tipo = "merch",
                ordenId = o.Id,
                fecha = o.CreatedAt,
                estado = o.Estado,
                eventoNombre = (string?)null,
                cantidadEntradas = (int?)null,
                monto = o.Total
            })
            .ToListAsync();

        var eventRecent = await eventOrders
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new
            {
                tipo = "evento",
                ordenId = o.Id,
                fecha = o.CreatedAt,
                estado = "pagada",
                eventoNombre = o.EventoNombre,
                cantidadEntradas = o.CantidadEntradas,
                monto = o.Total
            })
            .ToListAsync();

        var actividadReciente = merchRecent
            .Concat(eventRecent)
            .OrderByDescending(actividad => actividad.fecha)
            .Take(10)
            .ToList();

        return Ok(new
        {
            ingresos = new
            {
                historico = ingresosMerch + ingresosEventos,
                mesActual = ingresosMesActual,
                merchMesActual = ingresosMerchMesActual,
                eventosMesActual = ingresosEventosMesActual,
                mesAnterior = ingresosMesAnterior,
                variacionPorcentualVsMesAnterior = variacionVsMesAnterior
            },
            ordenes = new
            {
                nuevas = ordenesNuevas,
                enviadas = ordenesEnviadas
            },
            merch = new
            {
                unidadesVendidas = productosVendidos,
                topProductos
            },
            eventos = new
            {
                ticketsVendidos = entradasVendidas,
                eventosConVentas,
                topEventos
            },
            actividadReciente
        });
    }

    private Task<bool> HasActiveBackstageSubscriptionAsync(int bandaId) =>
        _context.Bandas.AsNoTracking().AnyAsync(b =>
            b.Id == bandaId &&
            b.OnboardingCompletado &&
            b.SuscripcionEstado == "active");
}
