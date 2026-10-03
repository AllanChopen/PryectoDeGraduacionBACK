using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class IdentidadBandaDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public IFormFile Logo { get; set; } = null!;
}
