namespace Proyecto.DTOs;

public class BandaDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Biografia { get; set; }
    public string? ImagenUrl { get; set; }
    public string? Genero { get; set; }
    public int UsuarioId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
