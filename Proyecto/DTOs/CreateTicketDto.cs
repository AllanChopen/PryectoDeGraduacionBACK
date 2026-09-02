namespace Proyecto.DTOs;

public class CreateTicketDto
{
    public int EventoId { get; set; }
    public int OrdenId { get; set; }
    public bool Estado { get; set; }
}
