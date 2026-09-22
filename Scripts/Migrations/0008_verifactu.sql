-- =============================================================================
-- 0008_verifactu.sql  ·  Facturacion Veri*Factu (M6): esquema vf
-- -----------------------------------------------------------------------------
-- Idempotente. NO edita scripts anteriores.
-- Fuente de diseno: docs/04-modelo-datos.md §6 y docs/06-verifactu/*.
-- Inalterabilidad (RD-06): FacturaEmitida, LineaFactura y RegistroFacturacion
-- llevan trigger INSTEAD OF UPDATE, DELETE (verificado: detiene incluso a
-- db_owner). El DENY al login de aplicacion se escribe condicionado a que exista
-- (12-infraestructura-despliegue.md §3.1). El estado mutable del envio y la
-- referencia al PDF viven en tablas aparte.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'vf')
    EXEC(N'CREATE SCHEMA vf AUTHORIZATION dbo;');
GO

-- Catalogos del emisor (mutables) ------------------------------------------------
IF OBJECT_ID(N'vf.SerieFacturacion', N'U') IS NULL
BEGIN
    CREATE TABLE vf.SerieFacturacion
    (
        Id               uniqueidentifier NOT NULL CONSTRAINT PK_SerieFacturacion PRIMARY KEY,
        GestoriaId       uniqueidentifier NOT NULL,
        ClienteEmisorId  uniqueidentifier NOT NULL,
        Codigo           nvarchar(20)     NOT NULL,   -- 'A', 'T' (tickets), 'R' (rectificativas)
        Descripcion      nvarchar(100)    NOT NULL,
        Ejercicio        smallint         NOT NULL,
        UltimoNumero     int              NOT NULL CONSTRAINT DF_SerieFacturacion_Ultimo DEFAULT 0,
        Activa           bit              NOT NULL CONSTRAINT DF_SerieFacturacion_Activa DEFAULT 1,
        CONSTRAINT FK_SerieFacturacion_Gestoria FOREIGN KEY (GestoriaId)      REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_SerieFacturacion_Cliente  FOREIGN KEY (ClienteEmisorId) REFERENCES dbo.Cliente (Id),
        CONSTRAINT UX_SerieFacturacion UNIQUE (ClienteEmisorId, Codigo, Ejercicio)
    );
END
GO

IF OBJECT_ID(N'vf.Destinatario', N'U') IS NULL
BEGIN
    CREATE TABLE vf.Destinatario
    (
        Id               uniqueidentifier NOT NULL CONSTRAINT PK_Destinatario PRIMARY KEY,
        GestoriaId       uniqueidentifier NOT NULL,
        ClienteEmisorId  uniqueidentifier NOT NULL,
        Nif              nvarchar(20)     NULL,       -- NULL en facturas simplificadas / extranjeros sin NIF (IDOtro, no soportado en la demo)
        Nombre           nvarchar(120)    NOT NULL,
        Direccion        nvarchar(300)    NULL,
        Pais             char(2)          NOT NULL CONSTRAINT DF_Destinatario_Pais DEFAULT 'ES',
        Activo           bit              NOT NULL CONSTRAINT DF_Destinatario_Activo DEFAULT 1,
        CONSTRAINT FK_Destinatario_Gestoria FOREIGN KEY (GestoriaId)      REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_Destinatario_Cliente  FOREIGN KEY (ClienteEmisorId) REFERENCES dbo.Cliente (Id)
    );
    CREATE INDEX IX_Destinatario_Emisor ON vf.Destinatario (GestoriaId, ClienteEmisorId, Nombre);
END
GO

IF OBJECT_ID(N'vf.ArticuloServicio', N'U') IS NULL
BEGIN
    CREATE TABLE vf.ArticuloServicio
    (
        Id               uniqueidentifier NOT NULL CONSTRAINT PK_ArticuloServicio PRIMARY KEY,
        GestoriaId       uniqueidentifier NOT NULL,
        ClienteEmisorId  uniqueidentifier NOT NULL,
        Descripcion      nvarchar(200)    NOT NULL,
        PrecioUnitario   decimal(18,2)    NOT NULL,
        TipoIva          decimal(5,2)     NOT NULL,
        TipoRecargo      decimal(5,2)     NULL,
        Exento           bit              NOT NULL CONSTRAINT DF_ArticuloServicio_Exento DEFAULT 0,
        Activo           bit              NOT NULL CONSTRAINT DF_ArticuloServicio_Activo DEFAULT 1,
        CONSTRAINT FK_ArticuloServicio_Gestoria FOREIGN KEY (GestoriaId)      REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_ArticuloServicio_Cliente  FOREIGN KEY (ClienteEmisorId) REFERENCES dbo.Cliente (Id)
    );
END
GO

-- Cadena por emisor: control de concurrencia (RD-05) y planificador de envio ------------
-- Se crea al dar de alta la primera serie del emisor, NUNCA dentro de la transaccion de emision.
IF OBJECT_ID(N'vf.CadenaEmisor', N'U') IS NULL
BEGIN
    CREATE TABLE vf.CadenaEmisor
    (
        NifEmisor                 char(9)          NOT NULL CONSTRAINT PK_CadenaEmisor PRIMARY KEY,
        GestoriaId                uniqueidentifier NOT NULL,
        ClienteEmisorId           uniqueidentifier NOT NULL,
        UltimaHuella              char(64)         NULL,
        UltimoNumero              bigint           NOT NULL CONSTRAINT DF_CadenaEmisor_UltimoNumero DEFAULT 0,
        FechaUltimoRegistroUtc    datetime2(3)     NULL,
        ProximoEnvioPermitidoUtc  datetime2(3)     NULL,   -- control de flujo por emisor (TiempoEsperaEnvio de la AEAT)
        CONSTRAINT FK_CadenaEmisor_Gestoria FOREIGN KEY (GestoriaId)      REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_CadenaEmisor_Cliente  FOREIGN KEY (ClienteEmisorId) REFERENCES dbo.Cliente (Id)
    );
END
GO

-- Tablas INMUTABLES (RD-06) -----------------------------------------------------------------
IF OBJECT_ID(N'vf.FacturaEmitida', N'U') IS NULL
BEGIN
    CREATE TABLE vf.FacturaEmitida
    (
        Id                    uniqueidentifier NOT NULL CONSTRAINT PK_FacturaEmitida PRIMARY KEY,
        GestoriaId            uniqueidentifier NOT NULL,
        ClienteEmisorId       uniqueidentifier NOT NULL,
        NifEmisor             char(9)          NOT NULL,
        SerieId               uniqueidentifier NOT NULL,
        NumSerieFactura       nvarchar(60)     NOT NULL,   -- limite del parametro numserie del QR
        FechaExpedicion       date             NOT NULL,
        TipoFactura           char(2)          NOT NULL,   -- F1 F2 F3 R1 R2 R3 R4 R5 (ClaveTipoFacturaType)
        TipoRectificativa     char(1)          NULL,       -- S (sustitucion) | I (diferencias)
        FacturaRectificadaId  uniqueidentifier NULL,
        MotivoRectificacion   nvarchar(300)    NULL,
        DestinatarioNif       nvarchar(20)     NULL,
        DestinatarioNombre    nvarchar(120)    NULL,
        BaseTotal             decimal(18,2)    NOT NULL,
        CuotaTotal            decimal(18,2)    NOT NULL,
        CuotaRecargoTotal     decimal(18,2)    NOT NULL,
        PorcentajeRetencion   decimal(5,2)     NOT NULL CONSTRAINT DF_FacturaEmitida_Retencion DEFAULT 0,
        RetencionTotal        decimal(18,2)    NOT NULL,
        ImporteTotal          decimal(18,2)    NOT NULL,
        Descripcion           nvarchar(500)    NOT NULL,
        FechaHoraCreacionUtc  datetime2(3)     NOT NULL,
        UsuarioId             uniqueidentifier NOT NULL,
        UrlQr                 nvarchar(400)    NOT NULL,   -- la URL exacta que se imprimio en el QR (entorno incluido)
        CONSTRAINT FK_FacturaEmitida_Gestoria    FOREIGN KEY (GestoriaId)           REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_FacturaEmitida_Cliente     FOREIGN KEY (ClienteEmisorId)      REFERENCES dbo.Cliente (Id),
        CONSTRAINT FK_FacturaEmitida_Serie       FOREIGN KEY (SerieId)              REFERENCES vf.SerieFacturacion (Id),
        CONSTRAINT FK_FacturaEmitida_Rectificada FOREIGN KEY (FacturaRectificadaId) REFERENCES vf.FacturaEmitida (Id),
        CONSTRAINT FK_FacturaEmitida_Usuario     FOREIGN KEY (UsuarioId)            REFERENCES dbo.Usuario (Id),
        CONSTRAINT UX_FacturaEmitida_Numero UNIQUE (NifEmisor, NumSerieFactura),
        CONSTRAINT CK_FacturaEmitida_Tipo CHECK (TipoFactura IN ('F1','F2','F3','R1','R2','R3','R4','R5')),
        CONSTRAINT CK_FacturaEmitida_TipoRect CHECK (TipoRectificativa IS NULL OR TipoRectificativa IN ('S','I')),
        CONSTRAINT CK_FacturaEmitida_Importe CHECK (ImporteTotal BETWEEN -999999999999.99 AND 999999999999.99)
    );
    CREATE INDEX IX_FacturaEmitida_Emisor ON vf.FacturaEmitida (GestoriaId, ClienteEmisorId, FechaExpedicion DESC);
END
GO

IF OBJECT_ID(N'vf.LineaFactura', N'U') IS NULL
BEGIN
    CREATE TABLE vf.LineaFactura
    (
        Id                bigint           NOT NULL IDENTITY(1,1) CONSTRAINT PK_LineaFactura PRIMARY KEY,
        GestoriaId        uniqueidentifier NOT NULL,
        FacturaEmitidaId  uniqueidentifier NOT NULL,
        Orden             int              NOT NULL,
        Descripcion       nvarchar(300)    NOT NULL,
        Cantidad          decimal(18,3)    NOT NULL,
        PrecioUnitario    decimal(18,2)    NOT NULL,
        TipoIva           decimal(5,2)     NOT NULL,
        TipoRecargo       decimal(5,2)     NULL,
        Exenta            bit              NOT NULL,
        BaseLinea         decimal(18,2)    NOT NULL,
        CONSTRAINT FK_LineaFactura_Factura FOREIGN KEY (FacturaEmitidaId) REFERENCES vf.FacturaEmitida (Id)
    );
    CREATE INDEX IX_LineaFactura_Factura ON vf.LineaFactura (GestoriaId, FacturaEmitidaId, Orden);
END
GO

IF OBJECT_ID(N'vf.RegistroFacturacion', N'U') IS NULL
BEGIN
    CREATE TABLE vf.RegistroFacturacion
    (
        Id                              bigint            NOT NULL IDENTITY(1,1) CONSTRAINT PK_RegistroFacturacion PRIMARY KEY,
        GestoriaId                      uniqueidentifier  NOT NULL,
        NifEmisor                       char(9)           NOT NULL,
        Tipo                            nvarchar(10)      NOT NULL,   -- ALTA | ANULACION
        FacturaEmitidaId                uniqueidentifier  NOT NULL,
        NumeroEnCadena                  bigint            NOT NULL,
        PrimerRegistro                  bit               NOT NULL,
        HuellaAnterior                  char(64)          NULL,
        Huella                          char(64)          NOT NULL,
        FechaHoraHusoGenRegistro        datetimeoffset(0) NOT NULL,
        FechaHoraHusoGenRegistroTexto   varchar(25)       NOT NULL,   -- el literal ISO que entro en la huella
        CadenaHuella                    nvarchar(1000)    NOT NULL,   -- el texto exacto que se hasheo: la prueba
        XmlRegistro                     nvarchar(max)     NOT NULL,   -- el XML tal cual se remite
        IdSistemaInformatico            nvarchar(2)       NOT NULL,
        VersionSistemaInformatico       nvarchar(50)      NOT NULL,
        NumeroInstalacion               nvarchar(100)     NOT NULL,
        CONSTRAINT FK_RegistroFacturacion_Factura FOREIGN KEY (FacturaEmitidaId) REFERENCES vf.FacturaEmitida (Id),
        CONSTRAINT UX_RegistroFacturacion_Cadena UNIQUE (NifEmisor, NumeroEnCadena),   -- sin huecos ni duplicados (RD-05)
        CONSTRAINT CK_RegistroFacturacion_Tipo CHECK (Tipo IN (N'ALTA', N'ANULACION'))
    );
    CREATE INDEX IX_RegistroFacturacion_Factura ON vf.RegistroFacturacion (GestoriaId, FacturaEmitidaId);
END
GO

IF OBJECT_ID(N'vf.TR_FacturaEmitida_Inalterable', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER vf.TR_FacturaEmitida_Inalterable ON vf.FacturaEmitida INSTEAD OF UPDATE, DELETE AS
           BEGIN SET NOCOUNT ON; THROW 50001, N''Las facturas emitidas son inalterables (RD 1007/2023). Use rectificativa o anulacion.'', 1; END');
GO
IF OBJECT_ID(N'vf.TR_LineaFactura_Inalterable', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER vf.TR_LineaFactura_Inalterable ON vf.LineaFactura INSTEAD OF UPDATE, DELETE AS
           BEGIN SET NOCOUNT ON; THROW 50001, N''Las lineas de una factura emitida son inalterables (RD 1007/2023).'', 1; END');
GO
IF OBJECT_ID(N'vf.TR_RegistroFacturacion_Inalterable', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER vf.TR_RegistroFacturacion_Inalterable ON vf.RegistroFacturacion INSTEAD OF UPDATE, DELETE AS
           BEGIN SET NOCOUNT ON; THROW 50001, N''Los registros de facturacion son inalterables (RD 1007/2023).'', 1; END');
GO

-- Estado mutable del envio, cola (outbox) y lotes --------------------------------------------
IF OBJECT_ID(N'vf.EstadoEnvioRegistro', N'U') IS NULL
BEGIN
    CREATE TABLE vf.EstadoEnvioRegistro
    (
        RegistroFacturacionId  bigint           NOT NULL CONSTRAINT PK_EstadoEnvioRegistro PRIMARY KEY,
        GestoriaId             uniqueidentifier NOT NULL,
        Estado                 nvarchar(30)     NOT NULL,   -- GENERADO EN_COLA ENVIADO ACEPTADO ACEPTADO_CON_ERRORES RECHAZADO ERROR_TECNICO DEAD_LETTER BLOQUEADO_SIN_CERTIFICADO ERROR_VALIDACION
        Intentos               int              NOT NULL CONSTRAINT DF_EstadoEnvioRegistro_Intentos DEFAULT 0,
        FechaEnvioUtc          datetime2(3)     NULL,
        FechaRespuestaUtc      datetime2(3)     NULL,
        CsvAeat                nvarchar(100)    NULL,
        CodigoErrorAeat        nvarchar(20)     NULL,
        DescripcionErrorAeat   nvarchar(1000)   NULL,
        LoteEnvioId            bigint           NULL,
        CONSTRAINT FK_EstadoEnvioRegistro_Registro FOREIGN KEY (RegistroFacturacionId) REFERENCES vf.RegistroFacturacion (Id)
    );
    CREATE INDEX IX_EstadoEnvioRegistro_Estado ON vf.EstadoEnvioRegistro (GestoriaId, Estado);
END
GO

IF OBJECT_ID(N'vf.EnvioPendiente', N'U') IS NULL
BEGIN
    CREATE TABLE vf.EnvioPendiente
    (
        Id                     bigint           NOT NULL IDENTITY(1,1) CONSTRAINT PK_EnvioPendiente PRIMARY KEY,
        RegistroFacturacionId  bigint           NOT NULL,
        GestoriaId             uniqueidentifier NOT NULL,
        NifEmisor              char(9)          NOT NULL,
        FechaAltaUtc           datetime2(3)     NOT NULL,
        ProximoIntentoUtc      datetime2(3)     NOT NULL,
        Intentos               int              NOT NULL CONSTRAINT DF_EnvioPendiente_Intentos DEFAULT 0,
        Estado                 nvarchar(20)     NOT NULL,   -- PENDIENTE EN_CURSO COMPLETADO DEAD_LETTER
        TomadoUtc              datetime2(3)     NULL,       -- marca de "en curso" con caducidad (E4)
        UltimoError            nvarchar(1000)   NULL,
        LoteEnvioId            bigint           NULL,
        CONSTRAINT FK_EnvioPendiente_Registro FOREIGN KEY (RegistroFacturacionId) REFERENCES vf.RegistroFacturacion (Id),
        CONSTRAINT CK_EnvioPendiente_Estado CHECK (Estado IN (N'PENDIENTE', N'EN_CURSO', N'COMPLETADO', N'DEAD_LETTER'))
    );
    -- El indice que sirve la lectura del worker con READPAST, UPDLOCK, ROWLOCK
    CREATE INDEX IX_EnvioPendiente_Trabajo ON vf.EnvioPendiente (Estado, ProximoIntentoUtc, NifEmisor) INCLUDE (RegistroFacturacionId);
END
GO

IF OBJECT_ID(N'vf.LoteEnvio', N'U') IS NULL
BEGIN
    CREATE TABLE vf.LoteEnvio
    (
        Id                    bigint           NOT NULL IDENTITY(1,1) CONSTRAINT PK_LoteEnvio PRIMARY KEY,
        GestoriaId            uniqueidentifier NOT NULL,
        NifEmisor             char(9)          NOT NULL,
        FechaEnvioUtc         datetime2(3)     NOT NULL,
        NumRegistros          int              NOT NULL,
        Cliente               nvarchar(40)     NOT NULL,   -- SimuladorAeat | ClienteAeatSoap
        RespuestaXml          nvarchar(max)    NULL,
        TiempoEsperaSegundos  int              NULL,
        Resultado             nvarchar(30)     NOT NULL,   -- OK | ERROR_TECNICO
        Error                 nvarchar(1000)   NULL
    );
END
GO

-- PDF de la factura: se genera UNA vez y se guarda; tabla aparte porque FacturaEmitida es inmutable ---------
IF OBJECT_ID(N'vf.FacturaPdf', N'U') IS NULL
BEGIN
    CREATE TABLE vf.FacturaPdf
    (
        FacturaEmitidaId   uniqueidentifier NOT NULL CONSTRAINT PK_FacturaPdf PRIMARY KEY,
        GestoriaId         uniqueidentifier NOT NULL,
        ClaveAlmacen       uniqueidentifier NULL,
        VersionPlantilla   nvarchar(20)     NULL,
        Motor              nvarchar(30)     NULL,       -- Playwright | Basico
        FechaGeneracionUtc datetime2(3)     NULL,
        Intentos           int              NOT NULL CONSTRAINT DF_FacturaPdf_Intentos DEFAULT 0,
        Error              nvarchar(1000)   NULL,
        CONSTRAINT FK_FacturaPdf_Factura FOREIGN KEY (FacturaEmitidaId) REFERENCES vf.FacturaEmitida (Id)
    );
END
GO

-- Certificados y apoderamientos (ADR-002) ------------------------------------------------------------
IF OBJECT_ID(N'vf.Certificado', N'U') IS NULL
BEGIN
    CREATE TABLE vf.Certificado
    (
        Id               uniqueidentifier NOT NULL CONSTRAINT PK_Certificado PRIMARY KEY,
        GestoriaId       uniqueidentifier NOT NULL,
        NifTitular       char(9)          NOT NULL,
        Tipo             nvarchar(20)     NOT NULL,   -- Gestoria | Obligado
        Alias            nvarchar(100)    NOT NULL,
        HuellaDigital    nvarchar(64)     NOT NULL,
        ValidoDesde      date             NOT NULL,
        ValidoHasta      date             NOT NULL,
        MaterialCifrado  varbinary(max)   NULL,       -- PFX protegido con Data Protection; NULL en la demo (el simulador no valida certificados)
        Estado           nvarchar(20)     NOT NULL CONSTRAINT DF_Certificado_Estado DEFAULT N'Vigente',
        CONSTRAINT FK_Certificado_Gestoria FOREIGN KEY (GestoriaId) REFERENCES dbo.Gestoria (Id),
        CONSTRAINT CK_Certificado_Tipo CHECK (Tipo IN (N'Gestoria', N'Obligado')),
        CONSTRAINT CK_Certificado_Estado CHECK (Estado IN (N'Vigente', N'Caducado', N'Revocado'))
    );
END
GO

IF OBJECT_ID(N'vf.Apoderamiento', N'U') IS NULL
BEGIN
    CREATE TABLE vf.Apoderamiento
    (
        Id                   uniqueidentifier NOT NULL CONSTRAINT PK_Apoderamiento PRIMARY KEY,
        GestoriaId           uniqueidentifier NOT NULL,
        ClienteId            uniqueidentifier NOT NULL,
        Alcance              nvarchar(100)    NOT NULL,
        FechaAlta            date             NOT NULL,
        FechaFin             date             NULL,
        DocumentoRespaldoId  uniqueidentifier NULL,
        CONSTRAINT FK_Apoderamiento_Gestoria  FOREIGN KEY (GestoriaId)          REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_Apoderamiento_Cliente   FOREIGN KEY (ClienteId)           REFERENCES dbo.Cliente (Id),
        CONSTRAINT FK_Apoderamiento_Documento FOREIGN KEY (DocumentoRespaldoId) REFERENCES dbo.Documento (Id)
    );
END
GO

IF OBJECT_ID(N'vf.AccesoCertificadoLog', N'U') IS NULL
BEGIN
    CREATE TABLE vf.AccesoCertificadoLog
    (
        Id             bigint           NOT NULL IDENTITY(1,1) CONSTRAINT PK_AccesoCertificadoLog PRIMARY KEY,
        GestoriaId     uniqueidentifier NOT NULL,
        CertificadoId  uniqueidentifier NOT NULL,
        UsuarioId      uniqueidentifier NULL,
        FechaUtc       datetime2(3)     NOT NULL CONSTRAINT DF_AccesoCertificadoLog_Fecha DEFAULT SYSUTCDATETIME(),
        Motivo         nvarchar(200)    NOT NULL,
        NifObligado    char(9)          NOT NULL,
        CONSTRAINT FK_AccesoCertificadoLog_Certificado FOREIGN KEY (CertificadoId) REFERENCES vf.Certificado (Id)
    );
END
GO
IF OBJECT_ID(N'vf.TR_AccesoCertificadoLog_SoloAnexado', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER vf.TR_AccesoCertificadoLog_SoloAnexado ON vf.AccesoCertificadoLog INSTEAD OF UPDATE, DELETE AS
           BEGIN SET NOCOUNT ON; THROW 50002, N''El registro de accesos a certificados es de solo anexado.'', 1; END');
GO

-- Declaracion responsable: historico por version (sin tenant: es del productor) ------------------------
IF OBJECT_ID(N'vf.DeclaracionResponsableHistorico', N'U') IS NULL
BEGIN
    CREATE TABLE vf.DeclaracionResponsableHistorico
    (
        Id                int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_DeclaracionResponsableHistorico PRIMARY KEY,
        Version           nvarchar(50)  NOT NULL,
        FechaSuscripcion  date          NOT NULL,
        Contenido         nvarchar(max) NOT NULL,   -- JSON con todos los campos declarados
        HashSha256        char(64)      NOT NULL,
        FechaRegistroUtc  datetime2(3)  NOT NULL CONSTRAINT DF_DeclaracionResponsableHistorico_Fecha DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UX_DeclaracionResponsableHistorico_Version UNIQUE (Version)
    );
END
GO

-- RLS para todas las tablas con tenant del esquema vf --------------------------------------------------------
DECLARE @tablas TABLE (Nombre sysname);
INSERT INTO @tablas VALUES (N'vf.SerieFacturacion'), (N'vf.Destinatario'), (N'vf.ArticuloServicio'), (N'vf.CadenaEmisor'), (N'vf.FacturaEmitida'), (N'vf.LineaFactura'),
                           (N'vf.RegistroFacturacion'), (N'vf.EstadoEnvioRegistro'), (N'vf.EnvioPendiente'), (N'vf.LoteEnvio'), (N'vf.FacturaPdf'),
                           (N'vf.Certificado'), (N'vf.Apoderamiento'), (N'vf.AccesoCertificadoLog');
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

-- Segunda barrera de inalterabilidad: DENY al login de aplicacion, si existe (hoy no: agente_ro es db_owner) ------
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'aserta_app')
BEGIN
    EXEC(N'DENY UPDATE, DELETE ON SCHEMA::vf TO aserta_app;');
    EXEC(N'GRANT UPDATE ON vf.EstadoEnvioRegistro TO aserta_app;');
    EXEC(N'GRANT UPDATE ON vf.EnvioPendiente TO aserta_app;');
    EXEC(N'GRANT UPDATE ON vf.CadenaEmisor TO aserta_app;');
    EXEC(N'GRANT UPDATE ON vf.LoteEnvio TO aserta_app;');
    EXEC(N'GRANT UPDATE ON vf.FacturaPdf TO aserta_app;');
    EXEC(N'GRANT UPDATE, DELETE ON vf.SerieFacturacion TO aserta_app;');
    EXEC(N'GRANT UPDATE, DELETE ON vf.Destinatario TO aserta_app;');
    EXEC(N'GRANT UPDATE, DELETE ON vf.ArticuloServicio TO aserta_app;');
    EXEC(N'GRANT UPDATE ON vf.Certificado TO aserta_app;');
    EXEC(N'GRANT UPDATE ON vf.Apoderamiento TO aserta_app;');
END
GO
