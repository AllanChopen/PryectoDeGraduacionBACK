namespace Proyecto.DTOs;

public class SubscriptorDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int BandaId { get; set; }
    public int UsuarioId { get; set; }
    public string? TipoSuscripcion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime SuscripcionEn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
