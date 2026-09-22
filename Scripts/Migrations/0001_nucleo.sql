-- =============================================================================
-- 0001_nucleo.sql  ·  Nucleo de plataforma (M0)
-- -----------------------------------------------------------------------------
-- Idempotente: se puede ejecutar N veces. Un script YA APLICADO NO SE EDITA:
-- toda correccion es un script nuevo (ADR-001 §2.6).
-- Fuente de diseno: docs/04-modelo-datos.md §1 y §2.
-- Contenido:
--   1. dbo.MigracionAplicada (la crea tambien el runner; aqui por completitud)
--   2. dbo.Gestoria (tenant)
--   3. Tablas de ASP.NET Core Identity (claves uniqueidentifier)
--   4. dbo.Usuario (extiende Identity: tenant, cliente, estado, MFA)
--   5. dbo.Auditoria (solo-anexado, RD-08)
--   6. dbo.EjecucionProgramada
--   7. RLS: dbo.fn_FiltroTenant + SECURITY POLICY dbo.PoliticaTenant (RD-04)
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- 1. Control de migraciones ----------------------------------------------------
IF OBJECT_ID(N'dbo.MigracionAplicada', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigracionAplicada
    (
        Nombre              nvarchar(200)  NOT NULL CONSTRAINT PK_MigracionAplicada PRIMARY KEY,
        HashSha256          char(64)       NOT NULL,
        FechaAplicacionUtc  datetime2(3)   NOT NULL CONSTRAINT DF_MigracionAplicada_Fecha DEFAULT SYSUTCDATETIME(),
        DuracionMs          int            NOT NULL CONSTRAINT DF_MigracionAplicada_Duracion DEFAULT 0
    );
END
GO

-- 2. Gestoria (tenant) ----------------------------------------------------------
IF OBJECT_ID(N'dbo.Gestoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Gestoria
    (
        Id                        uniqueidentifier NOT NULL CONSTRAINT PK_Gestoria PRIMARY KEY CONSTRAINT DF_Gestoria_Id DEFAULT NEWSEQUENTIALID(),
        Nombre                    nvarchar(200)    NOT NULL,
        Nif                       nvarchar(9)      NOT NULL,
        Estado                    nvarchar(20)     NOT NULL CONSTRAINT DF_Gestoria_Estado DEFAULT N'Activa',
        FechaAlta                 date             NOT NULL,
        ZonaHoraria               nvarchar(64)     NOT NULL CONSTRAINT DF_Gestoria_ZonaHoraria DEFAULT N'Europe/Madrid',
        -- RD-09 y RD-07 como configuracion del tenant, no como constante de codigo
        ExigeAprobacionCliente    bit              NOT NULL CONSTRAINT DF_Gestoria_ExigeAprobacion DEFAULT 1,
        AsesorVeTodosLosClientes  bit              NOT NULL CONSTRAINT DF_Gestoria_AsesorVeTodos DEFAULT 1,
        CONSTRAINT CK_Gestoria_Estado CHECK (Estado IN (N'Activa', N'Suspendida', N'Baja')),
        CONSTRAINT CK_Gestoria_Nif    CHECK (LEN(Nif) = 9)
    );
    CREATE UNIQUE INDEX UX_Gestoria_Nif ON dbo.Gestoria (Nif);
END
GO

-- 3. ASP.NET Core Identity -------------------------------------------------------
-- Esquema estandar de Identity con claves uniqueidentifier. Sin GestoriaId:
-- el login se hace por email antes de conocer el tenant. El tenant vive en
-- dbo.Usuario (1:1 con AspNetUsers), que SI esta bajo RLS.
IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles
    (
        Id               uniqueidentifier NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
        Name             nvarchar(256)    NULL,
        NormalizedName   nvarchar(256)    NULL,
        ConcurrencyStamp nvarchar(max)    NULL
    );
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers
    (
        Id                   uniqueidentifier   NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
        UserName             nvarchar(256)      NULL,
        NormalizedUserName   nvarchar(256)      NULL,
        Email                nvarchar(256)      NULL,
        NormalizedEmail      nvarchar(256)      NULL,
        EmailConfirmed       bit                NOT NULL,
        PasswordHash         nvarchar(max)      NULL,
        SecurityStamp        nvarchar(max)      NULL,
        ConcurrencyStamp     nvarchar(max)      NULL,
        PhoneNumber          nvarchar(max)      NULL,
        PhoneNumberConfirmed bit                NOT NULL,
        TwoFactorEnabled     bit                NOT NULL,
        LockoutEnd           datetimeoffset(7)  NULL,
        LockoutEnabled       bit                NOT NULL,
        AccessFailedCount    int                NOT NULL
    );
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
    CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
END
GO

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims
    (
        Id         int              NOT NULL IDENTITY(1,1) CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
        RoleId     uniqueidentifier NOT NULL,
        ClaimType  nvarchar(max)    NULL,
        ClaimValue nvarchar(max)    NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims (RoleId);
END
GO

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims
    (
        Id         int              NOT NULL IDENTITY(1,1) CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
        UserId     uniqueidentifier NOT NULL,
        ClaimType  nvarchar(max)    NULL,
        ClaimValue nvarchar(max)    NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims (UserId);
END
GO

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins
    (
        LoginProvider       nvarchar(450)    NOT NULL,   -- longitud por defecto del modelo de Identity (sin MaxLengthForKeys)
        ProviderKey         nvarchar(450)    NOT NULL,
        ProviderDisplayName nvarchar(max)    NULL,
        UserId              uniqueidentifier NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins (UserId);
END
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles
    (
        UserId uniqueidentifier NOT NULL,
        RoleId uniqueidentifier NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles (RoleId);
END
GO

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens
    (
        UserId        uniqueidentifier NOT NULL,
        LoginProvider nvarchar(450)    NOT NULL,
        Name          nvarchar(450)    NOT NULL,
        Value         nvarchar(max)    NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
END
GO

-- 4. Usuario (extension de Identity con tenant) ----------------------------------
IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario
    (
        Id              uniqueidentifier NOT NULL CONSTRAINT PK_Usuario PRIMARY KEY,
        GestoriaId      uniqueidentifier NOT NULL,
        ClienteId       uniqueidentifier NULL,      -- no nulo => usuario del lado cliente. FK en 0003 (Cliente aun no existe)
        NombreCompleto  nvarchar(200)    NOT NULL,
        Estado          nvarchar(20)     NOT NULL CONSTRAINT DF_Usuario_Estado DEFAULT N'Activo',
        MfaObligatorio  bit              NOT NULL CONSTRAINT DF_Usuario_Mfa DEFAULT 0,
        CONSTRAINT FK_Usuario_AspNetUsers FOREIGN KEY (Id)         REFERENCES dbo.AspNetUsers (Id),
        CONSTRAINT FK_Usuario_Gestoria    FOREIGN KEY (GestoriaId) REFERENCES dbo.Gestoria (Id),
        CONSTRAINT CK_Usuario_Estado CHECK (Estado IN (N'Activo', N'Bloqueado', N'Baja'))
    );
    CREATE INDEX IX_Usuario_Gestoria ON dbo.Usuario (GestoriaId, Estado);
    CREATE INDEX IX_Usuario_Cliente  ON dbo.Usuario (GestoriaId, ClienteId) WHERE ClienteId IS NOT NULL;
END
GO

-- 5. Auditoria (solo-anexado, RD-08) ---------------------------------------------
IF OBJECT_ID(N'dbo.Auditoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Auditoria
    (
        Id             bigint           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Auditoria PRIMARY KEY,
        GestoriaId     uniqueidentifier NOT NULL,
        UsuarioId      uniqueidentifier NULL,       -- NULL = sistema (runner, workers, motor)
        FechaUtc       datetime2(3)     NOT NULL CONSTRAINT DF_Auditoria_Fecha DEFAULT SYSUTCDATETIME(),
        Accion         nvarchar(50)     NOT NULL,   -- Alta, Modificacion, Baja, Login, CambioEstado, ...
        EntidadTipo    nvarchar(100)    NOT NULL,
        EntidadId      nvarchar(64)     NOT NULL,
        Detalle        nvarchar(max)    NULL,       -- JSON con los cambios (antes/despues)
        DireccionIp    nvarchar(45)     NULL,
        AgenteUsuario  nvarchar(500)    NULL
    );
    CREATE INDEX IX_Auditoria_Entidad ON dbo.Auditoria (GestoriaId, EntidadTipo, EntidadId, FechaUtc DESC);
    CREATE INDEX IX_Auditoria_Fecha   ON dbo.Auditoria (GestoriaId, FechaUtc DESC);
END
GO

IF OBJECT_ID(N'dbo.TR_Auditoria_SoloAnexado', N'TR') IS NULL
    EXEC(N'CREATE TRIGGER dbo.TR_Auditoria_SoloAnexado ON dbo.Auditoria
           INSTEAD OF UPDATE, DELETE AS
           BEGIN
               SET NOCOUNT ON;
               THROW 50002, N''La auditoria es de solo anexado (RD-08): no se puede modificar ni borrar.'', 1;
           END');
GO

-- 6. Ejecuciones programadas ------------------------------------------------------
IF OBJECT_ID(N'dbo.EjecucionProgramada', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EjecucionProgramada
    (
        Tarea               nvarchar(100) NOT NULL CONSTRAINT PK_EjecucionProgramada PRIMARY KEY,
        UltimaEjecucionUtc  datetime2(3)  NULL,
        ProximaEjecucionUtc datetime2(3)  NULL,
        Estado              nvarchar(30)  NOT NULL CONSTRAINT DF_EjecucionProgramada_Estado DEFAULT N'Pendiente'
    );
END
GO

-- 7. Row-Level Security (RD-04) ----------------------------------------------------
-- Verificado el 2026-09-22 contra esta misma base: funciona tambien para db_owner.
-- SESSION_CONTEXT lo fija el interceptor de conexion de EF Core; el runner y el
-- seed usan EsMantenimiento = 1.
IF OBJECT_ID(N'dbo.fn_FiltroTenant', N'IF') IS NULL
    EXEC(N'CREATE FUNCTION dbo.fn_FiltroTenant(@GestoriaId uniqueidentifier)
           RETURNS TABLE WITH SCHEMABINDING AS
           RETURN SELECT 1 AS Permitido
           WHERE @GestoriaId = CAST(SESSION_CONTEXT(N''GestoriaId'') AS uniqueidentifier)
              OR CAST(SESSION_CONTEXT(N''EsMantenimiento'') AS bit) = 1;');
GO

IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'PoliticaTenant')
    EXEC(N'CREATE SECURITY POLICY dbo.PoliticaTenant
           ADD FILTER PREDICATE dbo.fn_FiltroTenant(Id) ON dbo.Gestoria,
           ADD BLOCK  PREDICATE dbo.fn_FiltroTenant(Id) ON dbo.Gestoria,
           ADD FILTER PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Usuario,
           ADD BLOCK  PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Usuario,
           ADD FILTER PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Auditoria,
           ADD BLOCK  PREDICATE dbo.fn_FiltroTenant(GestoriaId) ON dbo.Auditoria
           WITH (STATE = ON, SCHEMABINDING = ON);');
GO
