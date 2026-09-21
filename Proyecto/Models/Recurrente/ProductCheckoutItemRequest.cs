namespace Proyecto.Models.Recurrente
{
    public class ProductCheckoutItemRequest
    {
        public int Id { get; set; }

        public int? VariacionId { get; set; }

        public int Cantidad { get; set; } = 1;
    }
}
