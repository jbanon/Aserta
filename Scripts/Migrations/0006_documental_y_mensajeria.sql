-- =============================================================================
-- 0006_documental_y_mensajeria.sql  ·  Gestion documental (M3), portal (M2)
--                                        y mensajeria contextual
-- -----------------------------------------------------------------------------
-- Idempotente. NO edita scripts anteriores.
-- Fuente de diseno: docs/04-modelo-datos.md §5.3-5.5.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Documento ---------------------------------------------------------------------
-- El fichero NO se guarda aqui: vive en IAlmacenDocumental bajo ClaveAlmacen,
-- fuera del directorio de despliegue y cifrado en reposo.
IF OBJECT_ID(N'dbo.Documento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documento
    (
        Id                uniqueidentifier NOT NULL CONSTRAINT PK_Documento PRIMARY KEY CONSTRAINT DF_Documento_Id DEFAULT NEWSEQUENTIALID(),
        GestoriaId        uniqueidentifier NOT NULL,
        ClienteId         uniqueidentifier NOT NULL,
        Tipo              nvarchar(40)     NOT NULL,   -- FacturaRecibida | FacturaEmitida | Ticket | ExtractoBancario | Nomina | ReciboAlquiler | Contrato | Justificante | Otro
        Ejercicio         smallint         NOT NULL,
        Periodo           nvarchar(2)      NOT NULL,   -- mes 01..12 normalmente; tambien 1T..4T / AN
        NombreOriginal    nvarchar(260)    NOT NULL,
        ClaveAlmacen      uniqueidentifier NOT NULL,
        HashSha256        char(64)         NOT NULL,
        TamanoBytes       bigint           NOT NULL,
        TipoMime          nvarchar(100)    NOT NULL,
        Estado            nvarchar(20)     NOT NULL CONSTRAINT DF_Documento_Estado DEFAULT N'Recibido',   -- Recibido | EnRevision | Validado | Rechazado
        MotivoRechazo     nvarchar(500)    NULL,
        SubidoPorId       uniqueidentifier NOT NULL,
        FechaSubidaUtc    datetime2(3)     NOT NULL CONSTRAINT DF_Documento_FechaSubida DEFAULT SYSUTCDATETIME(),
        RevisadoPorId     uniqueidentifier NULL,
        FechaRevisionUtc  datetime2(3)     NULL,
        DatosExtraidos    nvarchar(max)    NULL,       -- JSON del adaptador OCR/IA (simulado). Sugerido, nunca autoritativo
        OrigenExtraccion  nvarchar(40)     NULL,       -- Simulado | Manual | ...
        Notas             nvarchar(1000)   NULL,
        CONSTRAINT FK_Documento_Gestoria  FOREIGN KEY (GestoriaId)    REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_Documento_Cliente   FOREIGN KEY (ClienteId)     REFERENCES dbo.Cliente (Id),
        CONSTRAINT FK_Documento_SubidoPor FOREIGN KEY (SubidoPorId)   REFERENCES dbo.Usuario (Id),
        CONSTRAINT FK_Documento_Revisor   FOREIGN KEY (RevisadoPorId) REFERENCES dbo.Usuario (Id),
        CONSTRAINT CK_Documento_Estado CHECK (Estado IN (N'Recibido', N'EnRevision', N'Validado', N'Rechazado')),
        CONSTRAINT CK_Documento_Periodo CHECK (Periodo IN (N'1T',N'2T',N'3T',N'4T',N'01',N'02',N'03',N'04',N'05',N'06',N'07',N'08',N'09',N'10',N'11',N'12',N'AN')),
        CONSTRAINT UX_Documento_Hash UNIQUE (ClienteId, HashSha256)   -- el mismo ticket subido dos veces desde el movil
    );
    CREATE INDEX IX_Documento_Bandeja ON dbo.Documento (GestoriaId, Estado, FechaSubidaUtc DESC) INCLUDE (ClienteId, Tipo, Ejercicio, Periodo);
    CREATE INDEX IX_Documento_Cliente ON dbo.Documento (GestoriaId, ClienteId, Ejercicio, Periodo, Tipo);
END
GO

-- Vinculo documento <-> obligacion --------------------------------------------------
IF OBJECT_ID(N'dbo.DocumentoObligacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentoObligacion
    (
        DocumentoId   uniqueidentifier NOT NULL,
        ObligacionId  uniqueidentifier NOT NULL,
        GestoriaId    uniqueidentifier NOT NULL,
        CONSTRAINT PK_DocumentoObligacion PRIMARY KEY (DocumentoId, ObligacionId),
        CONSTRAINT FK_DocumentoObligacion_Documento  FOREIGN KEY (DocumentoId)  REFERENCES dbo.Documento (Id),
        CONSTRAINT FK_DocumentoObligacion_Obligacion FOREIGN KEY (ObligacionId) REFERENCES dbo.Obligacion (Id)
    );
    CREATE INDEX IX_DocumentoObligacion_Obligacion ON dbo.DocumentoObligacion (GestoriaId, ObligacionId);
END
GO

-- FK diferida de 0003: justificante de la obligacion
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Obligacion_Justificante')
    ALTER TABLE dbo.Obligacion ADD CONSTRAINT FK_Obligacion_Justificante FOREIGN KEY (JustificanteDocumentoId) REFERENCES dbo.Documento (Id);
GO

-- RequisitoPeriodo: "te faltan 2 facturas de julio" -----------------------------------
IF OBJECT_ID(N'dbo.RequisitoPeriodo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequisitoPeriodo
    (
        Id                uniqueidentifier NOT NULL CONSTRAINT PK_RequisitoPeriodo PRIMARY KEY CONSTRAINT DF_RequisitoPeriodo_Id DEFAULT NEWSEQUENTIALID(),
        GestoriaId        uniqueidentifier NOT NULL,
        ClienteId         uniqueidentifier NOT NULL,
        Ejercicio         smallint         NOT NULL,
        Periodo           nvarchar(2)      NOT NULL,
        TipoDocumento     nvarchar(40)     NOT NULL,
        CantidadEsperada  int              NULL,       -- NULL = "al menos uno" / sin cantidad fija
        Obligatorio       bit              NOT NULL CONSTRAINT DF_RequisitoPeriodo_Obligatorio DEFAULT 1,
        Descripcion       nvarchar(300)    NOT NULL,
        ReglaOrigenId     int              NULL,       -- NULL = ajustado a mano por la gestoria
        NoAplica          bit              NOT NULL CONSTRAINT DF_RequisitoPeriodo_NoAplica DEFAULT 0,
        CONSTRAINT FK_RequisitoPeriodo_Gestoria FOREIGN KEY (GestoriaId) REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_RequisitoPeriodo_Cliente  FOREIGN KEY (ClienteId)  REFERENCES dbo.Cliente (Id),
        CONSTRAINT UX_RequisitoPeriodo UNIQUE (GestoriaId, ClienteId, Ejercicio, Periodo, TipoDocumento),   -- idempotencia del motor
        CONSTRAINT CK_RequisitoPeriodo_Periodo CHECK (Periodo IN (N'1T',N'2T',N'3T',N'4T',N'01',N'02',N'03',N'04',N'05',N'06',N'07',N'08',N'09',N'10',N'11',N'12',N'AN'))
    );
END
GO

-- Mensajeria contextual: un hilo cuelga de UNA obligacion o de UN documento, nunca suelto ---------
IF OBJECT_ID(N'dbo.Hilo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Hilo
    (
        Id                     uniqueidentifier NOT NULL CONSTRAINT PK_Hilo PRIMARY KEY CONSTRAINT DF_Hilo_Id DEFAULT NEWSEQUENTIALID(),
        GestoriaId             uniqueidentifier NOT NULL,
        ClienteId              uniqueidentifier NOT NULL,
        ObligacionId           uniqueidentifier NULL,
        DocumentoId            uniqueidentifier NULL,
        Asunto                 nvarchar(200)    NOT NULL,
        Estado                 nvarchar(20)     NOT NULL CONSTRAINT DF_Hilo_Estado DEFAULT N'Abierto',   -- Abierto | Cerrado
        FechaUltimoMensajeUtc  datetime2(3)     NOT NULL,
        CONSTRAINT FK_Hilo_Gestoria   FOREIGN KEY (GestoriaId)   REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_Hilo_Cliente    FOREIGN KEY (ClienteId)    REFERENCES dbo.Cliente (Id),
        CONSTRAINT FK_Hilo_Obligacion FOREIGN KEY (ObligacionId) REFERENCES dbo.Obligacion (Id),
        CONSTRAINT FK_Hilo_Documento  FOREIGN KEY (DocumentoId)  REFERENCES dbo.Documento (Id),
        CONSTRAINT CK_Hilo_Ancla CHECK ((ObligacionId IS NOT NULL AND DocumentoId IS NULL) OR (ObligacionId IS NULL AND DocumentoId IS NOT NULL)),
        CONSTRAINT CK_Hilo_Estado CHECK (Estado IN (N'Abierto', N'Cerrado'))
    );
    CREATE INDEX IX_Hilo_Cliente     ON dbo.Hilo (GestoriaId, ClienteId, FechaUltimoMensajeUtc DESC);
    CREATE INDEX IX_Hilo_Obligacion  ON dbo.Hilo (GestoriaId, ObligacionId) WHERE ObligacionId IS NOT NULL;
    CREATE INDEX IX_Hilo_Documento   ON dbo.Hilo (GestoriaId, DocumentoId) WHERE DocumentoId IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.Mensaje', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Mensaje
    (
        Id                     bigint           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Mensaje PRIMARY KEY,
        GestoriaId             uniqueidentifier NOT NULL,   -- para RLS
        HiloId                 uniqueidentifier NOT NULL,
        AutorId                uniqueidentifier NOT NULL,
        FechaUtc               datetime2(3)     NOT NULL CONSTRAINT DF_Mensaje_Fecha DEFAULT SYSUTCDATETIME(),
        Cuerpo                 nvarchar(4000)   NOT NULL,
        LeidoPorClienteUtc     datetime2(3)     NULL,
        LeidoPorGestoriaUtc    datetime2(3)     NULL,
        CONSTRAINT FK_Mensaje_Hilo  FOREIGN KEY (HiloId)  REFERENCES dbo.Hilo (Id),
        CONSTRAINT FK_Mensaje_Autor FOREIGN KEY (AutorId) REFERENCES dbo.Usuario (Id)
    );
    CREATE INDEX IX_Mensaje_Hilo ON dbo.Mensaje (GestoriaId, HiloId, FechaUtc);
END
GO

-- RLS ---------------------------------------------------------------------------------------
DECLARE @tablas TABLE (Nombre sysname);
INSERT INTO @tablas VALUES (N'dbo.Documento'), (N'dbo.DocumentoObligacion'), (N'dbo.RequisitoPeriodo'), (N'dbo.Hilo'), (N'dbo.Mensaje');
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
