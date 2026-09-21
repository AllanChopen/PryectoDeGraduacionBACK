using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.DTOs;
using Proyecto.Models;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TicketsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ticket>>> GetAll()
    {
        return Ok(await _context.Tickets.AsNoTracking().ToListAsync());
    }

    [HttpGet("checkout/{checkoutId}")]
    public async Task<IActionResult> GetTicketsByCheckout(string checkoutId)
    {
        var orden = await _context.Ordenes
            .AsNoTracking()
            .FirstOrDefaultAsync(o =>
                o.RecurrenteCheckoutId == checkoutId);

        if (orden == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Compra no encontrada."
            });
        }

        // Todavía estamos esperando confirmación del webhook
        if (orden.EstadoPago == "pendiente")
        {
            return Ok(new
            {
                success = true,
                estadoPago = orden.EstadoPago,
                message = "Confirmando el pago..."
            });
        }

        if (orden.EstadoPago != "pagado")
        {
            return Ok(new
            {
                success = false,
                estadoPago = orden.EstadoPago,
                message = "La compra no se encuentra pagada."
            });
        }

        // Esta ruta es específicamente para entradas
        if (!orden.EventoId.HasValue)
        {
            return BadRequest(new
            {
                success = false,
                message = "Esta compra no corresponde a un evento."
            });
        }

        var evento = await _context.Eventos
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.Id == orden.EventoId.Value);

        if (evento == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Evento no encontrado."
            });
        }

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.OrdenId == orden.Id)
            .OrderBy(t => t.Id)
            .Select(t => new
            {
                t.Uuid,
                t.CodigoQr,
                t.Estado,
                t.UsedDate
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            estadoPago = orden.EstadoPago,

            compra = new
            {
                orden.Id,
                orden.Uuid,
                orden.CompradorNombre,
                orden.CompradorEmail,
                orden.CantidadEntradas,
                orden.Total
            },

            evento = new
            {
                evento.Id,
                evento.Uuid,
                evento.Nombre,
                evento.Fecha,
                evento.Hora,
                evento.Ubicacion
            },

            tickets
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Ticket>> GetById(int id)
    {
        var item = await _context.Tickets.FindAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("qr/{codigoQr}")]
    public async Task<IActionResult> GetByQr(string codigoQr)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CodigoQr == codigoQr);

        if (ticket == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Ticket no encontrado."
            });
        }

        var evento = await _context.Eventos
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ticket.EventoId);

        if (evento == null)
        {
            return NotFound(new
            {
                success = false,
                message = "El evento asociado al ticket no existe."
            });
        }

        return Ok(new
        {
            success = true,

            ticket = new
            {
                ticket.Uuid,
                ticket.CodigoQr,
                ticket.Estado,
                ticket.UsedDate,

                evento = new
                {
                    evento.Id,
                    evento.Uuid,
                    evento.Nombre,
                    evento.Fecha,
                    evento.Hora,
                    evento.Ubicacion
                }
            }
        });
    }

    [HttpPost("qr/{codigoQr}/canjear")]
    public async Task<IActionResult> CanjearTicket(string codigoQr)
    {
        var ticket = await _context.Tickets
            .FirstOrDefaultAsync(t => t.CodigoQr == codigoQr);

        if (ticket == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Ticket no encontrado."
            });
        }

        if (!ticket.Estado || ticket.UsedDate.HasValue)
        {
            return BadRequest(new
            {
                success = false,
                message = "Este ticket ya fue utilizado.",
                usedDate = ticket.UsedDate
            });
        }

        ticket.Estado = false;
        ticket.UsedDate = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            message = "Ticket canjeado correctamente.",
            ticket = new
            {
                ticket.Uuid,
                ticket.CodigoQr,
                ticket.Estado,
                ticket.UsedDate
            }
        });
    }

    [HttpPost]
    public async Task<ActionResult<Ticket>> Create([FromBody] CreateTicketDto dto)
    {
        var item = new Ticket
        {
            Uuid = Guid.NewGuid(),
            EventoId = dto.EventoId,
            OrdenId = dto.OrdenId,
            Estado = dto.Estado,
            CodigoQr = Guid.NewGuid().ToString("N"),
            UsedDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        _context.Tickets.Add(item);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTicketDto dto)
    {
        var item = await _context.Tickets.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        item.EventoId = dto.EventoId;
        item.OrdenId = dto.OrdenId;
        item.Estado = dto.Estado;
        item.UsedDate = dto.UsedDate;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Tickets.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.Tickets.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
