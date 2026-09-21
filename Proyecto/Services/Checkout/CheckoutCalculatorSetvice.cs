using Microsoft.EntityFrameworkCore;
using Proyecto.Models.Recurrente;

namespace Proyecto.Services.Checkout
{
    public class CheckoutCalculatorService
    {
        private readonly AppDbContext _context;

        public CheckoutCalculatorService(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // PRODUCTOS
        // =========================
        public async Task<CheckoutCalculation> CalculateProductsAsync(
            CreateProductCheckoutRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                throw new Exception("El carrito está vacío.");

            decimal subtotal = 0;
            int? bandaId = null;

            foreach (var item in request.Items)
            {
                if (item.Cantidad <= 0)
                    throw new ArgumentException("La cantidad debe ser mayor a 0.");

                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Id == item.Id);

                if (producto == null)
                    throw new KeyNotFoundException(
                        $"Producto {item.Id} no encontrado.");

                if (!producto.Disponible)
                    throw new InvalidOperationException(
                        $"El producto {producto.Id} no está disponible.");

                // ============================
                // VALIDAR STOCK
                // ============================

                if (item.VariacionId.HasValue)
                {
                    // Producto CON variación seleccionada
                    var variacion = await _context.ProductoVariaciones
                        .FirstOrDefaultAsync(v =>
                            v.Id == item.VariacionId.Value &&
                            v.ProductoId == producto.Id);

                    if (variacion == null)
                    {
                        throw new InvalidOperationException(
                            $"La variación {item.VariacionId.Value} " +
                            $"no pertenece al producto {producto.Id}.");
                    }

                    if (!variacion.Disponible)
                    {
                        throw new InvalidOperationException(
                            $"La variación {variacion.Nombre} no está disponible.");
                    }

                    if (variacion.Stock < item.Cantidad)
                    {
                        throw new InvalidOperationException(
                            $"Stock insuficiente para la variación " +
                            $"{variacion.Nombre}. Disponible: {variacion.Stock}.");
                    }
                }
                else
                {
                    // Producto SIN variación
                    if (producto.Stock < item.Cantidad)
                    {
                        throw new InvalidOperationException(
                            $"Stock insuficiente para el producto {producto.Id}. " +
                            $"Disponible: {producto.Stock}.");
                    }
                }

                ValidarBanda(ref bandaId, producto.BandaId);

                subtotal += producto.Precio * item.Cantidad;
            }

            return await CrearCalculo(bandaId!.Value, subtotal, incluirEnvio: true);
        }

        // =========================
        // EVENTO
        // =========================
        public async Task<CheckoutCalculation> CalculateEventAsync(
            CreateEventCheckoutRequest request)
        {
            if (request.Cantidad <= 0)
                throw new ArgumentException(
                    "La cantidad de entradas debe ser mayor a 0.");

            var evento = await _context.Eventos
                .FirstOrDefaultAsync(e => e.Id == request.EventoId);

            if (evento == null)
                throw new KeyNotFoundException(
                    $"No se encontró el evento {request.EventoId}.");

            if (!evento.PrecioEntrada.HasValue)
                throw new InvalidOperationException(
                    "El evento no tiene un precio de entrada configurado.");

            if (!evento.EntradasDisponibles.HasValue)
                throw new InvalidOperationException(
                    "El evento no tiene aforo disponible configurado.");

            if (evento.EntradasDisponibles.Value < request.Cantidad)
                throw new InvalidOperationException(
                    $"No hay suficientes entradas disponibles. " +
                    $"Disponibles: {evento.EntradasDisponibles.Value}.");

            var subtotal = evento.PrecioEntrada.Value * request.Cantidad;

            // Los tickets de evento se envían por correo, no aplica envío físico.
            return await CrearCalculo(evento.BandaId, subtotal, incluirEnvio: false);
        }

        // =========================
        // PRECIO FINAL
        // =========================
        private async Task<CheckoutCalculation> CrearCalculo(
            int bandaId,
            decimal subtotal,
            bool incluirEnvio)
        {
            var markup = Math.Max(
                subtotal * 0.10m,
                5.00m
            );

            markup = Math.Round(markup, 2);

            decimal costoEnvio = 0m;

            if (incluirEnvio)
            {
                var banda = await _context.Bandas
                    .FirstOrDefaultAsync(b => b.Id == bandaId);

                costoEnvio = banda?.PrecioEnvio ?? 0m;
            }

            var total = subtotal + markup + costoEnvio;

            return new CheckoutCalculation
            {
                BandaId = bandaId,
                Subtotal = subtotal,
                Markup = markup,
                CostoEnvio = costoEnvio,
                Total = total
            };
        }

        private static void ValidarBanda(
            ref int? bandaId,
            int itemBandaId)
        {
            if (bandaId == null)
            {
                bandaId = itemBandaId;
                return;
            }

            if (bandaId != itemBandaId)
            {
                throw new Exception(
                    "Todos los productos deben pertenecer a la misma banda.");
            }
        }
    }

    public class CheckoutCalculation
    {
        public int BandaId { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Markup { get; set; }

        public decimal CostoEnvio { get; set; }

        public decimal Total { get; set; }
    }
}