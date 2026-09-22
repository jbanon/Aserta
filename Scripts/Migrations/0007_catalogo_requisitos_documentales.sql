-- =============================================================================
-- 0007_catalogo_requisitos_documentales.sql  ·  Reglas de requisito documental
-- -----------------------------------------------------------------------------
-- Idempotente. NO edita scripts anteriores.
-- "Que se espera recibir de un cliente para un periodo" tambien es dato, no
-- codigo (misma idea que cat.ReglaObligacion): asi el portal puede decir
-- "te faltan 2 facturas de julio" (04-modelo-datos.md §5.4).
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'cat.ReglaRequisito', N'U') IS NULL
BEGIN
    CREATE TABLE cat.ReglaRequisito
    (
        Id               int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_ReglaRequisito PRIMARY KEY,
        Clave            nvarchar(60)  NOT NULL,
        TipoDocumento    nvarchar(40)  NOT NULL,
        Descripcion      nvarchar(300) NOT NULL,   -- en lenguaje llano: es lo que ve el cliente
        Periodicidad     nvarchar(20)  NOT NULL,   -- Mensual | Trimestral | Anual
        CantidadPorMes   int           NULL,       -- NULL = sin cantidad fija ("al menos uno"); 1 = uno por mes del periodo
        Obligatorio      bit           NOT NULL CONSTRAINT DF_ReglaRequisito_Obligatorio DEFAULT 1,
        Prioridad        int           NOT NULL CONSTRAINT DF_ReglaRequisito_Prioridad DEFAULT 100,
        Activa           bit           NOT NULL CONSTRAINT DF_ReglaRequisito_Activa DEFAULT 1,
        CONSTRAINT UX_ReglaRequisito_Clave UNIQUE (Clave),
        CONSTRAINT CK_ReglaRequisito_Periodicidad CHECK (Periodicidad IN (N'Mensual', N'Trimestral', N'Anual'))
    );
END
GO

IF OBJECT_ID(N'cat.ReglaRequisitoCondicion', N'U') IS NULL
BEGIN
    CREATE TABLE cat.ReglaRequisitoCondicion
    (
        Id        int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_ReglaRequisitoCondicion PRIMARY KEY,
        ReglaId   int           NOT NULL,
        Atributo  nvarchar(60)  NOT NULL,
        Operador  nvarchar(4)   NOT NULL,
        Valor     nvarchar(200) NOT NULL,
        CONSTRAINT FK_ReglaRequisitoCondicion_Regla FOREIGN KEY (ReglaId) REFERENCES cat.ReglaRequisito (Id) ON DELETE CASCADE,
        CONSTRAINT CK_ReglaRequisitoCondicion_Operador CHECK (Operador IN (N'=', N'<>', N'IN'))
    );
    CREATE INDEX IX_ReglaRequisitoCondicion_Regla ON cat.ReglaRequisitoCondicion (ReglaId);
END
GO

-- Datos --------------------------------------------------------------------------------------
MERGE cat.ReglaRequisito AS r
USING (VALUES
    (N'FACTURAS-RECIBIDAS',  N'FacturaRecibida', N'Facturas de compras y gastos del mes',              N'Mensual', NULL, 1, 10),
    (N'FACTURAS-EMITIDAS',   N'FacturaEmitida',  N'Facturas emitidas a tus clientes en el mes',        N'Mensual', NULL, 1, 20),
    (N'EXTRACTO-BANCARIO',   N'ExtractoBancario',N'Extracto bancario del mes',                         N'Mensual', 1,    1, 30),
    (N'NOMINAS',             N'Nomina',          N'Nóminas del mes',                                   N'Mensual', 1,    1, 40),
    (N'RECIBO-ALQUILER',     N'ReciboAlquiler',  N'Recibo del alquiler del local',                     N'Mensual', 1,    1, 50),
    (N'TICKETS',             N'Ticket',          N'Tickets de gastos menores (opcional)',              N'Mensual', NULL, 0, 60)
) AS v (Clave, TipoDocumento, Descripcion, Periodicidad, CantidadPorMes, Obligatorio, Prioridad)
ON r.Clave = v.Clave
WHEN NOT MATCHED THEN INSERT (Clave, TipoDocumento, Descripcion, Periodicidad, CantidadPorMes, Obligatorio, Prioridad, Activa)
    VALUES (v.Clave, v.TipoDocumento, v.Descripcion, v.Periodicidad, v.CantidadPorMes, v.Obligatorio, v.Prioridad, 1);
GO

DECLARE @c TABLE (Clave nvarchar(60), Atributo nvarchar(60), Operador nvarchar(4), Valor nvarchar(200));
INSERT INTO @c VALUES
    (N'FACTURAS-RECIBIDAS', N'FormaJuridica', N'IN', N'Autonomo,SL,SA,CB'),
    (N'FACTURAS-EMITIDAS',  N'FormaJuridica', N'IN', N'Autonomo,SL,SA,CB'),
    (N'FACTURAS-EMITIDAS',  N'RegimenIva',    N'<>', N'NoAplica'),
    (N'EXTRACTO-BANCARIO',  N'FormaJuridica', N'IN', N'Autonomo,SL,SA,CB'),
    (N'NOMINAS',            N'TieneEmpleados', N'=', N'1'),
    (N'RECIBO-ALQUILER',    N'AlquilaLocal',  N'=',  N'1'),
    (N'TICKETS',            N'FormaJuridica', N'IN', N'Autonomo,SL,SA,CB');

DELETE rc FROM cat.ReglaRequisitoCondicion rc JOIN cat.ReglaRequisito r ON r.Id = rc.ReglaId WHERE r.Clave IN (SELECT DISTINCT Clave FROM @c);
INSERT INTO cat.ReglaRequisitoCondicion (ReglaId, Atributo, Operador, Valor)
SELECT r.Id, c.Atributo, c.Operador, c.Valor FROM @c c JOIN cat.ReglaRequisito r ON r.Clave = c.Clave;
GO
