using Microsoft.AspNetCore.Http;

namespace Proyecto.DTOs;

public class UpdateBandaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Biografia { get; set; }
    public string? ImagenUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? BiografiaImagenUrl { get; set; }
    public IFormFile? Imagen { get; set; }
    public IFormFile? Logo { get; set; }
    public IFormFile? BiografiaImagen { get; set; }
    public string? Genero { get; set; }
    public int UsuarioId { get; set; }
    public string Slug { get; set; } = string.Empty;
}
