using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services.Recurrente;

namespace Proyecto.Services.Stock;

public class StockReservationService
{
    private readonly AppDbContext _context;
    private readonly IRecurrenteService _recurrenteService;

    public StockReservationService(
        AppDbContext context,
        IRecurrenteService recurrenteService)
    {
        _context = context;
        _recurrenteService = recurrenteService;
    }

    public async Task LiberarReservasExpiradasAsync()
    {
        // Margen de 30 segundos después de la expiración
        var limite =
            DateTime.UtcNow.AddSeconds(-30);

        var ordenes = await _context.Ordenes
            .Where(o =>
                o.EstadoPago == "pendiente" &&
                o.StockReservado &&
                o.ReservaExpiraAt != null &&
                o.ReservaExpiraAt <= limite &&
                o.RecurrenteCheckoutId != null)
            .ToListAsync();

        foreach (var orden in ordenes)
        {
            // Primero preguntamos a Recurrente
            var checkout =
                await _recurrenteService.GetCheckoutAsync(
                    orden.RecurrenteCheckoutId!);

            // Safety net:
            // si Recurrente dice que se pagó, NO devolvemos stock.
            if (checkout.Status == "paid")
            {
                orden.EstadoPago = "pagado";
                orden.StockReservado = false;
                orden.ReservaExpiraAt = null;
                orden.UpdatedAt = DateTime.UtcNow;

                continue;
            }

            // No está pagado: devolver inventario
            if (orden.EventoId.HasValue && orden.CantidadEntradas.HasValue)
            {
                // Es una compra de entradas
                var evento = await _context.Eventos
                    .FirstAsync(e => e.Id == orden.EventoId.Value);

                evento.EntradasDisponibles =
                    (evento.EntradasDisponibles ?? 0)
                    + orden.CantidadEntradas.Value;

                evento.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Es una compra de merch
                var detalles = await _context.OrdenesDetalle
                    .Where(d => d.OrdenId == orden.Id)
                    .ToListAsync();

                foreach (var detalle in detalles)
                {
                    if (detalle.ProductoVariacionId.HasValue)
                    {
                        var variacion =
                            await _context.ProductoVariaciones
                                .FirstAsync(v =>
                                    v.Id ==
                                    detalle.ProductoVariacionId.Value);

                        variacion.Stock += detalle.Cantidad;
                        variacion.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        var producto =
                            await _context.Productos
                                .FirstAsync(p =>
                                    p.Id == detalle.ProductoId);

                        producto.Stock =
                            (producto.Stock ?? 0)
                            + detalle.Cantidad;

                        producto.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            orden.StockReservado = false;
            orden.EstadoPago = "expirado";
            orden.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
