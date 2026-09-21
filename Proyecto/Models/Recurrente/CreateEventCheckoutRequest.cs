namespace Proyecto.Models.Recurrente
{
    public class CreateEventCheckoutRequest
    {
        public int EventoId { get; set; }

        public int Cantidad { get; set; } = 1;

        public string? Nombre { get; set; }

        public string? Email { get; set; }

        public string? Telefono { get; set; }
    }
}
