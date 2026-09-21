using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("orden_detalle")]
public class OrdenDetalle
{
    [Column("id")]
    public int Id { get; set; }

    [Column("uuid")]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Column("orden_id")]
    public int OrdenId { get; set; }

    [Column("producto_id")]
    public int ProductoId { get; set; }

    [Column("producto_variacion_id")]
    public int? ProductoVariacionId { get; set; }

    [Column("producto_nombre")]
    public string? ProductoNombre { get; set; }

    [Column("variacion_nombre")]
    public string? VariacionNombre { get; set; }

    [Column("imagen_url")]
    public string? ImagenUrl { get; set; }

    [Column("cantidad")]
    public int Cantidad { get; set; }

    [Column("precio_unitario")]
    public decimal PrecioUnitario { get; set; }

    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
