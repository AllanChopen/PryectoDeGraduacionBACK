using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

[Table("password_reset_code")]
public class PasswordResetCode
{
    [Key]
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("code_hash")]
    public byte[] CodeHash { get; set; } = [];

    [Column("requested_at")]
    public DateTime RequestedAt { get; set; }

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("attempts")]
    public int Attempts { get; set; }
}
