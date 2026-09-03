namespace Proyecto.DTOs;

public class PublicacionDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int BandaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public DateTime FechaPublicacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
