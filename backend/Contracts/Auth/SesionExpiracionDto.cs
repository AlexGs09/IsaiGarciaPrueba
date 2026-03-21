namespace Backend.Contracts.Auth;

public sealed class SesionExpiracionDto
{
    public bool MostrarAvisoExpiracion { get; set; }
    public int MinutosInactividadConfigurados { get; set; }
    public int MinutosAvisoExpiracion { get; set; }
    public DateTime ExpiraEn { get; set; }
}
