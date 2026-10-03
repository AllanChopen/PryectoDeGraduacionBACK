using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Proyecto.Models.Recurrente;

public class ConnectedAccountRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    // Por ahora solo se admiten cuentas individuales.
    [Required, RegularExpression("individual")]
    public string AccountType { get; set; } = "individual";

    [Required]
    public string HolderName { get; set; } = string.Empty;

    [Required]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Required]
    public string BankName { get; set; } = string.Empty;

    [Required, RegularExpression("GTQ|USD")]
    public string Currency { get; set; } = "GTQ";

    [Required, RegularExpression("checking|savings")]
    public string BankAccountType { get; set; } = "checking";

    [Required]
    public IFormFile TaxRegistrationDocument { get; set; } = null!;

    [Required]
    public IFormFile IdCardFront { get; set; } = null!;

    [Required]
    public IFormFile IdCardBack { get; set; } = null!;
}
