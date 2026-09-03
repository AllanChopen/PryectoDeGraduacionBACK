using Microsoft.AspNetCore.Mvc;
using Proyecto.DTOs;
using PryectoDeGraduacionBACK.Services.S3;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/[controller]")]
public class S3Controller : ControllerBase
{
    private readonly IS3Service _s3Service;

    public S3Controller(IS3Service s3Service)
    {
        _s3Service = s3Service;
    }

    [HttpPost("test-upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> TestUpload([FromForm] S3TestUploadRequest request)
    {
        var image = request.Image;

        var imageError = ImageFileValidator.Validate(image);
        if (imageError is not null)
        {
            return BadRequest(imageError);
        }

        await using var stream = image.OpenReadStream();
        var key = await _s3Service.UploadAsync(
            stream,
            Path.GetFileName(image.FileName),
            image.ContentType,
            "test-uploads");

        return Ok(new
        {
            message = "Imagen subida correctamente.",
            key
        });
    }
}
