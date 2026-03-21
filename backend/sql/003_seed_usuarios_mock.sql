SET NOCOUNT ON;

DECLARE @UsuarioActivoId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @UsuarioPreBloqueoId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @UsuarioBloqueadoTemporalId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @UsuarioBloqueadoSistemaId UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
DECLARE @UsuarioInactivoId UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555555';

DECLARE @AhoraUtc DATETIME2(0) = GETUTCDATE();

DELETE FROM dbo.AuditoriaInicioSesion
WHERE UsuarioId IN (
    @UsuarioActivoId,
    @UsuarioPreBloqueoId,
    @UsuarioBloqueadoTemporalId,
    @UsuarioBloqueadoSistemaId,
    @UsuarioInactivoId
);

DELETE FROM dbo.SesionesUsuario
WHERE UsuarioId IN (
    @UsuarioActivoId,
    @UsuarioPreBloqueoId,
    @UsuarioBloqueadoTemporalId,
    @UsuarioBloqueadoSistemaId,
    @UsuarioInactivoId
);

DELETE FROM dbo.Usuarios
WHERE Id IN (
    @UsuarioActivoId,
    @UsuarioPreBloqueoId,
    @UsuarioBloqueadoTemporalId,
    @UsuarioBloqueadoSistemaId,
    @UsuarioInactivoId
);

INSERT INTO dbo.Usuarios
(
    Id,
    Dni,
    Username,
    Correo,
    ContrasenaHash,
    Nombres,
    PrimerApellido,
    SegundoApellido,
    FechaNacimiento,
    Nacionalidad,
    NumeroCelular,
    Estado,
    BloqueadoHasta,
    MotivoBloqueo,
    IntentosFallidos,
    UltimoLoginAt,
    CreatedAt,
    UpdatedAt
)
VALUES
(
    @UsuarioActivoId,
    '70000001',
    'usuario.activo',
    'activo@pruebatecnica.local',
    '$2a$11$M9So/dZW4riombP/A8pTmuDO0FG9Yc2hrCVHg7Jqm0bVV3B.H8L9O',
    'Carlos Andres',
    'Ramirez',
    'Soto',
    '1994-05-10',
    'Peruana',
    '987654321',
    'ACTIVO',
    NULL,
    NULL,
    0,
    NULL,
    @AhoraUtc,
    @AhoraUtc
),
(
    @UsuarioPreBloqueoId,
    '70000002',
    'usuario.prebloqueo',
    'prebloqueo@pruebatecnica.local',
    '$2a$11$M9So/dZW4riombP/A8pTmuDO0FG9Yc2hrCVHg7Jqm0bVV3B.H8L9O',
    'Lucia Fernanda',
    'Martinez',
    'Rojas',
    '1997-11-22',
    'Peruana',
    '912345678',
    'ACTIVO',
    NULL,
    NULL,
    3,
    NULL,
    @AhoraUtc,
    @AhoraUtc
),
(
    @UsuarioBloqueadoTemporalId,
    '70000003',
    'usuario.temporal',
    'temporal@pruebatecnica.local',
    '$2a$11$OtlT0r104rlzJsuJT00zCuF75Acfg/ZwJWhqqSze4cCRUag4iX2zi',
    'Miguel Angel',
    'Paredes',
    'Lopez',
    '1991-03-15',
    'Peruana',
    '923456789',
    'ACTIVO',
    DATEADD(MINUTE, 15, @AhoraUtc),
    'BLOQUEO_POR_INTENTOS',
    4,
    NULL,
    @AhoraUtc,
    @AhoraUtc
),
(
    @UsuarioBloqueadoSistemaId,
    '70000004',
    'usuario.sistema',
    'bloqueadosistema@pruebatecnica.local',
    '$2a$11$y9w7R0HKpkAOOEfU.sxKDuFKyc7SYBNgblGPBj2uOoyec8yUsUsv2',
    'Sandra Milagros',
    'Quispe',
    'Navarro',
    '1989-08-03',
    'Peruana',
    '934567891',
    'BLOQUEADO_SISTEMA',
    NULL,
    'BLOQUEO_MANUAL_SISTEMA',
    0,
    NULL,
    @AhoraUtc,
    @AhoraUtc
),
(
    @UsuarioInactivoId,
    '70000005',
    'usuario.inactivo',
    'inactivo@pruebatecnica.local',
    '$2a$11$uEnxNPbax2QY9QealkXAMeKJV2xsffhFUdclgMmGN2RVnJxp7zpCC',
    'Patricia Elena',
    'Vega',
    'Castillo',
    '1996-01-29',
    'Peruana',
    '945678912',
    'INACTIVO',
    NULL,
    'USUARIO_INACTIVO',
    0,
    NULL,
    @AhoraUtc,
    @AhoraUtc
);

INSERT INTO dbo.AuditoriaInicioSesion
(
    UsuarioId,
    IdentificadorIngresado,
    TipoIdentificador,
    Resultado,
    Ip,
    UserAgent,
    FechaEvento
)
VALUES
(@UsuarioPreBloqueoId, 'usuario.prebloqueo', 'USERNAME', 'PASSWORD_INVALIDA', '127.0.0.1', 'seed-mock', DATEADD(MINUTE, -30, @AhoraUtc)),
(@UsuarioPreBloqueoId, 'usuario.prebloqueo', 'USERNAME', 'PASSWORD_INVALIDA', '127.0.0.1', 'seed-mock', DATEADD(MINUTE, -20, @AhoraUtc)),
(@UsuarioPreBloqueoId, 'usuario.prebloqueo', 'USERNAME', 'PASSWORD_INVALIDA', '127.0.0.1', 'seed-mock', DATEADD(MINUTE, -10, @AhoraUtc)),
(@UsuarioBloqueadoTemporalId, 'usuario.temporal', 'USERNAME', 'BLOQUEADO_TEMPORAL', '127.0.0.1', 'seed-mock', DATEADD(MINUTE, -5, @AhoraUtc)),
(@UsuarioBloqueadoSistemaId, 'usuario.sistema', 'USERNAME', 'BLOQUEADO_SISTEMA', '127.0.0.1', 'seed-mock', DATEADD(MINUTE, -15, @AhoraUtc)),
(@UsuarioInactivoId, 'usuario.inactivo', 'USERNAME', 'USUARIO_INACTIVO', '127.0.0.1', 'seed-mock', DATEADD(MINUTE, -25, @AhoraUtc));
