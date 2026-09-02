namespace Proyecto.DTOs;

public class UpdatePublicacionDto
{
    public int BandaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public string Estado { get; set; } = "publicada";
}
