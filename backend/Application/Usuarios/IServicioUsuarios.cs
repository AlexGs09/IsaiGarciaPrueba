using Backend.Contracts.Usuarios;

namespace Backend.Application.Usuarios;

public interface IServicioUsuarios
{
    Task<PerfilUsuarioResponseDto?> ObtenerPerfilAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
