using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class CreateEventoDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
    public DateTime Fecha { get; set; }
    public TimeOnly? Hora { get; set; }
    public string? Ubicacion { get; set; }
    public string? UbicacionUrl { get; set; }
    public IFormFile? Imagen { get; set; }
    public int? Capacidad { get; set; }
    public decimal? PrecioEntrada { get; set; }
    public string Estado { get; set; } = "Upcoming";
}
