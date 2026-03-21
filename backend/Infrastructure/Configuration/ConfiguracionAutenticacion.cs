namespace Backend.Infrastructure.Configuration;

public sealed class ConfiguracionAutenticacion
{
    public int MaximoIntentosFallidosLogin { get; set; } = 4;
    public int MinutosBloqueoPorIntentos { get; set; } = 15;
    public int MinutosDuracionTokenAcceso { get; set; } = 20;
    public int DiasDuracionRefreshToken { get; set; } = 7;
    public string EmisorJwt { get; set; } = "Backend.PruebaTecnica";
    public string AudienciaJwt { get; set; } = "Frontend.PruebaTecnica";
    public string LlaveSecretaJwt { get; set; } = "CLAVE_SUPER_SECRETA_DESARROLLO_2026_PRUEBA_TECNICA";
}
