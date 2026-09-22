-- =============================================================================
-- 0002_catalogo_normativo.sql  ·  Esquema del catalogo normativo (cat)
-- -----------------------------------------------------------------------------
-- Idempotente. NO edita scripts anteriores.
-- Fuente de diseno: docs/04-modelo-datos.md §4.
-- El catalogo es dato compartido: NO lleva GestoriaId y queda FUERA de la
-- politica RLS (01-mapa-dominio.md §4.4).
-- Los DATOS del catalogo (modelos, reglas, plazos 2026/2027, festivos) van en
-- 0004_catalogo_datos_2026_2027.sql.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'cat')
    EXEC(N'CREATE SCHEMA cat AUTHORIZATION dbo;');
GO

-- Modelos tributarios (303, 111, 130...) y obligaciones mercantiles (CCAA, LIBROS)
IF OBJECT_ID(N'cat.ModeloTributario', N'U') IS NULL
BEGIN
    CREATE TABLE cat.ModeloTributario
    (
        Codigo             nvarchar(10)   NOT NULL CONSTRAINT PK_ModeloTributario PRIMARY KEY,
        Nombre             nvarchar(200)  NOT NULL,
        Descripcion        nvarchar(1000) NOT NULL,
        DescripcionCliente nvarchar(1000) NOT NULL,   -- lenguaje llano para el portal del cliente
        Periodicidad       nvarchar(20)   NOT NULL,   -- Trimestral | Mensual | Anual | PagoFraccionado
        EsInformativo      bit            NOT NULL CONSTRAINT DF_ModeloTributario_Informativo DEFAULT 0,
        EsResumenAnualDe   nvarchar(10)   NULL,       -- '390' es resumen anual de '303'
        Organismo          nvarchar(30)   NOT NULL CONSTRAINT DF_ModeloTributario_Organismo DEFAULT N'AEAT',
        CONSTRAINT FK_ModeloTributario_ResumenDe FOREIGN KEY (EsResumenAnualDe) REFERENCES cat.ModeloTributario (Codigo),
        CONSTRAINT CK_ModeloTributario_Periodicidad CHECK (Periodicidad IN (N'Trimestral', N'Mensual', N'Anual', N'PagoFraccionado')),
        CONSTRAINT CK_ModeloTributario_Organismo    CHECK (Organismo IN (N'AEAT', N'RegistroMercantil'))
    );
END
GO

-- Plazos por ejercicio y periodo. Se guarda la fecha YA TRASLADADA por dia
-- inhabil (RD-03); cat.DiaInhabil permite recalcular y verificar.
IF OBJECT_ID(N'cat.PlazoModelo', N'U') IS NULL
BEGIN
    CREATE TABLE cat.PlazoModelo
    (
        Id                       int          NOT NULL IDENTITY(1,1) CONSTRAINT PK_PlazoModelo PRIMARY KEY,
        ModeloCodigo             nvarchar(10) NOT NULL,
        Ejercicio                smallint     NOT NULL,
        Periodo                  nvarchar(2)  NOT NULL,   -- 1T..4T | 01..12 | AN | 1P..3P
        FechaInicioPresentacion  date         NOT NULL,
        FechaLimiteDomiciliacion date         NULL,       -- NULL en modelos sin ingreso (informativos)
        FechaLimitePresentacion  date         NOT NULL,
        Fuente                   nvarchar(300) NOT NULL,
        Confirmado               bit          NOT NULL CONSTRAINT DF_PlazoModelo_Confirmado DEFAULT 0,  -- el [VERIFICAR] hecho dato
        CONSTRAINT FK_PlazoModelo_Modelo FOREIGN KEY (ModeloCodigo) REFERENCES cat.ModeloTributario (Codigo),
        CONSTRAINT UX_PlazoModelo UNIQUE (ModeloCodigo, Ejercicio, Periodo),
        CONSTRAINT CK_PlazoModelo_Periodo CHECK (Periodo IN (N'1T',N'2T',N'3T',N'4T',N'01',N'02',N'03',N'04',N'05',N'06',N'07',N'08',N'09',N'10',N'11',N'12',N'AN',N'1P',N'2P',N'3P')),
        CONSTRAINT CK_PlazoModelo_Orden CHECK (FechaInicioPresentacion <= FechaLimitePresentacion
                                               AND (FechaLimiteDomiciliacion IS NULL OR FechaLimiteDomiciliacion <= FechaLimitePresentacion))
    );
END
GO

