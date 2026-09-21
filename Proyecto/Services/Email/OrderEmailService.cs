using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using System.Net;
using System.Text;

namespace Proyecto.Services.Email;

public class OrderEmailService : IOrderEmailService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public OrderEmailService(
        AppDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task SendProductOrderToBandAsync(int ordenId)
    {
        // 1. Buscar la orden
        var orden = await _context.Ordenes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ordenId);

        if (orden == null)
            throw new KeyNotFoundException(
                $"No se encontró la orden {ordenId}.");

        // Seguridad: este correo es únicamente para merch
        if (orden.EventoId.HasValue)
            throw new InvalidOperationException(
                "La orden corresponde a un evento.");

        if (orden.EstadoPago != "pagado")
            throw new InvalidOperationException(
                "La orden todavía no ha sido pagada.");

        // 2. Obtener detalles
        var detalles = await _context.OrdenesDetalle
            .AsNoTracking()
            .Where(d => d.OrdenId == orden.Id)
            .OrderBy(d => d.Id)
            .ToListAsync();

        if (detalles.Count == 0)
            throw new InvalidOperationException(
                "La orden no contiene productos.");

        // 3. Usamos un producto únicamente para determinar la banda.
        // Los datos mostrados en el email vienen de los snapshots.
        var primerProducto = await _context.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == detalles[0].ProductoId);

        if (primerProducto == null)
            throw new KeyNotFoundException(
                "No se encontró el producto asociado a la orden.");

        // 4. Banda
        var banda = await _context.Bandas
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.Id == primerProducto.BandaId);

        if (banda == null)
            throw new KeyNotFoundException(
                "No se encontró la banda asociada a la orden.");

        // 5. Usuario dueño de la banda
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == banda.UsuarioId);

        if (usuario == null ||
            string.IsNullOrWhiteSpace(usuario.Email))
        {
            throw new InvalidOperationException(
                "La banda no tiene un correo configurado.");
        }

        // 6. Construir HTML
        var html = BuildProductOrderBandEmail(
            orden,
            detalles,
            banda);

        // 7. Enviar
        await _emailService.SendAsync(
            usuario.Email,
            $"Nueva venta #{orden.Id} - {banda.Nombre}",
            html);
    }

    private static string BuildProductOrderBandEmail(
        Orden orden,
        List<OrdenDetalle> detalles,
        Banda banda)
    {
        var items = BuildProductItemsHtml(detalles);

        return $"""
            <!doctype html>
            <html lang="es">
              <body style="margin:0;padding:24px 12px;background:#0d0107;background-image:linear-gradient(140deg,#0d0107,#100418 55%,#0e213a);font-family:Inter,system-ui,sans-serif;color:#e9e4e6;">
                <div style="max-width:640px;margin:0 auto;background:#141018;border:1px solid rgba(167,148,148,.22);border-radius:12px;padding:22px;">
                  <p style="margin:0 0 8px 0;color:#a79494;font-size:12px;letter-spacing:.1em;text-transform:uppercase;">Nueva venta</p>
                  <h1 style="margin:0 0 14px 0;font-size:26px;line-height:1.2;font-weight:600;color:#e9e4e6;">Tienes una nueva venta</h1>
                  <div style="width:86px;height:3px;border-radius:4px;background:linear-gradient(115deg,#5a4252,#0e213a 58%,#a79494);margin:0 0 18px 0;"></div>

                  <p style="margin:0 0 14px 0;color:#e9e4e6;font-size:15px;">
                      Se ha confirmado una nueva compra de productos de
                      <strong>{WebUtility.HtmlEncode(banda.Nombre)}</strong>.
                  </p>

                  <p style="margin:0 0 6px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;">Orden</p>
                  <p style="margin:0 0 14px 0;color:#e9e4e6;font-size:15px;">#{orden.Id} &middot; Estado del pago: Pagado</p>

                  <p style="margin:18px 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;">Comprador</p>
                  <div style="margin-bottom:14px;padding:14px;border-radius:10px;border:1px solid rgba(167,148,148,.2);background:#1d1721;line-height:1.6;color:#e9e4e6;font-size:15px;">
                      <strong>Nombre:</strong> {WebUtility.HtmlEncode(orden.CompradorNombre)}<br>
                      <strong>Email:</strong> {WebUtility.HtmlEncode(orden.CompradorEmail)}<br>
                      <strong>Teléfono:</strong> {WebUtility.HtmlEncode(orden.CompradorTelefono)}<br>
                      <strong>Dirección de envío:</strong> {WebUtility.HtmlEncode(orden.DireccionEnvio)}
                  </div>

                  <p style="margin:18px 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;">Productos</p>

                  {items}

                  <p style="margin:18px 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;">Resumen</p>
                  <div style="padding:14px;border-radius:10px;border:1px solid rgba(167,148,148,.2);background:#1d1721;line-height:1.6;color:#e9e4e6;font-size:15px;">
                      Subtotal: Q{orden.Subtotal:0.00}<br>
                      Envío: Q{orden.CostoEnvio:0.00}<br>
                      Tarifa de servicio: Q{orden.TarifaServicio:0.00}<br>
                      <strong>Total pagado: Q{orden.Total:0.00}</strong>
                  </div>

                  <p style="margin:18px 0 0 0;color:#a79494;font-size:13px;">
                      Esta orden está pagada y pendiente de ser procesada para su envío.
                  </p>
                </div>
              </body>
            </html>
            """;
    }

    public async Task SendProductOrderConfirmationToCustomerAsync(
        int ordenId)
    {
        // 1. Buscar la orden
        var orden = await _context.Ordenes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ordenId);

        if (orden == null)
            throw new KeyNotFoundException(
                $"No se encontró la orden {ordenId}.");

        // Seguridad: este correo es únicamente para merch
        if (orden.EventoId.HasValue)
            throw new InvalidOperationException(
                "La orden corresponde a un evento.");

        if (orden.EstadoPago != "pagado")
            throw new InvalidOperationException(
                "La orden todavía no ha sido pagada.");

        if (string.IsNullOrWhiteSpace(orden.CompradorEmail))
            throw new InvalidOperationException(
                "La orden no tiene un correo de comprador configurado.");

        // 2. Obtener detalles
        var detalles = await _context.OrdenesDetalle
            .AsNoTracking()
            .Where(d => d.OrdenId == orden.Id)
            .OrderBy(d => d.Id)
            .ToListAsync();

        if (detalles.Count == 0)
            throw new InvalidOperationException(
                "La orden no contiene productos.");

        // 3. Construir HTML
        var html = BuildProductOrderCustomerEmail(orden, detalles);

        // 4. Enviar directamente al comprador (ya congelado en la orden)
        await _emailService.SendAsync(
            orden.CompradorEmail,
            $"Compra confirmada - Orden #{orden.Id}",
            html);
    }

    private static string BuildProductOrderCustomerEmail(
        Orden orden,
        List<OrdenDetalle> detalles)
    {
        var items = BuildProductItemsHtml(detalles);

        return $"""
            <!doctype html>
            <html lang="es">
              <body style="margin:0;padding:24px 12px;background:#0d0107;background-image:linear-gradient(140deg,#0d0107,#100418 55%,#0e213a);font-family:Inter,system-ui,sans-serif;color:#e9e4e6;">
                <div style="max-width:640px;margin:0 auto;background:#141018;border:1px solid rgba(167,148,148,.22);border-radius:12px;padding:22px;">
                  <p style="margin:0 0 8px 0;color:#a79494;font-size:12px;letter-spacing:.1em;text-transform:uppercase;">Pago confirmado</p>
                  <h1 style="margin:0 0 14px 0;font-size:26px;line-height:1.2;font-weight:600;color:#e9e4e6;">¡Gracias por tu compra!</h1>
                  <div style="width:86px;height:3px;border-radius:4px;background:linear-gradient(115deg,#5a4252,#0e213a 58%,#a79494);margin:0 0 18px 0;"></div>

                  <p style="margin:0 0 14px 0;color:#e9e4e6;font-size:15px;">
                      Hola {WebUtility.HtmlEncode(orden.CompradorNombre)}, hemos confirmado el pago de tu compra.
                  </p>

                  <p style="margin:0 0 6px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;">Orden</p>
                  <p style="margin:0 0 14px 0;color:#e9e4e6;font-size:15px;">#{orden.Id} &middot; Estado del pago: Pagado</p>

                  <p style="margin:18px 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;">Envío</p>
                  <div style="margin-bottom:14px;padding:14px;border-radius:10px;border:1px solid rgba(167,148,148,.2);background:#1d1721;line-height:1.6;color:#e9e4e6;font-size:15px;">
                      <strong>Dirección de envío:</strong> {WebUtility.HtmlEncode(orden.DireccionEnvio)}
                  </div>

                  <p style="margin:18px 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;">Productos</p>

                  {items}

                  <p style="margin:18px 0 10px 0;color:#a79494;font-size:12px;letter-spacing:.08em;text-transform:uppercase;">Resumen</p>
                  <div style="padding:14px;border-radius:10px;border:1px solid rgba(167,148,148,.2);background:#1d1721;line-height:1.6;color:#e9e4e6;font-size:15px;">
                      Subtotal: Q{orden.Subtotal:0.00}<br>
                      Envío: Q{orden.CostoEnvio:0.00}<br>
                      Tarifa de servicio: Q{orden.TarifaServicio:0.00}<br>
                      <strong>Total pagado: Q{orden.Total:0.00}</strong>
                  </div>

                  <p style="margin:18px 0 0 0;color:#a79494;font-size:13px;">
                      Te avisaremos cuando tu pedido sea enviado.
                  </p>
                </div>
              </body>
            </html>
            """;
    }

    private static string BuildProductItemsHtml(List<OrdenDetalle> detalles)
    {
        var items = new StringBuilder();

        foreach (var detalle in detalles)
        {
            var productoNombre = WebUtility.HtmlEncode(
                detalle.ProductoNombre ?? "Producto");

            var variacionNombre = WebUtility.HtmlEncode(
                detalle.VariacionNombre);

            var imagenUrl = WebUtility.HtmlEncode(
                detalle.ImagenUrl);

            items.Append("""<div style="display:flex;gap:14px;align-items:flex-start;margin-bottom:16px;padding:14px;border-radius:10px;border:1px solid rgba(167,148,148,.2);background:#1d1721;">""");

            if (!string.IsNullOrWhiteSpace(imagenUrl))
            {
                items.Append($"""
                    <img
                        src="{imagenUrl}"
                        alt="{productoNombre}"
                        style="width:72px;height:72px;object-fit:cover;border-radius:8px;flex-shrink:0;"
                    />
                    """);
            }

            items.Append($"""
                <div style="color:#e9e4e6;font-size:15px;line-height:1.6;">
                    <strong>{productoNombre}</strong><br>
                """);

            if (!string.IsNullOrWhiteSpace(variacionNombre))
            {
                items.Append($"""
                    Variación: {variacionNombre}<br>
                    """);
            }

            items.Append($"""
                    Cantidad: {detalle.Cantidad}<br>
                    Precio unitario: Q{detalle.PrecioUnitario:0.00}<br>
                    Subtotal: Q{detalle.Subtotal:0.00}
                </div>
            </div>
            """);
        }

        return items.ToString();
    }
}
