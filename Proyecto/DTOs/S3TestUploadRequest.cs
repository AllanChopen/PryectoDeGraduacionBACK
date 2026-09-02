using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class S3TestUploadRequest
{
    [Required]
    public required IFormFile Image { get; init; }
}
