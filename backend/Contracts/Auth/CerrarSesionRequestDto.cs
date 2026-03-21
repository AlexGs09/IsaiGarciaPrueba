using System.ComponentModel.DataAnnotations;

namespace Backend.Contracts.Auth;

public sealed class CerrarSesionRequestDto
{
    [Required(ErrorMessage = "El token JTI es obligatorio.")]
    public string TokenJti { get; set; } = string.Empty;
}
