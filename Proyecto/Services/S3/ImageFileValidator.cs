namespace PryectoDeGraduacionBACK.Services.S3;

public static class ImageFileValidator
{
    public static string? Validate(IFormFile? image)
    {
        if (image is null)
        {
            return null;
        }

        if (image.Length == 0)
        {
            return "La imagen está vacía.";
        }

        if (string.IsNullOrWhiteSpace(image.ContentType) ||
            !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return "El archivo debe ser una imagen.";
        }

        return null;
    }
}
