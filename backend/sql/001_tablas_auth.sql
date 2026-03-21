IF OBJECT_ID('dbo.AuditoriaInicioSesion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditoriaInicioSesion
    (
        Id BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_AuditoriaInicioSesion PRIMARY KEY,
        UsuarioId UNIQUEIDENTIFIER NULL,
        IdentificadorIngresado VARCHAR(150) NOT NULL,
        TipoIdentificador VARCHAR(20) NOT NULL,
        Resultado VARCHAR(30) NOT NULL,
        Ip VARCHAR(64) NULL,
        UserAgent VARCHAR(300) NULL,
        FechaEvento DATETIME2(0) NOT NULL
            CONSTRAINT DF_AuditoriaInicioSesion_FechaEvento DEFAULT SYSDATETIME(),
        CONSTRAINT CK_AuditoriaInicioSesion_TipoIdentificador
            CHECK (TipoIdentificador IN ('CORREO', 'USERNAME', 'DNI', 'DESCONOCIDO')),
        CONSTRAINT CK_AuditoriaInicioSesion_Resultado
            CHECK (Resultado IN (
                'OK',
                'PASSWORD_INVALIDA',
                'USUARIO_NO_EXISTE',
                'BLOQUEADO_TEMPORAL',
                'BLOQUEADO_SISTEMA',
                'TOKEN_EXPIRADO',
                'USUARIO_INACTIVO'
            ))
    );
END;
GO

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios
    (
        Id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_Usuarios PRIMARY KEY
            CONSTRAINT DF_Usuarios_Id DEFAULT NEWID(),
        Dni VARCHAR(20) NOT NULL,
        Username VARCHAR(50) NOT NULL,
        Correo VARCHAR(150) NOT NULL,
        ContrasenaHash VARCHAR(255) NOT NULL,
        Nombres VARCHAR(100) NOT NULL,
        PrimerApellido VARCHAR(100) NOT NULL,
        SegundoApellido VARCHAR(100) NULL,
        FechaNacimiento DATE NOT NULL,
        Nacionalidad VARCHAR(80) NOT NULL,
        NumeroCelular VARCHAR(20) NOT NULL,
        Estado VARCHAR(20) NOT NULL
            CONSTRAINT DF_Usuarios_Estado DEFAULT 'ACTIVO',
        BloqueadoHasta DATETIME2(0) NULL,
        MotivoBloqueo VARCHAR(200) NULL,
        IntentosFallidos INT NOT NULL
            CONSTRAINT DF_Usuarios_IntentosFallidos DEFAULT 0,
        UltimoLoginAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_Usuarios_CreatedAt DEFAULT SYSDATETIME(),
        UpdatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_Usuarios_UpdatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_Usuarios_Dni UNIQUE (Dni),
        CONSTRAINT UQ_Usuarios_Username UNIQUE (Username),
        CONSTRAINT UQ_Usuarios_Correo UNIQUE (Correo),
        CONSTRAINT CK_Usuarios_Estado
            CHECK (Estado IN ('ACTIVO', 'BLOQUEADO_SISTEMA', 'INACTIVO')),
        CONSTRAINT CK_Usuarios_IntentosFallidos
            CHECK (IntentosFallidos >= 0)
    );
END;
GO

IF OBJECT_ID('dbo.SesionesUsuario', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SesionesUsuario
    (
        Id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_SesionesUsuario PRIMARY KEY
            CONSTRAINT DF_SesionesUsuario_Id DEFAULT NEWID(),
        UsuarioId UNIQUEIDENTIFIER NOT NULL,
        TokenJti VARCHAR(100) NOT NULL,
        RefreshTokenHash VARCHAR(255) NOT NULL,
        Ip VARCHAR(64) NULL,
        UserAgent VARCHAR(300) NULL,
        IniciaEn DATETIME2(0) NOT NULL
            CONSTRAINT DF_SesionesUsuario_IniciaEn DEFAULT SYSDATETIME(),
        ExpiraEn DATETIME2(0) NOT NULL,
        UltimoMovimientoEn DATETIME2(0) NOT NULL
            CONSTRAINT DF_SesionesUsuario_UltimoMovimientoEn DEFAULT SYSDATETIME(),
        CerradaEn DATETIME2(0) NULL,
        MotivoCierre VARCHAR(100) NULL,
        CONSTRAINT UQ_SesionesUsuario_TokenJti UNIQUE (TokenJti),
        CONSTRAINT FK_SesionesUsuario_Usuarios
            FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
        CONSTRAINT CK_SesionesUsuario_MotivoCierre
            CHECK (MotivoCierre IS NULL OR MotivoCierre IN ('LOGOUT', 'EXPIRACION', 'REVOCADA')),
        CONSTRAINT CK_SesionesUsuario_ExpiraEn
            CHECK (ExpiraEn >= IniciaEn),
        CONSTRAINT CK_SesionesUsuario_CerradaEn
            CHECK (CerradaEn IS NULL OR CerradaEn >= IniciaEn)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_AuditoriaInicioSesion_Usuarios'
)
BEGIN
    ALTER TABLE dbo.AuditoriaInicioSesion
    ADD CONSTRAINT FK_AuditoriaInicioSesion_Usuarios
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Usuarios_Correo'
      AND object_id = OBJECT_ID('dbo.Usuarios')
)
BEGIN
    CREATE INDEX IX_Usuarios_Correo ON dbo.Usuarios(Correo);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Usuarios_Username'
      AND object_id = OBJECT_ID('dbo.Usuarios')
)
BEGIN
    CREATE INDEX IX_Usuarios_Username ON dbo.Usuarios(Username);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Usuarios_Dni'
      AND object_id = OBJECT_ID('dbo.Usuarios')
)
BEGIN
    CREATE INDEX IX_Usuarios_Dni ON dbo.Usuarios(Dni);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SesionesUsuario_UsuarioId_CerradaEn_ExpiraEn'
      AND object_id = OBJECT_ID('dbo.SesionesUsuario')
)
BEGIN
    CREATE INDEX IX_SesionesUsuario_UsuarioId_CerradaEn_ExpiraEn
        ON dbo.SesionesUsuario(UsuarioId, CerradaEn, ExpiraEn);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AuditoriaInicioSesion_UsuarioId_FechaEvento'
      AND object_id = OBJECT_ID('dbo.AuditoriaInicioSesion')
)
BEGIN
    CREATE INDEX IX_AuditoriaInicioSesion_UsuarioId_FechaEvento
        ON dbo.AuditoriaInicioSesion(UsuarioId, FechaEvento DESC);
END;
GO
