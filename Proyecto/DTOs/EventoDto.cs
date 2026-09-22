namespace Proyecto.DTOs;

public class EventoDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int BandaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime Fecha { get; set; }
    public TimeOnly? Hora { get; set; }
    public string? Ubicacion { get; set; }
    public string? UbicacionUrl { get; set; }
    public string? ImagenUrl { get; set; }
    public int? Capacidad { get; set; }
    public int? EntradasDisponibles { get; set; }
    public decimal? PrecioEntrada { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
