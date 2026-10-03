using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Proyecto.Models;
using Proyecto.Models.Recurrente;
using Proyecto.Services.Checkout;
using Proyecto.Services.Email;
using Proyecto.Services.Recurrente;
using Svix;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecurrenteController : ControllerBase
{
    private readonly IRecurrenteService _recurrenteService;
    private readonly CheckoutCalculatorService _checkoutCalculator;
    private readonly RecurrenteSettings _recurrenteSettings;
    private readonly AppDbContext _context;
    private readonly IOrderEmailService _orderEmailService;
    private readonly IConfiguration _configuration;

    public RecurrenteController(
        IRecurrenteService recurrenteService,
        CheckoutCalculatorService checkoutCalculator,
        IOptions<RecurrenteSettings> recurrenteSettings,
        AppDbContext context,
        IOrderEmailService orderEmailService,
        IConfiguration configuration)
    {
        _recurrenteService = recurrenteService;
        _checkoutCalculator = checkoutCalculator;
        _recurrenteSettings = recurrenteSettings.Value;
        _context = context;
        _orderEmailService = orderEmailService;
        _configuration = configuration;
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var response = await _recurrenteService.TestConnectionAsync();

            return Ok(new
            {
                success = true,
                recurrenteResponse = response
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "bandadmin")]
    [HttpPost("connected-account")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> CreateConnectedAccount(
        [FromForm] ConnectedAccountRequest request)
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        if (!int.TryParse(bandaIdClaim, out var bandaId) || bandaId <= 0)
        {
            return Unauthorized("No se pudo identificar la banda del usuario.");
        }

        var banda = await _context.Bandas.FirstOrDefaultAsync(b => b.Id == bandaId);
        if (banda is null)
        {
            return NotFound("Banda no encontrada.");
        }

        if (!string.IsNullOrWhiteSpace(banda.RecurrenteAccountId))
        {
            return Conflict("La banda ya tiene una cuenta de Recurrente.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.HolderName) ||
            string.IsNullOrWhiteSpace(request.BankAccountNumber) ||
            string.IsNullOrWhiteSpace(request.BankName) ||
            request.TaxRegistrationDocument is null || request.TaxRegistrationDocument.Length == 0 ||
            request.IdCardFront is null || request.IdCardFront.Length == 0 ||
            request.IdCardBack is null || request.IdCardBack.Length == 0)
        {
            return BadRequest("Completa los datos bancarios y adjunta los tres documentos requeridos.");
        }

        try
        {
            var response = await _recurrenteService.CreateConnectedAccountAsync(
                banda.Nombre,
                request);

            banda.RecurrenteAccountId = response.Id;
            banda.RecurrenteAccountStatus = response.Status;
            banda.RecurrenteOnboardingCompleted = response.OnboardingCompleted;
            banda.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                accountId = response.Id,
                status = response.Status,
                onboardingCompleted = response.OnboardingCompleted
            });
        }
        catch (HttpRequestException ex) when (ex.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError)
        {
            return BadRequest(new
            {
                success = false,
                error = "Recurrente rechazó los datos o documentos enviados. Revisa el formulario."
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "No se pudo configurar la cuenta de Recurrente."
            });
        }
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "bandadmin")]
    [HttpGet("banks")]
    public async Task<ActionResult<IReadOnlyList<RecurrenteBankDto>>> GetBanks(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _recurrenteService.GetBanksAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            return StatusCode(502, new { message = "No se pudo obtener la lista de bancos de Recurrente." });
        }
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "bandadmin")]
    [HttpPost("subscription/checkout")]
    public async Task<IActionResult> CreateBackstageSubscriptionCheckout()
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        if (!int.TryParse(bandaIdClaim, out var bandaId) || bandaId <= 0)
        {
            return Unauthorized("No se pudo identificar la banda del usuario.");
        }

        var banda = await _context.Bandas.FindAsync(bandaId);
        if (banda is null)
        {
            return NotFound("Banda no encontrada.");
        }

        if (string.IsNullOrWhiteSpace(banda.RecurrenteAccountId))
        {
            return Conflict("Configura primero la cuenta para recibir pagos.");
        }

        if (!string.IsNullOrWhiteSpace(banda.RecurrenteSubscriptionId))
        {
            return Conflict("La banda ya tiene una suscripción de Backstage.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(banda.RecurrenteSubscriptionCheckoutId))
            {
                var existing = await _recurrenteService.GetCheckoutAsync(
                    banda.RecurrenteSubscriptionCheckoutId);
                if (existing.Status == "unpaid" && !string.IsNullOrWhiteSpace(existing.CheckoutUrl))
                {
                    return Ok(new { checkoutId = existing.Id, checkoutUrl = existing.CheckoutUrl });
                }

                if (existing.Status != "expired")
                {
                    return Conflict("El checkout anterior está en proceso; espera la confirmación.");
                }
            }

            var baseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/');
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var frontendUri) ||
                (frontendUri.Scheme != Uri.UriSchemeHttps && frontendUri.Scheme != Uri.UriSchemeHttp))
            {
                return StatusCode(500, "No se configuró Frontend:BaseUrl.");
            }

            var checkout = await _recurrenteService.CreateBackstageSubscriptionCheckoutAsync(
                $"{baseUrl}/onboarding/completed",
                $"{baseUrl}/onboarding/subscription");

            banda.RecurrenteSubscriptionCheckoutId = checkout.Id;
            banda.SuscripcionSetupConfirmado = false;
            banda.SuscripcionEstado = "pending";
            banda.SuscripcionFinPeriodo = null;
            banda.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { checkoutId = checkout.Id, checkoutUrl = checkout.CheckoutUrl });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, "No se pudo crear o consultar el checkout en Recurrente.");
        }
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "bandadmin")]
    [HttpGet("subscription/status")]
    public async Task<IActionResult> GetBackstageSubscriptionStatus()
    {
        var bandaIdClaim = User.FindFirst("BandaId")?.Value;
        if (!int.TryParse(bandaIdClaim, out var bandaId) || bandaId <= 0)
        {
            return Unauthorized();
        }

        var banda = await _context.Bandas.FindAsync(bandaId);
        if (banda is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(banda.RecurrenteSubscriptionId))
        {
            try
            {
                var subscription = await _recurrenteService.GetSubscriptionAsync(
                    banda.RecurrenteSubscriptionId);
                banda.SuscripcionEstado = subscription.Status;
                banda.SuscripcionFinPeriodo = subscription.CurrentPeriodEnd;
                if (banda.SuscripcionSetupConfirmado && subscription.Status == "active")
                {
                    banda.OnboardingCompletado = true;
                }

                banda.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, "No se pudo consultar la suscripción en Recurrente.");
            }
        }

        return Ok(new
        {
            subscriptionId = banda.RecurrenteSubscriptionId,
            status = banda.SuscripcionEstado,
            currentPeriodEnd = banda.SuscripcionFinPeriodo,
            onboardingCompleted = banda.OnboardingCompletado,
            hasAccess = banda.OnboardingCompletado && banda.SuscripcionEstado == "active"
        });
    }

    [HttpPost("checkout/productos")]
    public async Task<IActionResult> CreateProductCheckout(
        [FromBody] CreateProductCheckoutRequest request)
    {
        try
        {
            // 1. Calcular todo usando precios reales de BD
            var calculation =
                await _checkoutCalculator.CalculateProductsAsync(request);

            var reservaExpiraAt = DateTime.UtcNow.AddMinutes(15);

            // 2. Crear orden local
            var orden = new Orden
            {
                Uuid = Guid.NewGuid(),

                // Compra anónima
                UsuarioId = null,
                BandaId = calculation.BandaId,

                // Datos del comprador
                CompradorNombre = request.Nombre,
                CompradorEmail = request.Email,
                CompradorTelefono = request.Telefono,
                DireccionEnvio = request.DireccionEnvio,

                // Snapshot financiero
                Subtotal = calculation.Subtotal,
                TarifaServicio = calculation.Markup,
                CostoEnvio = calculation.CostoEnvio,
                Total = calculation.Total,

                // Estados independientes
                Estado = "nueva",
                EstadoPago = "pendiente",
                StockReservado = true,
                ReservaExpiraAt = reservaExpiraAt,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };

            _context.Ordenes.Add(orden);

            // Necesitamos orden.Id para OrdenDetalle
            await _context.SaveChangesAsync();

            // 3. Crear detalles y reservar stock
            foreach (var item in request.Items)
            {
                var producto = await _context.Productos
                    .FirstAsync(p => p.Id == item.Id);

                // ========================================
                // RESERVAR STOCK
                // ========================================

                string? variacionNombre = null;

                if (item.VariacionId.HasValue)
                {
                    // Producto con variación
                    var variacion = await _context.ProductoVariaciones
                        .FirstAsync(v =>
                            v.Id == item.VariacionId.Value &&
                            v.ProductoId == producto.Id);

                    if (variacion.Stock < item.Cantidad)
                    {
                        throw new InvalidOperationException(
                            $"Ya no hay stock suficiente para la variación {variacion.Nombre}."
                        );
                    }

                    variacion.Stock -= item.Cantidad;
                    variacion.UpdatedAt = DateTime.UtcNow;

                    variacionNombre = variacion.Nombre;
                }
                else
                {
                    // Producto sin variación
                    if ((producto.Stock ?? 0) < item.Cantidad)
                    {
                        throw new InvalidOperationException(
                            $"Ya no hay stock suficiente para el producto {producto.Id}."
                        );
                    }

                    producto.Stock -= item.Cantidad;
                    producto.UpdatedAt = DateTime.UtcNow;
                }

                // ========================================
                // GUARDAR SNAPSHOT DE LA COMPRA
                // ========================================

                var detalle = new OrdenDetalle
                {
                    Uuid = Guid.NewGuid(),

                    OrdenId = orden.Id,
                    ProductoId = producto.Id,
                    ProductoVariacionId = item.VariacionId,

                    ProductoNombre = producto.Nombre,
                    VariacionNombre = variacionNombre,
                    ImagenUrl = producto.ImagenUrl,

                    Cantidad = item.Cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = producto.Precio * item.Cantidad,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.OrdenesDetalle.Add(detalle);
            }

            // La orden ahora tiene inventario reservado
            orden.StockReservado = true;

            await _context.SaveChangesAsync();

            // 4. Crear checkout en Recurrente
            var banda = await _context.Bandas
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == calculation.BandaId);
            if (banda is null)
            {
                throw new KeyNotFoundException("No se encontró la banda de la orden.");
            }

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                throw new InvalidOperationException("No está configurada Frontend:BaseUrl.");
            }

            var recurrenteResponse =
                await _recurrenteService.CreateCheckoutAsync(
                    calculation,
                    reservaExpiraAt,
                    $"{frontendBaseUrl}/{banda.Slug}/checkout/productos/exito",
                    $"{frontendBaseUrl}/{banda.Slug}/carrito");

            // 5. Asociar checkout de Recurrente con nuestra orden
            orden.RecurrenteCheckoutId = recurrenteResponse.Id;
            orden.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 6. Respuesta al frontend
            return Ok(new
            {
                success = true,

                orden = new
                {
                    id = orden.Id,
                    uuid = orden.Uuid,
                    estado = orden.Estado,
                    estadoPago = orden.EstadoPago
                },

                calculation = new
                {
                    subtotal = calculation.Subtotal,
                    tarifaServicio = calculation.Markup,
                    costoEnvio = calculation.CostoEnvio,
                    total = calculation.Total
                },

                recurrente = new
                {
                    checkoutId = recurrenteResponse.Id,
                    checkoutUrl = recurrenteResponse.CheckoutUrl,
                    status = recurrenteResponse.Status,
                    totalInCents = recurrenteResponse.TotalInCents
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("checkout/evento")]
    public async Task<IActionResult> CreateEventCheckout(
        [FromBody] CreateEventCheckoutRequest request)
    {
        try
        {
            var calculation =
                await _checkoutCalculator.CalculateEventAsync(request);

            var reservaExpiraAt = DateTime.UtcNow.AddMinutes(15);

            // Reservar aforo
            var evento = await _context.Eventos
                .FirstOrDefaultAsync(e => e.Id == request.EventoId);

            if (evento == null)
            {
                throw new KeyNotFoundException(
                    $"No se encontró el evento {request.EventoId}.");
            }

            var banda = await _context.Bandas
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == evento.BandaId);

            if (banda == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "No se encontró la banda del evento."
                });
            }

            var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                throw new InvalidOperationException(
                    "No está configurada la URL base del frontend (Frontend:BaseUrl).");
            }

            var successUrl = $"{frontendBaseUrl}/{banda.Slug}/checkout/evento/exito";
            var cancelUrl = $"{frontendBaseUrl}/{banda.Slug}/shows/{evento.Id}";

            if (!evento.EntradasDisponibles.HasValue)
            {
                throw new InvalidOperationException(
                    "El evento no tiene aforo disponible configurado.");
            }

            if (evento.EntradasDisponibles.Value < request.Cantidad)
            {
                throw new InvalidOperationException(
                    $"No hay suficientes entradas disponibles. " +
                    $"Disponibles: {evento.EntradasDisponibles.Value}.");
            }

            evento.EntradasDisponibles -= request.Cantidad;
            evento.UpdatedAt = DateTime.UtcNow;

            var orden = new Orden
            {
                UsuarioId = null,

                CompradorNombre = request.Nombre,
                CompradorEmail = request.Email,
                CompradorTelefono = request.Telefono,

                BandaId = evento.BandaId,
                EventoId = evento.Id,
                CantidadEntradas = request.Cantidad,

                // Snapshot del evento
                EventoNombre = evento.Nombre,
                EventoFecha = evento.Fecha,
                EventoHora = evento.Hora,
                EventoUbicacion = evento.Ubicacion,
                EventoImagenUrl = evento.ImagenUrl,
                PrecioEntrada = evento.PrecioEntrada,

                Subtotal = calculation.Subtotal,
                TarifaServicio = calculation.Markup,
                CostoEnvio = 0,
                Total = calculation.Total,

                Estado = "nueva",
                EstadoPago = "pendiente",

                StockReservado = true,
                ReservaExpiraAt = reservaExpiraAt,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Ordenes.Add(orden);

            await _context.SaveChangesAsync();

            var recurrenteResponse =
                await _recurrenteService.CreateCheckoutAsync(
                    calculation,
                    reservaExpiraAt,
                    successUrl,
                    cancelUrl);

            orden.RecurrenteCheckoutId = recurrenteResponse.Id;
            orden.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,

                orden = new
                {
                    orden.Id,
                    orden.Uuid,
                    orden.EventoId,
                    orden.CantidadEntradas,
                    orden.EstadoPago,
                    orden.StockReservado,
                    orden.ReservaExpiraAt
                },

                calculation = new
                {
                    calculation.Subtotal,
                    calculation.Markup,
                    calculation.Total
                },

                recurrente = new
                {
                    checkoutId = recurrenteResponse.Id,
                    checkoutUrl = recurrenteResponse.CheckoutUrl,
                    status = recurrenteResponse.Status,
                    totalInCents = recurrenteResponse.TotalInCents
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        // ============================
        // 1. VERIFICAR FIRMA SVIX
        // ============================
        try
        {
            var wh = new Webhook(
                _recurrenteSettings.WebhookSecret
            );

            var headers = new WebHeaderCollection();

            foreach (var header in Request.Headers)
            {
                headers.Add(header.Key, header.Value.ToString());
            }

            wh.Verify(body, headers);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Webhook con firma inválida: {ex.Message}"
            );

            return Unauthorized();
        }

        // ============================
        // 2. DESERIALIZAR
        // ============================

        using var document = JsonDocument.Parse(body);
        var payload = document.RootElement;
        if (payload.TryGetProperty("data", out var nestedData) &&
            nestedData.ValueKind == JsonValueKind.Object)
        {
            payload = nestedData;
        }

        var eventType = payload.TryGetProperty("event_type", out var eventTypeElement)
            ? eventTypeElement.GetString()
            : null;

        if (eventType is "setup_intent.succeeded" or "setup_intent.cancelled" ||
            eventType?.StartsWith("subscription.", StringComparison.Ordinal) == true)
        {
            return await HandleBackstageSubscriptionWebhookAsync(payload, eventType!);
        }

        var webhookEvent =
            JsonSerializer.Deserialize<RecurrenteWebhookEvent>(payload.GetRawText());

        if (webhookEvent == null)
            return BadRequest();

        Console.WriteLine("===== RECURRENTE WEBHOOK =====");
        Console.WriteLine($"Event: {webhookEvent.EventType}");
        Console.WriteLine($"Payment: {webhookEvent.Id}");
        Console.WriteLine($"Status: {webhookEvent.Status}");
        Console.WriteLine($"Amount: {webhookEvent.AmountInCents}");
        Console.WriteLine($"Checkout: {webhookEvent.Checkout?.Id}");
        Console.WriteLine($"Checkout Status: {webhookEvent.Checkout?.Status}");
        Console.WriteLine("==============================");

        // ============================
        // 3. SOLO EVENTO UNIFICADO
        // ============================

        if (webhookEvent.EventType != "intent.succeeded")
            return Ok();

        // ============================
        // 4. BUSCAR ORDEN POR CHECKOUT
        // ============================

        var checkoutId = webhookEvent.Checkout?.Id;

        if (string.IsNullOrWhiteSpace(checkoutId))
        {
            Console.WriteLine("Webhook sin checkout ID.");
            return BadRequest();
        }

        var orden = await _context.Ordenes
            .FirstOrDefaultAsync(o =>
                o.RecurrenteCheckoutId == checkoutId);

        if (orden == null)
        {
            Console.WriteLine(
                $"No existe orden para checkout {checkoutId}"
            );

            return Ok();
        }

        // ============================
        // 5. CONFIRMAR PAGO EXITOSO
        // ============================

        if (webhookEvent.Status != "succeeded" ||
            webhookEvent.Checkout?.Status != "paid")
        {
            Console.WriteLine(
                $"Pago no confirmado para orden {orden.Id}"
            );

            return Ok();
        }

        // ============================
        // 6. IDEMPOTENCIA
        // ============================

        if (orden.EstadoPago == "pagado")
        {
            Console.WriteLine(
                $"Orden {orden.Id} ya estaba pagada. Ignorando webhook repetido."
            );

            return Ok(new
            {
                success = true,
                message = "La orden ya había sido procesada."
            });
        }

        // ============================
        // 7. VERIFICAR MONTO
        // ============================

        var totalEsperadoEnCentavos =
            (int)Math.Round(
                orden.Total * 100,
                MidpointRounding.AwayFromZero
            );

        if (webhookEvent.AmountInCents != totalEsperadoEnCentavos)
        {
            Console.WriteLine(
                $"Monto incorrecto. " +
                $"Orden: {totalEsperadoEnCentavos}, " +
                $"Recurrente: {webhookEvent.AmountInCents}"
            );

            return BadRequest();
        }

        // ============================
        // 8. MARCAR COMO PAGADA
        // ============================

        orden.EstadoPago = "pagado";
        orden.StockReservado = false;
        orden.ReservaExpiraAt = null;
        orden.UpdatedAt = DateTime.UtcNow;

        // =====================================================
        // SI ES UNA COMPRA DE EVENTO, GENERAR LOS TICKETS
        // =====================================================

        if (orden.EventoId.HasValue &&
            orden.CantidadEntradas.HasValue)
        {
            for (int i = 0; i < orden.CantidadEntradas.Value; i++)
            {
                var ticket = new Ticket
                {
                    EventoId = orden.EventoId.Value,
                    OrdenId = orden.Id,

                    // Aunque el modelo ya tiene default,
                    // lo dejamos explícito para que quede claro.
                    CodigoQr = Guid.NewGuid().ToString("N"),

                    // true = ticket válido / disponible para usar
                    Estado = true,

                    UsedDate = null,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                };

                _context.Tickets.Add(ticket);
            }
        }

        await _context.SaveChangesAsync();

        Console.WriteLine(
            $"Orden {orden.Id} marcada como PAGADA."
        );

        // =====================================================
        // NOTIFICACIONES POR CORREO
        // =====================================================

        if (!orden.EventoId.HasValue)
        {
            // Compra de merch
            try
            {
                await _orderEmailService
                    .SendProductOrderToBandAsync(orden.Id);

                // Mailtrap Sandbox Free: 1 email / 10 segundos.
                // Hack únicamente para pruebas locales de la tesis.
                await Task.Delay(TimeSpan.FromSeconds(11));

                await _orderEmailService
                    .SendProductOrderConfirmationToCustomerAsync(orden.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error enviando correos de la orden #{orden.Id}: {ex.Message}");
            }
        }
        else
        {
            try
            {
                await _orderEmailService.SendEventTicketsToCustomerAsync(orden.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error enviando tickets de la orden #{orden.Id}: {ex.Message}");
            }
        }

        return Ok(new
        {
            success = true,
            message = "Pago procesado correctamente."
        });
    }

    private async Task<IActionResult> HandleBackstageSubscriptionWebhookAsync(
        JsonElement payload,
        string eventType)
    {
        if (eventType is "setup_intent.succeeded" or "setup_intent.cancelled")
        {
            if (!payload.TryGetProperty("checkout", out var checkout) ||
                !checkout.TryGetProperty("id", out var checkoutIdValue))
            {
                return Ok();
            }

            var checkoutId = checkoutIdValue.GetString();
            var banda = await _context.Bandas.FirstOrDefaultAsync(b =>
                b.RecurrenteSubscriptionCheckoutId == checkoutId);
            if (banda is null)
            {
                return Ok();
            }

            if (eventType == "setup_intent.succeeded" &&
                payload.TryGetProperty("status", out var setupStatus) &&
                setupStatus.GetString() == "succeeded")
            {
                banda.SuscripcionSetupConfirmado = true;
                if (!string.IsNullOrWhiteSpace(banda.RecurrenteSubscriptionId) &&
                    banda.SuscripcionEstado == "active")
                {
                    banda.OnboardingCompletado = true;
                }
            }
            else if (eventType == "setup_intent.cancelled" &&
                     !banda.SuscripcionSetupConfirmado)
            {
                banda.SuscripcionEstado = "setup_failed";
            }

            banda.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok();
        }

        if (eventType is not ("subscription.create" or "subscription.past_due" or
            "subscription.cancel" or "subscription.pause" or "subscription.unpause" or
            "subscription.reactivate" or "subscription.update"))
        {
            return Ok();
        }

        if (!payload.TryGetProperty("id", out var subscriptionIdValue))
        {
            return Ok();
        }

        var subscriptionId = subscriptionIdValue.GetString();
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return Ok();
        }

        var subscription = await _recurrenteService.GetSubscriptionAsync(subscriptionId);
        Banda? target;
        if (eventType == "subscription.create")
        {
            var checkoutId = subscription.Checkout?.Id;
            target = string.IsNullOrWhiteSpace(checkoutId)
                ? null
                : await _context.Bandas.FirstOrDefaultAsync(b =>
                    b.RecurrenteSubscriptionCheckoutId == checkoutId);
            if (target is not null &&
                !string.IsNullOrWhiteSpace(target.RecurrenteSubscriptionId) &&
                target.RecurrenteSubscriptionId != subscriptionId)
            {
                return Ok();
            }
        }
        else
        {
            target = await _context.Bandas.FirstOrDefaultAsync(b =>
                b.RecurrenteSubscriptionId == subscriptionId);
        }

        if (target is null)
        {
            return Ok();
        }

        target.RecurrenteSubscriptionId = subscription.Id;
        target.SuscripcionEstado = subscription.Status;
        target.SuscripcionFinPeriodo = subscription.CurrentPeriodEnd;
        if (target.SuscripcionSetupConfirmado && subscription.Status == "active")
        {
            target.OnboardingCompletado = true;
        }

        target.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok();
    }
}
