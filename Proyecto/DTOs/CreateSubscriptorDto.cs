namespace Proyecto.DTOs;

public class CreateSubscriptorDto
{
    public int BandaId { get; set; }
    public int UsuarioId { get; set; }
    public string? TipoSuscripcion { get; set; }
    public string Estado { get; set; } = "activo";
}
