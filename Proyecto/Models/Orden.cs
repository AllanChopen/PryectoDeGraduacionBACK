using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("orden")]
public class Orden
{
    [Column("id")]
    public int Id { get; set; }

    [Column("uuid")]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Column("usuario_id")]
    public int? UsuarioId { get; set; }

    [Column("banda_id")]
    public int? BandaId { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "nueva";

    [Column("total")]
    public decimal Total { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Column("estado_pago")]
    public string EstadoPago { get; set; } = "pendiente";

    [Column("recurrente_checkout_id")]
    public string? RecurrenteCheckoutId { get; set; }

    [Column("comprador_nombre")]
    public string? CompradorNombre { get; set; }

    [Column("comprador_email")]
    public string? CompradorEmail { get; set; }

    [Column("comprador_telefono")]
    public string? CompradorTelefono { get; set; }

    [Column("direccion_envio")]
    public string? DireccionEnvio { get; set; }

    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("tarifa_servicio")]
    public decimal TarifaServicio { get; set; }
    
    [Column("costo_envio")]
    public decimal CostoEnvio { get; set; }
    
    [Column("stock_reservado")]
    public bool StockReservado { get; set; } = false;
    
    [Column("reserva_expira_at")]
    public DateTime? ReservaExpiraAt { get; set; }
    
    [Column("evento_id")]
    public int? EventoId { get; set; }

    [Column("cantidad_entradas")]
    public int? CantidadEntradas { get; set; }

    [Column("evento_nombre")]
    public string? EventoNombre { get; set; }

    [Column("evento_fecha")]
    public DateTime? EventoFecha { get; set; }

    [Column("evento_hora")]
    public TimeOnly? EventoHora { get; set; }

    [Column("evento_ubicacion")]
    public string? EventoUbicacion { get; set; }

    [Column("evento_imagen_url")]
    public string? EventoImagenUrl { get; set; }

    [Column("precio_entrada")]
    public decimal? PrecioEntrada { get; set; }
}
