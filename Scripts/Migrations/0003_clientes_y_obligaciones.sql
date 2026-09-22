-- =============================================================================
-- 0003_clientes_y_obligaciones.sql  ·  Ficha de cliente, perfil fiscal
--                                       versionado y obligaciones (M1 / base de M4)
-- -----------------------------------------------------------------------------
-- Idempotente. NO edita scripts anteriores.
-- Fuente de diseno: docs/04-modelo-datos.md §3 y §5.1-5.2.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Cliente -----------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Cliente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cliente
    (
        Id                     uniqueidentifier NOT NULL CONSTRAINT PK_Cliente PRIMARY KEY CONSTRAINT DF_Cliente_Id DEFAULT NEWSEQUENTIALID(),
        GestoriaId             uniqueidentifier NOT NULL,
        Nif                    nvarchar(9)      NOT NULL,
        RazonSocial            nvarchar(200)    NOT NULL,
        NombreComercial        nvarchar(200)    NULL,
        FormaJuridica          nvarchar(20)     NOT NULL,   -- Autonomo | SL | SA | CB | Particular
        Email                  nvarchar(200)    NULL,
        Telefono               nvarchar(30)     NULL,
        DireccionCalle         nvarchar(200)    NULL,
        DireccionCodigoPostal  nvarchar(10)     NULL,
        DireccionMunicipio     nvarchar(100)    NULL,
        DireccionProvincia     nvarchar(100)    NULL,
        AsesorResponsableId    uniqueidentifier NOT NULL,
        FechaAlta              date             NOT NULL,
        FechaBaja              date             NULL,
        Estado                 nvarchar(20)     NOT NULL CONSTRAINT DF_Cliente_Estado DEFAULT N'Activo',
        Notas                  nvarchar(max)    NULL,
        CONSTRAINT FK_Cliente_Gestoria FOREIGN KEY (GestoriaId)          REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_Cliente_Asesor   FOREIGN KEY (AsesorResponsableId) REFERENCES dbo.Usuario (Id),
        CONSTRAINT UX_Cliente_Gestoria_Nif UNIQUE (GestoriaId, Nif),   -- mismo NIF en dos gestorias si; dos veces en la misma no
        CONSTRAINT CK_Cliente_Nif           CHECK (LEN(Nif) = 9 AND Nif NOT LIKE N'%[^A-Z0-9]%'),   -- formato basico; el digito de control lo valida el dominio
        CONSTRAINT CK_Cliente_FormaJuridica CHECK (FormaJuridica IN (N'Autonomo', N'SL', N'SA', N'CB', N'Particular')),
        CONSTRAINT CK_Cliente_Estado        CHECK (Estado IN (N'Activo', N'Baja')),
        CONSTRAINT CK_Cliente_Fechas        CHECK (FechaBaja IS NULL OR FechaBaja >= FechaAlta)
    );
    CREATE INDEX IX_Cliente_Gestoria_Estado ON dbo.Cliente (GestoriaId, Estado, RazonSocial);
    CREATE INDEX IX_Cliente_Asesor          ON dbo.Cliente (GestoriaId, AsesorResponsableId);
END
GO

-- FK diferida de 0001: usuario del lado cliente
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Usuario_Cliente')
    ALTER TABLE dbo.Usuario ADD CONSTRAINT FK_Usuario_Cliente FOREIGN KEY (ClienteId) REFERENCES dbo.Cliente (Id);
GO