-- Reglas: "cuando aplica un modelo". Las condiciones de una regla son AND;
-- para expresar OR se crean dos reglas para el mismo modelo (el motor deduplica
-- por modelo+periodo y conserva la de menor Prioridad como origen).
IF OBJECT_ID(N'cat.ReglaObligacion', N'U') IS NULL
BEGIN
    CREATE TABLE cat.ReglaObligacion
    (
        Id                     int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_ReglaObligacion PRIMARY KEY,
        Clave                  nvarchar(60)  NOT NULL,   -- clave natural estable para seeds y tests: '303-TRIMESTRAL'
        ModeloCodigo           nvarchar(10)  NOT NULL,
        Descripcion            nvarchar(300) NOT NULL,
        Periodicidad           nvarchar(20)  NOT NULL,   -- Trimestral | Mensual | Anual | PagoFraccionado
        VigenteDesdeEjercicio  smallint      NOT NULL,
        VigenteHastaEjercicio  smallint      NULL,
        Prioridad              int           NOT NULL CONSTRAINT DF_ReglaObligacion_Prioridad DEFAULT 100,
        Activa                 bit           NOT NULL CONSTRAINT DF_ReglaObligacion_Activa DEFAULT 1,
        CONSTRAINT FK_ReglaObligacion_Modelo FOREIGN KEY (ModeloCodigo) REFERENCES cat.ModeloTributario (Codigo),
        CONSTRAINT UX_ReglaObligacion_Clave UNIQUE (Clave),
        CONSTRAINT CK_ReglaObligacion_Periodicidad CHECK (Periodicidad IN (N'Trimestral', N'Mensual', N'Anual', N'PagoFraccionado'))
    );
END
GO

IF OBJECT_ID(N'cat.ReglaCondicion', N'U') IS NULL
BEGIN
    CREATE TABLE cat.ReglaCondicion
    (
        Id        int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_ReglaCondicion PRIMARY KEY,
        ReglaId   int           NOT NULL,
        Atributo  nvarchar(60)  NOT NULL,   -- nombre del atributo del perfil fiscal o 'FormaJuridica'
        Operador  nvarchar(4)   NOT NULL,   -- = | <> | IN
        Valor     nvarchar(200) NOT NULL,   -- para IN: valores separados por coma
        CONSTRAINT FK_ReglaCondicion_Regla FOREIGN KEY (ReglaId) REFERENCES cat.ReglaObligacion (Id) ON DELETE CASCADE,
        CONSTRAINT CK_ReglaCondicion_Operador CHECK (Operador IN (N'=', N'<>', N'IN'))
    );
    CREATE INDEX IX_ReglaCondicion_Regla ON cat.ReglaCondicion (ReglaId);
END
GO

IF OBJECT_ID(N'cat.DiaInhabil', N'U') IS NULL
BEGIN
    CREATE TABLE cat.DiaInhabil
    (
        Fecha       date          NOT NULL,
        Ambito      nvarchar(50)  NOT NULL CONSTRAINT DF_DiaInhabil_Ambito DEFAULT N'Nacional',
        Descripcion nvarchar(200) NOT NULL,
        CONSTRAINT PK_DiaInhabil PRIMARY KEY (Fecha, Ambito)
    );
END
GO

-- Funcion de apoyo para que los scripts de datos guarden la fecha ya trasladada
-- (RD-03): sabado, domingo o dia inhabil del ambito => siguiente dia habil.
-- La logica equivalente vive en Aserta.Dominio (CalendarioHabil) y se prueba alli.
IF OBJECT_ID(N'cat.fn_SiguienteDiaHabil', N'FN') IS NOT NULL
    DROP FUNCTION cat.fn_SiguienteDiaHabil;
GO
CREATE FUNCTION cat.fn_SiguienteDiaHabil(@Fecha date, @Ambito nvarchar(50))
RETURNS date AS
BEGIN
    DECLARE @f date = @Fecha, @i int = 0;
    WHILE @i < 30
    BEGIN
        -- DATEPART(dw) depende de @@DATEFIRST; se usa una formula independiente: 1 = lunes ... 7 = domingo
        DECLARE @dow int = ((DATEPART(dw, @f) + @@DATEFIRST - 2) % 7) + 1;
        IF @dow < 6 AND NOT EXISTS (SELECT 1 FROM cat.DiaInhabil WHERE Fecha = @f AND Ambito = @Ambito)
            RETURN @f;
        SET @f = DATEADD(day, 1, @f);
        SET @i += 1;
    END
    RETURN @f;
END
GO
