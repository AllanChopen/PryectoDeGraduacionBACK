namespace Proyecto.DTOs;

public class OrdenDetalleDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int OrdenId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
