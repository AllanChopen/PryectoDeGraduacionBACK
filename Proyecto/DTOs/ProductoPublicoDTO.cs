namespace Proyecto.DTOs;

public class ProductoPublicDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Disponible { get; set; }
    public int? Stock { get; set; }
    public bool TieneTalla { get; set; }
    public string? ImagenUrl { get; set; }
    public List<ProductoVariacionDto> Variaciones { get; set; } = new();
}
