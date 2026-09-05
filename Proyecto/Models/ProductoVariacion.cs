using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("producto_variacion")]
public class ProductoVariacion
{
    [Column("id")]
    public int Id { get; set; }

    [Column("uuid")]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Column("producto_id")]
    public int ProductoId { get; set; }

    [Column("nombre")]
    public string? Nombre { get; set; }

    [Column("stock")]
    public int Stock { get; set; } = 0;

    [Column("disponible")]
    public bool Disponible { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}