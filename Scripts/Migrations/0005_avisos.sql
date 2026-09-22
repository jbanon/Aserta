-- =============================================================================
-- 0005_avisos.sql  ·  Bandeja de avisos en pantalla (M4: avisos automaticos)
-- -----------------------------------------------------------------------------
-- Idempotente. NO edita scripts anteriores.
-- El puerto INotificador se implementa en la demo como bandeja en pantalla
-- (00-vision-y-alcance.md §5.2): cada aviso es una fila de esta tabla.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Aviso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Aviso
    (
        Id                 bigint           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Aviso PRIMARY KEY,
        GestoriaId         uniqueidentifier NOT NULL,
        UsuarioDestinoId   uniqueidentifier NOT NULL,
        Tipo               nvarchar(40)     NOT NULL,   -- VencimientoProximo, Vencida, DocumentacionRecibida, ...
        Titulo             nvarchar(200)    NOT NULL,
        Cuerpo             nvarchar(1000)   NULL,
        EntidadTipo        nvarchar(100)    NULL,
        EntidadId          nvarchar(64)     NULL,
        Clave              nvarchar(200)    NOT NULL,   -- idempotencia: el mismo aviso no se genera dos veces
        FechaUtc           datetime2(3)     NOT NULL CONSTRAINT DF_Aviso_Fecha DEFAULT SYSUTCDATETIME(),
        LeidoUtc           datetime2(3)     NULL,
        CONSTRAINT FK_Aviso_Gestoria FOREIGN KEY (GestoriaId)       REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_Aviso_Usuario  FOREIGN KEY (UsuarioDestinoId) REFERENCES dbo.Usuario (Id),
        CONSTRAINT UX_Aviso_Clave UNIQUE (GestoriaId, UsuarioDestinoId, Clave)
    );
    CREATE INDEX IX_Aviso_Bandeja ON dbo.Aviso (GestoriaId, UsuarioDestinoId, LeidoUtc, FechaUtc DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.security_predicates p JOIN sys.security_policies s ON s.object_id = p.object_id
               WHERE s.name = N'PoliticaTenant' AND p.target_object_id = OBJECT_ID(N'dbo.Aviso'))
    EXEC(N'ALTER SECURITY POLICY dbo.PoliticaTenant
           ADD FILTER PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Aviso,
           ADD BLOCK  PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Aviso;');
GO
