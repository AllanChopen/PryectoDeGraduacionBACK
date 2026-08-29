namespace Proyecto.DTOs;

public class BandaDto
{
    public Guid Uuid { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Biografia { get; set; }
    public string? ImagenUrl { get; set; }
    public string? Genero { get; set; }
}