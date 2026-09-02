namespace Proyecto.DTOs;

public class ProductoPublicDto
{
    public Guid Uuid { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Disponible { get; set; }
    public int? Stock { get; set; }
    public List<ProductoVariacionDto> Variaciones { get; set; } = new();
}