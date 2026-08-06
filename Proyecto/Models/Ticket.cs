using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("ticket")]
public class Ticket
{
    [Column("id")]
    public int Id { get; set; }

    [Column("uuid")]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Column("evento_id")]
    public int EventoId { get; set; }

    [Column("orden_id")]
    public int OrdenId { get; set; }

    [Column("codigo_qr")]
    public string CodigoQr { get; set; } = Guid.NewGuid().ToString("N");

    [Column("estado")]
    public bool Estado { get; set; }

    [Column("used_date")]
    public DateTime? UsedDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
