namespace Proyecto.DTOs;

public class UpdateEventoDto
{
    public int BandaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime Fecha { get; set; }
    public TimeOnly? Hora { get; set; }
    public string? Ubicacion { get; set; }
    public string? UbicacionUrl { get; set; }
    public int? Capacidad { get; set; }
    public decimal? PrecioEntrada { get; set; }
    public string Estado { get; set; } = "programado";
}
