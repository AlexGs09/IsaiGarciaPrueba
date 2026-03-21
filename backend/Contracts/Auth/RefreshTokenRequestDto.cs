using System.ComponentModel.DataAnnotations;

namespace Backend.Contracts.Auth;

public sealed class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "El refresh token es obligatorio.")]
    public string RefreshToken { get; set; } = string.Empty;
}
