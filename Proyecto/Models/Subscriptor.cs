using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("subscriptor")]
public class Subscriptor
{
    [Column("id")]
    public int Id { get; set; }

    [Column("uuid")]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Column("banda_id")]
    public int BandaId { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("tipo_suscripcion")]
    public string? TipoSuscripcion { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "activo";

    [Column("fecha_inicio")]
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;

    [Column("fecha_fin")]
    public DateTime? FechaFin { get; set; }

    [Column("suscripción_en")]
    public DateTime SuscripcionEn { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
