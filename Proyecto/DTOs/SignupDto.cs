using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Proyecto.DTOs;

public class SignupDto
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

    [Required]
    [MinLength(2)]
    public string BandaNombre { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    public IFormFile? Logo { get; set; }
}
