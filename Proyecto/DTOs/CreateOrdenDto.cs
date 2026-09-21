namespace Proyecto.DTOs;

public class CreateOrdenDto
{
    public int? UsuarioId { get; set; }
    public string Estado { get; set; } = "pendiente";
    public decimal Total { get; set; }
}
