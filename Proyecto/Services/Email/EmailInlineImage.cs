namespace Proyecto.Services.Email;

public class EmailInlineImage
{
    public string ContentId { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/png";
}
