using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Proyecto.Models.Recurrente;

public class CreateConnectedAccountRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public IFormFile TaxRegistrationDocument { get; set; } = null!;

    [Required]
    public IFormFile IdCardImage { get; set; } = null!;

    [Required]
    public IFormFile IdCardImageBack { get; set; } = null!;
}
