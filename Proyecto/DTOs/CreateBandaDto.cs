namespace Proyecto.DTOs;

public class CreateBandaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Biografia { get; set; }
    public string? ImagenUrl { get; set; }
    public string? BiografiaImagenUrl { get; set; }
    public string? Genero { get; set; }
    public int UsuarioId { get; set; }
    public string Slug { get; set; } = string.Empty;
}
