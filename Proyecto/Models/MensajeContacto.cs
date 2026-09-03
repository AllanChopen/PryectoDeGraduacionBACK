namespace Proyecto.Models;

public class MensajeContacto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int BandaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Estado { get; set; } = "nuevo";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
