using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public sealed class Usuario
{
    public Guid Id { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string ContrasenaHash { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string PrimerApellido { get; set; } = string.Empty;
    public string? SegundoApellido { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public string Nacionalidad { get; set; } = string.Empty;
    public string NumeroCelular { get; set; } = string.Empty;
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;
    public DateTime? BloqueadoHasta { get; set; }
    public string? MotivoBloqueo { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? UltimoLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<SesionUsuario> Sesiones { get; set; } = [];
    public ICollection<AuditoriaInicioSesion> AuditoriasInicioSesion { get; set; } = [];
}
