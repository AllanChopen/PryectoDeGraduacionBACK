namespace Proyecto.Models;

public class BandaStats
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public long TotalSubscriptores { get; set; }
    public long TotalProductos { get; set; }
    public long TotalEventos { get; set; }
    public long TotalPublicaciones { get; set; }
    public long TotalMensajes { get; set; }
}
