namespace Proyecto.DTOs;

public class ProductoVariacionDto
{
    public Guid Uuid { get; set; }
    public string? Nombre { get; set; }
    public decimal? Precio { get; set; }
    public int Stock { get; set; }
    public bool Disponible { get; set; }
    public string? Atributos { get; set; }
}