using System.ComponentModel.DataAnnotations;

namespace Backend.Contracts.Auth;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "El identificador es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El identificador debe tener entre 3 y 150 caracteres.")]
    public string Identificador { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contrasena es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contrasena debe tener al menos 8 caracteres.")]
    public string Contrasena { get; set; } = string.Empty;
}
