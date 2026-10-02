using Microsoft.AspNetCore.Http;

namespace Proyecto.DTOs;

public class UpdateMiBandaDto
{
    public string? Descripcion { get; set; }
    public string? Biografia { get; set; }
    public string? Genero { get; set; }
    public IFormFile? Portada { get; set; }
    public IFormFile? BiografiaImagen { get; set; }
}
