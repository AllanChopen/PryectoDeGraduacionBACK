namespace Proyecto.DTOs;

public class PersonalizacionBandaDto
{
    public string? Descripcion { get; set; }
    public string? Biografia { get; set; }
    public string? Genero { get; set; }
    public decimal? PrecioEnvio { get; set; }
    public IFormFile? Imagen { get; set; }
    public IFormFile? BiografiaImagen { get; set; }
}
