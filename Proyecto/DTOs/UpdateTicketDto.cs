namespace Proyecto.DTOs;

public class UpdateTicketDto
{
    public int EventoId { get; set; }
    public int OrdenId { get; set; }
    public bool Estado { get; set; }
    public DateTime? UsedDate { get; set; }
}
