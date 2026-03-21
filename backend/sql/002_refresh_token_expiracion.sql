IF COL_LENGTH('dbo.SesionesUsuario', 'RefreshTokenExpiraEn') IS NULL
BEGIN
    ALTER TABLE dbo.SesionesUsuario
    ADD RefreshTokenExpiraEn DATETIME2(0) NULL;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.SesionesUsuario')
      AND name = 'RefreshTokenExpiraEn'
)
BEGIN
    UPDATE dbo.SesionesUsuario
    SET RefreshTokenExpiraEn = DATEADD(DAY, 7, IniciaEn)
    WHERE RefreshTokenExpiraEn IS NULL;

    ALTER TABLE dbo.SesionesUsuario
    ALTER COLUMN RefreshTokenExpiraEn DATETIME2(0) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SesionesUsuario_TokenJti_RefreshTokenExpiraEn'
      AND object_id = OBJECT_ID('dbo.SesionesUsuario')
)
BEGIN
    CREATE INDEX IX_SesionesUsuario_TokenJti_RefreshTokenExpiraEn
        ON dbo.SesionesUsuario(TokenJti, RefreshTokenExpiraEn);
END;
GO
