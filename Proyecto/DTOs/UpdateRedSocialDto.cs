using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class UpdateRedSocialDto
{
    [Required]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;
}
