using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("banda")]
public class Banda
{
    [Column("id")]
    public int Id { get; set; }

    [Column("uuid")]
    public Guid Uuid { get; set; } = Guid.NewGuid();

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    public string? Descripcion { get; set; }

    [Column("biografia")]
    public string? Biografia { get; set; }

    [Column("imagen_url")]
    public string? ImagenUrl { get; set; }

    [Column("genero")]
    public string? Genero { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("biografia_imagen_url")]
    public string? BiografiaImagenUrl { get; set; }

    // Recurrente
    [Column("recurrente_account_id")]
    public string? RecurrenteAccountId { get; set; }

    [Column("recurrente_account_status")]
    public string? RecurrenteAccountStatus { get; set; }

    [Column("recurrente_onboarding_completed")]
    public bool RecurrenteOnboardingCompleted { get; set; } = false;
    
    [Column("precio_envio")]
    public decimal? PrecioEnvio { get; set; }
}