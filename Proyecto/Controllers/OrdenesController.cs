using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;
using Proyecto.Services.Email;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdenesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IOrderEmailService _orderEmailService;
    private readonly ILogger<OrdenesController> _logger;

    public OrdenesController(
        AppDbContext context,
        IOrderEmailService orderEmailService,
        ILogger<OrdenesController> logger)
    {
        _context = context;
        _orderEmailService = orderEmailService;
        _logger = logger;
    }

    [Authorize(Roles = "bandadmin")]
    [HttpGet("mis-ordenes/estado-resumen")]
    public async Task<IActionResult> GetEstadoResumen()
    {
        if (!TryGetBandaId(out var bandaId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "No se pudo identificar la banda."
            });
        }

        var ordenes = _context.Ordenes
            .AsNoTracking()
            .Where(o => o.BandaId == bandaId &&
                        !o.EventoId.HasValue &&
                        o.EstadoPago == "pagado");

        var nuevas = await ordenes.CountAsync(o => o.Estado == "nueva");
        var enviadas = await ordenes.CountAsync(o => o.Estado == "enviada");

        return Ok(new { nuevas, enviadas });
    }

    [Authorize(Roles = "bandadmin")]
    [HttpGet("mis-ordenes")]
    public async Task<IActionResult> GetMisOrdenes()
    {
        if (!TryGetBandaId(out var bandaId))
            return Unauthorized("No se encontró la banda del usuario.");

        var ordenes = await _context.Ordenes.AsNoTracking()
            .Where(o => o.BandaId == bandaId && o.EventoId == null && o.EstadoPago == "pagado")
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var ordenIds = ordenes.Select(o => o.Id).ToList();
        var detalles = await _context.OrdenesDetalle.AsNoTracking()
            .Where(d => ordenIds.Contains(d.OrdenId))
            .OrderBy(d => d.Id)
            .ToListAsync();
        var productosPorOrden = detalles.ToLookup(d => d.OrdenId);

        return Ok(ordenes.Select(o => ToMerchOrder(o, productosPorOrden[o.Id])));
    }

    [Authorize(Roles = "bandadmin")]
    [HttpGet("mis-ordenes/{id:int}")]
    public async Task<IActionResult> GetMiOrden(int id)
    {
        if (!TryGetBandaId(out var bandaId))
            return Unauthorized("No se encontró la banda del usuario.");

        var orden = await _context.Ordenes.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.BandaId == bandaId &&
                                      o.EventoId == null && o.EstadoPago == "pagado");
        if (orden is null)
            return NotFound(new { success = false, message = "Orden no encontrada." });

        var detalles = await _context.OrdenesDetalle.AsNoTracking()
            .Where(d => d.OrdenId == id)
            .OrderBy(d => d.Id)
            .ToListAsync();

        return Ok(ToMerchOrder(orden, detalles));
    }

    [Authorize(Roles = "bandadmin")]
    [HttpPost("mis-ordenes/{id:int}/enviar")]
    public async Task<IActionResult> MarcarComoEnviada(int id)
    {
        if (!TryGetBandaId(out var bandaId))
            return Unauthorized("No se encontró la banda del usuario.");

        var orden = await _context.Ordenes
            .FirstOrDefaultAsync(o => o.Id == id && o.BandaId == bandaId && o.EventoId == null);
        if (orden is null)
            return NotFound(new { success = false, message = "Orden no encontrada." });

        if (orden.EstadoPago != "pagado")
            return BadRequest(new { success = false, message = "La orden todavía no ha sido pagada." });

        if (orden.Estado == "enviada")
            return Ok(new { success = true, message = "La orden ya estaba marcada como enviada.", ordenId = orden.Id, estado = orden.Estado });

        if (orden.Estado != "nueva")
            return BadRequest(new { success = false, message = "La orden no se encuentra en estado nueva." });

        // La condición en la base evita que dos solicitudes cambien el estado a la vez.
        var actualizadas = await _context.Ordenes
            .Where(o => o.Id == id && o.BandaId == bandaId && o.EventoId == null &&
                        o.EstadoPago == "pagado" && o.Estado == "nueva")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Estado, "enviada")
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

        if (actualizadas == 0)
            return Conflict(new { success = false, message = "La orden cambió de estado. Actualiza la lista." });

        try
        {
            await _orderEmailService.SendProductOrderShippedToCustomerAsync(id);
            return Ok(new
            {
                success = true,
                message = "Orden marcada como enviada y correo enviado al comprador.",
                correoEnviado = true,
                ordenId = id,
                estado = "enviada"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La orden {OrdenId} se marcó como enviada, pero falló el correo al comprador.", id);
            return Ok(new
            {
                success = true,
                message = "La orden se marcó como enviada, pero no se pudo enviar el correo al comprador.",
                correoEnviado = false,
                ordenId = id,
                estado = "enviada"
            });
        }
    }

    private bool TryGetBandaId(out int bandaId) =>
        int.TryParse(User.FindFirst("BandaId")?.Value, out bandaId) && bandaId > 0;

    private static object ToMerchOrder(Orden orden, IEnumerable<OrdenDetalle> detalles) => new
    {
        orden.Id,
        orden.Uuid,
        orden.CompradorNombre,
        orden.CompradorEmail,
        orden.CompradorTelefono,
        orden.DireccionEnvio,
        orden.Subtotal,
        orden.CostoEnvio,
        orden.TarifaServicio,
        orden.Total,
        orden.EstadoPago,
        orden.Estado,
        orden.CreatedAt,
        orden.UpdatedAt,
        Productos = detalles.Select(d => new
        {
            d.Uuid,
            d.ProductoNombre,
            d.VariacionNombre,
            d.Cantidad,
            d.ImagenUrl,
            d.PrecioUnitario,
            d.Subtotal
        }).ToList()
    };

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Orden>>> GetAll()
    {
        return Ok(await _context.Ordenes.AsNoTracking().ToListAsync());
    }

    [Authorize(Roles = "admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Orden>> GetById(int id)
    {
        var item = await _context.Ordenes.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("checkout/{checkoutId}")]
    public async Task<IActionResult> GetByCheckout(string checkoutId)
    {
        var orden = await _context.Ordenes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.RecurrenteCheckoutId == checkoutId);

        if (orden is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Orden no encontrada."
            });
        }

        if (orden.EventoId.HasValue)
        {
            return BadRequest(new
            {
                success = false,
                message = "Este checkout corresponde a una compra de evento."
            });
        }

        var detalles = await _context.OrdenesDetalle
            .AsNoTracking()
            .Where(d => d.OrdenId == orden.Id)
            .OrderBy(d => d.Id)
            .ToListAsync();

        return Ok(new
        {
            success = true,
            estadoPago = orden.EstadoPago,
            orden = new
            {
                orden.Id,
                orden.Uuid,
                orden.CompradorNombre,
                orden.CompradorEmail,
                orden.CompradorTelefono,
                orden.DireccionEnvio,
                orden.Subtotal,
                orden.CostoEnvio,
                orden.TarifaServicio,
                orden.Total,
                orden.Estado,
                orden.EstadoPago
            },
            productos = detalles.Select(d => new
            {
                d.Uuid,
                d.ProductoNombre,
                d.VariacionNombre,
                d.ImagenUrl,
                d.Cantidad,
                d.PrecioUnitario,
                d.Subtotal
            })
        });
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<Orden>> Create([FromBody] CreateOrdenDto dto)
    {
        var item = new Orden
        {
            Uuid = Guid.NewGuid(),
            UsuarioId = dto.UsuarioId,
            Estado = dto.Estado,
            Total = dto.Total,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        _context.Ordenes.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrdenDto dto)
    {
        var item = await _context.Ordenes.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.UsuarioId = dto.UsuarioId;
        item.Estado = dto.Estado;
        item.Total = dto.Total;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Ordenes.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Ordenes.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
