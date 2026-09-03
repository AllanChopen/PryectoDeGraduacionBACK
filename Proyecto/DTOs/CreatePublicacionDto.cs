using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class CreatePublicacionDto
{
    [Required]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    public string Contenido { get; set; } = string.Empty;

    public IFormFile? Imagen { get; set; }
}
