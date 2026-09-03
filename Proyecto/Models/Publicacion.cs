using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("publicacion")]
public class Publicacion
{
    [Column("id")]
    public int Id { get; set; }

    [Column("uuid")]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Column("banda_id")]
    public int BandaId { get; set; }

    [Column("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Column("contenido")]
    public string Contenido { get; set; } = string.Empty;

    [Column("imagen_url")]
    public string? ImagenUrl { get; set; }

    [Column("fecha_publicacion")]
    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
