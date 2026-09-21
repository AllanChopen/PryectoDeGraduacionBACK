namespace Proyecto.DTOs;

public class OrdenDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int? UsuarioId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