-- PerfilFiscal: versionado temporal --------------------------------------------
-- Un autonomo que contrata a su primer empleado en julio pasa a tener 111 a
-- partir del 3T, no desde enero: por eso hay versiones y no columnas en Cliente.
IF OBJECT_ID(N'dbo.PerfilFiscal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PerfilFiscal
    (
        Id                             uniqueidentifier NOT NULL CONSTRAINT PK_PerfilFiscal PRIMARY KEY CONSTRAINT DF_PerfilFiscal_Id DEFAULT NEWSEQUENTIALID(),
        GestoriaId                     uniqueidentifier NOT NULL,   -- redundante con Cliente, necesario para RLS y filtros EF
        ClienteId                      uniqueidentifier NOT NULL,
        VigenteDesde                   date             NOT NULL,
        VigenteHasta                   date             NULL,
        RegimenIrpf                    nvarchar(30)     NOT NULL,   -- DirectaNormal | DirectaSimplificada | Objetiva | NoAplica
        RegimenIva                     nvarchar(30)     NOT NULL,   -- General | RecargoEquivalencia | Simplificado | CriterioCaja | Exento | NoAplica
        PeriodicidadIva                nvarchar(10)     NOT NULL,   -- Trimestral | Mensual | NoAplica
        TieneEmpleados                 bit              NOT NULL,
        PagaProfesionalesConRetencion  bit              NOT NULL,
        AlquilaLocal                   bit              NOT NULL,
        RepartePagosCapitalMobiliario  bit              NOT NULL,
        OperacionesIntracomunitarias   bit              NOT NULL,
        SuperaUmbral347                bit              NOT NULL,
        Territorio                     nvarchar(10)     NOT NULL,   -- Comun | Foral (Foral se bloquea en el alta: DA-14)
        CierreEjercicioMes             tinyint          NOT NULL CONSTRAINT DF_PerfilFiscal_CierreMes DEFAULT 12,
        CierreEjercicioDia             tinyint          NOT NULL CONSTRAINT DF_PerfilFiscal_CierreDia DEFAULT 31,
        CONSTRAINT FK_PerfilFiscal_Gestoria FOREIGN KEY (GestoriaId) REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_PerfilFiscal_Cliente  FOREIGN KEY (ClienteId)  REFERENCES dbo.Cliente (Id),
        CONSTRAINT CK_PerfilFiscal_RegimenIrpf     CHECK (RegimenIrpf IN (N'DirectaNormal', N'DirectaSimplificada', N'Objetiva', N'NoAplica')),
        CONSTRAINT CK_PerfilFiscal_RegimenIva      CHECK (RegimenIva IN (N'General', N'RecargoEquivalencia', N'Simplificado', N'CriterioCaja', N'Exento', N'NoAplica')),
        CONSTRAINT CK_PerfilFiscal_PeriodicidadIva CHECK (PeriodicidadIva IN (N'Trimestral', N'Mensual', N'NoAplica')),
        CONSTRAINT CK_PerfilFiscal_Territorio      CHECK (Territorio IN (N'Comun', N'Foral')),
        CONSTRAINT CK_PerfilFiscal_Vigencia        CHECK (VigenteHasta IS NULL OR VigenteHasta >= VigenteDesde),
        CONSTRAINT CK_PerfilFiscal_Cierre          CHECK (CierreEjercicioMes BETWEEN 1 AND 12 AND CierreEjercicioDia BETWEEN 1 AND 31)
    );
    -- Como maximo UN perfil vigente (sin fecha de fin) por cliente
    CREATE UNIQUE INDEX UX_PerfilFiscal_Vigente ON dbo.PerfilFiscal (ClienteId) WHERE VigenteHasta IS NULL;
    CREATE INDEX IX_PerfilFiscal_Cliente ON dbo.PerfilFiscal (GestoriaId, ClienteId, VigenteDesde);
END
GO

-- Obligacion: la entidad central -------------------------------------------------
IF OBJECT_ID(N'dbo.Obligacion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Obligacion
    (
        Id                          uniqueidentifier NOT NULL CONSTRAINT PK_Obligacion PRIMARY KEY CONSTRAINT DF_Obligacion_Id DEFAULT NEWSEQUENTIALID(),
        GestoriaId                  uniqueidentifier NOT NULL,
        ClienteId                   uniqueidentifier NOT NULL,
        ModeloCodigo                nvarchar(10)     NOT NULL,
        Ejercicio                   smallint         NOT NULL,
        Periodo                     nvarchar(2)      NOT NULL,
        Estado                      nvarchar(30)     NOT NULL CONSTRAINT DF_Obligacion_Estado DEFAULT N'PendienteDocumentacion',
        AsesorId                    uniqueidentifier NULL,
        FechaLimiteDomiciliacion    date             NULL,     -- NULL en modelos sin ingreso; el semaforo cae entonces a FechaLimitePresentacion
        FechaLimitePresentacion     date             NOT NULL,
        ImporteResultado            decimal(18,2)    NULL,
        SignoResultado              nvarchar(20)     NULL,     -- Ingresar | Devolver | Compensar | SinActividad
        FechaAprobacionClienteUtc   datetime2(3)     NULL,
        UsuarioAprobacionId         uniqueidentifier NULL,
        JustificanteDocumentoId     uniqueidentifier NULL,     -- FK a dbo.Documento se anade en la fase 3
        OrdenEnColumna              int              NOT NULL CONSTRAINT DF_Obligacion_Orden DEFAULT 0,
        ReglaOrigenId               int              NULL,
        FechaGeneracionUtc          datetime2(3)     NOT NULL CONSTRAINT DF_Obligacion_FechaGeneracion DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Obligacion_Gestoria  FOREIGN KEY (GestoriaId)          REFERENCES dbo.Gestoria (Id),
        CONSTRAINT FK_Obligacion_Cliente   FOREIGN KEY (ClienteId)           REFERENCES dbo.Cliente (Id),
        CONSTRAINT FK_Obligacion_Modelo    FOREIGN KEY (ModeloCodigo)        REFERENCES cat.ModeloTributario (Codigo),
        CONSTRAINT FK_Obligacion_Asesor    FOREIGN KEY (AsesorId)            REFERENCES dbo.Usuario (Id),
        CONSTRAINT FK_Obligacion_Aprobador FOREIGN KEY (UsuarioAprobacionId) REFERENCES dbo.Usuario (Id),
        CONSTRAINT FK_Obligacion_Regla     FOREIGN KEY (ReglaOrigenId)       REFERENCES cat.ReglaObligacion (Id),
        -- La clave que hace idempotente al motor: reejecutarlo no duplica nada
        CONSTRAINT UX_Obligacion UNIQUE (GestoriaId, ClienteId, ModeloCodigo, Ejercicio, Periodo),
        CONSTRAINT CK_Obligacion_Estado CHECK (Estado IN (N'PendienteDocumentacion', N'DocumentacionCompleta', N'EnPreparacion', N'RevisionInterna',
                                                          N'PendienteAprobacionCliente', N'Presentado', N'Cerrado', N'NoAplica')),
        CONSTRAINT CK_Obligacion_Periodo CHECK (Periodo IN (N'1T',N'2T',N'3T',N'4T',N'01',N'02',N'03',N'04',N'05',N'06',N'07',N'08',N'09',N'10',N'11',N'12',N'AN',N'1P',N'2P',N'3P')),
        CONSTRAINT CK_Obligacion_Signo   CHECK (SignoResultado IS NULL OR SignoResultado IN (N'Ingresar', N'Devolver', N'Compensar', N'SinActividad'))
    );
    -- Indices que sostienen el Kanban y el cuadro de mando (04-modelo-datos.md §5.1)
    CREATE INDEX IX_Obligacion_Tablero ON dbo.Obligacion (GestoriaId, Estado, FechaLimiteDomiciliacion)
        INCLUDE (ClienteId, ModeloCodigo, Ejercicio, Periodo, AsesorId);
    CREATE INDEX IX_Obligacion_Asesor  ON dbo.Obligacion (GestoriaId, AsesorId, Estado, FechaLimiteDomiciliacion);
    CREATE INDEX IX_Obligacion_Cliente ON dbo.Obligacion (GestoriaId, ClienteId, Ejercicio, Periodo);
END
GO

-- ObligacionHistorial: solo-anexado (RD-08) --------------------------------------
IF OBJECT_ID(N'dbo.ObligacionHistorial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ObligacionHistorial
    (
        Id              bigint           NOT NULL IDENTITY(1,1) CONSTRAINT PK_ObligacionHistorial PRIMARY KEY,
        GestoriaId      uniqueidentifier NOT NULL,   -- para RLS
        ObligacionId    uniqueidentifier NOT NULL,
        FechaUtc        datetime2(3)     NOT NULL CONSTRAINT DF_ObligacionHistorial_Fecha DEFAULT SYSUTCDATETIME(),
        UsuarioId       uniqueidentifier NULL,       -- NULL = motor / sistema
        TipoEvento      nvarchar(40)     NOT NULL,   -- Generada, CambioEstado, NoAplica, Reactivada, Reasignada, DocumentoVinculado, Mensaje, AprobacionCliente, Justificante...
        EstadoAnterior  nvarchar(30)     NULL,
        EstadoNuevo     nvarchar(30)     NULL,
        Comentario      nvarchar(1000)   NULL,
        ReferenciaId    nvarchar(64)     NULL,
        CONSTRAINT FK_ObligacionHistorial_Obligacion FOREIGN KEY (ObligacionId) REFERENCES dbo.Obligacion (Id),
        CONSTRAINT FK_ObligacionHistorial_Usuario    FOREIGN KEY (UsuarioId)    REFERENCES dbo.Usuario (Id)
    );
    CREATE INDEX IX_ObligacionHistorial_Obligacion ON dbo.ObligacionHistorial (GestoriaId, ObligacionId, FechaUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.TR_ObligacionHistorial_SoloAnexado', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER dbo.TR_ObligacionHistorial_SoloAnexado ON dbo.ObligacionHistorial
           INSTEAD OF UPDATE, DELETE AS
           BEGIN
               SET NOCOUNT ON;
               THROW 50002, N''El historial de una obligacion es de solo anexado (RD-08): no se puede modificar ni borrar.'', 1;
           END');
GO

-- RLS: incorporar las tablas nuevas a la politica -----------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.security_predicates p JOIN sys.security_policies s ON s.object_id = p.object_id
               WHERE s.name = N'PoliticaTenant' AND p.target_object_id = OBJECT_ID(N'dbo.Cliente'))
    EXEC(N'ALTER SECURITY POLICY dbo.PoliticaTenant
           ADD FILTER PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Cliente,
           ADD BLOCK  PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Cliente;');
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_predicates p JOIN sys.security_policies s ON s.object_id = p.object_id
               WHERE s.name = N'PoliticaTenant' AND p.target_object_id = OBJECT_ID(N'dbo.PerfilFiscal'))
    EXEC(N'ALTER SECURITY POLICY dbo.PoliticaTenant
           ADD FILTER PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.PerfilFiscal,
           ADD BLOCK  PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.PerfilFiscal;');
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_predicates p JOIN sys.security_policies s ON s.object_id = p.object_id
               WHERE s.name = N'PoliticaTenant' AND p.target_object_id = OBJECT_ID(N'dbo.Obligacion'))
    EXEC(N'ALTER SECURITY POLICY dbo.PoliticaTenant
           ADD FILTER PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Obligacion,
           ADD BLOCK  PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Obligacion;');
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_predicates p JOIN sys.security_policies s ON s.object_id = p.object_id
               WHERE s.name = N'PoliticaTenant' AND p.target_object_id = OBJECT_ID(N'dbo.ObligacionHistorial'))
    EXEC(N'ALTER SECURITY POLICY dbo.PoliticaTenant
           ADD FILTER PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.ObligacionHistorial,
           ADD BLOCK  PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.ObligacionHistorial;');
GO
