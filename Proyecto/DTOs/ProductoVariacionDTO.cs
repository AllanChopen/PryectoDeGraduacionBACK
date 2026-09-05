namespace Proyecto.DTOs;

public class ProductoVariacionDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int ProductoId { get; set; }
    public string? Nombre { get; set; }
    public int Stock { get; set; }
    public bool Disponible { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}