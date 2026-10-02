using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class CreateUsuarioDto
{
    [Required]
    [MinLength(2)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
