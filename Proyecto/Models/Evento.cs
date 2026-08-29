using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("evento")]
public class Evento
{
    [Column("id")]
    public int Id { get; set; }

    [Column("uuid")]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Column("banda_id")]
    public int BandaId { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; }

    [Column("hora")]
    public TimeOnly? Hora { get; set; }

    [Column("ubicacion")]
    public string? Ubicacion { get; set; }

    [Column("ubicacion_url")]
    public string? UbicacionUrl { get; set; }

    [Column("capacidad")]
    public int? Capacidad { get; set; }

    [Column("precio_entrada")]
    public decimal? PrecioEntrada { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "programado";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
