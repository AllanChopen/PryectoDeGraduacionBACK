namespace Proyecto.DTOs;

public class TicketDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public int EventoId { get; set; }
    public int OrdenId { get; set; }
    public string CodigoQr { get; set; } = string.Empty;
    public bool Estado { get; set; }
    public DateTime? UsedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
