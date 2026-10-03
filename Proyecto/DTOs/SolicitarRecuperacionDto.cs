using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class SolicitarRecuperacionDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
