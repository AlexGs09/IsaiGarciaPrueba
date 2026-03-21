namespace Backend.Contracts.Auth;

public sealed class UsuarioSesionDto
{
    public Guid Id { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public string Correo { get; set; } = string.Empty;
}
