using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class SendTestEmailDto
{
    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;
}
