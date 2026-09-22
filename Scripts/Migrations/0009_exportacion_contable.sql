-- =============================================================================
-- 0009_exportacion_contable.sql  ·  Exportacion contable (M5)
-- -----------------------------------------------------------------------------
-- Idempotente. NO edita scripts anteriores.
-- Fuente de diseno: docs/04-modelo-datos.md §7. RD-11: lo ya exportado se marca
-- y no se reexporta salvo peticion explicita. Se anade ExportacionFactura para
-- las facturas emitidas (vf), que el modelo no contemplaba.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.ExportacionContable', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExportacionContable
    (
        Id                 uniqueidentifier NOT NULL CONSTRAINT PK_ExportacionContable PRIMARY KEY,
        GestoriaId         uniqueidentifier NOT NULL,
        ClienteId          uniqueidentifier NOT NULL,
        Formato            nvarchar(30)     NOT NULL,   -- CsvGenerico | A3
        Ejercicio          smallint         NOT NULL,
        Periodo            nvarchar(2)      NOT NULL,
        FechaGeneracionUtc datetime2(3)     NOT NULL,
        UsuarioId          uniqueidentifier NOT NULL,
        ClaveAlmacen       uniqueidentifier NOT NULL,   -- el fichero generado se conserva: es la prueba de lo que se entrego
        NombreFichero      nvarchar(200)    NOT NULL,
        NumRegistros       int              NOT NULL,
        HashSha256         char(64)         NOT NULL,
        IncluyoExportados  bit              NOT NULL CONSTRAINT DF_ExportacionContable_Incluyo DEFAULT 0,
        CONSTRAINT FK_ExportacionContable_Gestoria FOREIGN KEY (GestoriaId) REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_ExportacionContable_Cliente  FOREIGN KEY (ClienteId)  REFERENCES dbo.Cliente (Id),
        CONSTRAINT FK_ExportacionContable_Usuario  FOREIGN KEY (UsuarioId)  REFERENCES dbo.Usuario (Id)
    );
    CREATE INDEX IX_ExportacionContable_Cliente ON dbo.ExportacionContable (GestoriaId, ClienteId, FechaGeneracionUtc DESC);
END
GO

-- RD-11: documentos (facturas recibidas, tickets) ya exportados
IF OBJECT_ID(N'dbo.ExportacionDocumento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExportacionDocumento
    (
        ExportacionId  uniqueidentifier NOT NULL,
        DocumentoId    uniqueidentifier NOT NULL,
        GestoriaId     uniqueidentifier NOT NULL,
        CONSTRAINT PK_ExportacionDocumento PRIMARY KEY (ExportacionId, DocumentoId),
        CONSTRAINT FK_ExportacionDocumento_Exportacion FOREIGN KEY (ExportacionId) REFERENCES dbo.ExportacionContable (Id),
        CONSTRAINT FK_ExportacionDocumento_Documento   FOREIGN KEY (DocumentoId)   REFERENCES dbo.Documento (Id)
    );
    CREATE INDEX IX_ExportacionDocumento_Documento ON dbo.ExportacionDocumento (GestoriaId, DocumentoId);
END
GO

-- RD-11 para facturas emitidas Veri*Factu
IF OBJECT_ID(N'dbo.ExportacionFactura', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExportacionFactura
    (
        ExportacionId     uniqueidentifier NOT NULL,
        FacturaEmitidaId  uniqueidentifier NOT NULL,
        GestoriaId        uniqueidentifier NOT NULL,
        CONSTRAINT PK_ExportacionFactura PRIMARY KEY (ExportacionId, FacturaEmitidaId),
        CONSTRAINT FK_ExportacionFactura_Exportacion FOREIGN KEY (ExportacionId)    REFERENCES dbo.ExportacionContable (Id),
        CONSTRAINT FK_ExportacionFactura_Factura     FOREIGN KEY (FacturaEmitidaId) REFERENCES vf.FacturaEmitida (Id)
    );
    CREATE INDEX IX_ExportacionFactura_Factura ON dbo.ExportacionFactura (GestoriaId, FacturaEmitidaId);
END
GO

DECLARE @tablas TABLE (Nombre sysname);
INSERT INTO @tablas VALUES (N'dbo.ExportacionContable'), (N'dbo.ExportacionDocumento'), (N'dbo.ExportacionFactura');
DECLARE @t sysname, @sql nvarchar(max);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @tablas;
OPEN c; FETCH NEXT FROM c INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.security_predicates p JOIN sys.security_policies s ON s.object_id = p.object_id
                   WHERE s.name = N'PoliticaTenant' AND p.target_object_id = OBJECT_ID(@t))
    BEGIN
        SET @sql = N'ALTER SECURITY POLICY dbo.PoliticaTenant ADD FILTER PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON ' + @t +
                   N', ADD BLOCK PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON ' + @t + N';';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM c INTO @t;
END
CLOSE c; DEALLOCATE c;
GO
