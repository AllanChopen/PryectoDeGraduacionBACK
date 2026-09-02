namespace Proyecto.DTOs;

public class UpdateUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Password optional: if present and non-empty, will be hashed and updated
    public string? Password { get; set; }
    public string Rol { get; set; } = string.Empty;
}
