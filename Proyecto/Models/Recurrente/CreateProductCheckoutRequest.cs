namespace Proyecto.Models.Recurrente
{
    public class CreateProductCheckoutRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string DireccionEnvio { get; set; } = string.Empty;

        public List<ProductCheckoutItemRequest> Items { get; set; } = new();
    }
}
