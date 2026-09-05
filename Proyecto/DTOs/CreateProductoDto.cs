using System.ComponentModel.DataAnnotations;

namespace Proyecto.DTOs;

public class CreateProductoDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Disponible { get; set; } = true;
    public int? Stock { get; set; }
    public bool TieneTalla { get; set; } = false;
    public IFormFile? Imagen { get; set; }
}
