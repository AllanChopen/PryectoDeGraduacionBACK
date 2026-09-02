namespace Proyecto.DTOs;

public class UpdateSubscriptorDto
{
    public int BandaId { get; set; }
    public int UsuarioId { get; set; }
    public string? TipoSuscripcion { get; set; }
    public string Estado { get; set; } = "activo";
    public DateTime? FechaFin { get; set; }
}
