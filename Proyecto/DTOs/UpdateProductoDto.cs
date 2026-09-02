namespace Proyecto.DTOs;

public class UpdateProductoDto
{
    public int BandaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Disponible { get; set; } = true;
    public int? Stock { get; set; }
}
