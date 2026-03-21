using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public sealed class SesionUsuario
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string TokenJti { get; set; } = string.Empty;
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime IniciaEn { get; set; }
    public DateTime ExpiraEn { get; set; }
    public DateTime RefreshTokenExpiraEn { get; set; }
    public DateTime UltimoMovimientoEn { get; set; }
    public DateTime? CerradaEn { get; set; }
    public MotivoCierreSesion? MotivoCierre { get; set; }

    public Usuario? Usuario { get; set; }
}
