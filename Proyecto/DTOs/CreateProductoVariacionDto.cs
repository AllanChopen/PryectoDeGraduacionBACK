namespace Proyecto.DTOs;

public class CreateProductoVariacionDto
{
    public string? Nombre { get; set; }
    public decimal? Precio { get; set; }
    public int Stock { get; set; }
    public bool Disponible { get; set; } = true;
    public string? Atributos { get; set; }
}
