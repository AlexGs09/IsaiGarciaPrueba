namespace Backend.Contracts.Usuarios;

public sealed class PerfilUsuarioResponseDto
{
    public string Dni { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public string Nacionalidad { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string NumeroCelular { get; set; } = string.Empty;
}
