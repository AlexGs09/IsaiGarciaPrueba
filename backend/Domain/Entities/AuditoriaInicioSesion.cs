using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public sealed class AuditoriaInicioSesion
{
    public long Id { get; set; }
    public Guid? UsuarioId { get; set; }
    public string IdentificadorIngresado { get; set; } = string.Empty;
    public TipoIdentificadorIngreso TipoIdentificador { get; set; } = TipoIdentificadorIngreso.Desconocido;
    public ResultadoAuditoriaInicioSesion Resultado { get; set; } = ResultadoAuditoriaInicioSesion.Ok;
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime FechaEvento { get; set; }

    public Usuario? Usuario { get; set; }
}
