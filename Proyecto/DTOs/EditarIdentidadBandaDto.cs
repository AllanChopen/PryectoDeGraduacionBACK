using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class EditarIdentidadBandaDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    public IFormFile? Logo { get; set; }
}
