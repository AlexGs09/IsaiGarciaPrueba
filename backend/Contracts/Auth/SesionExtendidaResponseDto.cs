namespace Backend.Contracts.Auth;

public sealed class SesionExtendidaResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public DateTime UltimoMovimientoEn { get; set; }
}
