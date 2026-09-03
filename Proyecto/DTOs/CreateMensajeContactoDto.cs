using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class CreateMensajeContactoDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Mensaje { get; set; } = string.Empty;
}
