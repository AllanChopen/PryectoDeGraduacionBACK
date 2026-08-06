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
    public int UsuarioId { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "pendiente";

    [Column("total")]
    public decimal Total { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
