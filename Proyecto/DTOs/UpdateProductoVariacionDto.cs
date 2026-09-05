namespace Proyecto.DTOs;

public class UpdateProductoVariacionDto
{
    public string? Nombre { get; set; }
    public int Stock { get; set; }
    public bool Disponible { get; set; } = true;
}
