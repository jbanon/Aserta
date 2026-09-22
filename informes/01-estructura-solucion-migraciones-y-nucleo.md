# 01 · Estructura de la solución, runner de migraciones y núcleo multi-tenant (M0)

> **Fase 1 · tarea 1** · Agente Programador · 2026-09-22

## Qué he construido

La solución completa según ADR-001 (seis proyectos + tres de tests), los scripts SQL `0001`–`0003` derivados de `docs/04-modelo-datos.md`, el runner de migraciones con control de hash, el `DbContext` con filtros por tenant, el interceptor de conexión que fija `SESSION_CONTEXT` para RLS, el interceptor de auditoría append-only, ASP.NET Core Identity con los cinco roles y el contexto de ejecución (tenant + usuario) que rellena el middleware web.

## Decisiones que he tomado yo

1. **He creado `Scripts/Migrations/0001_*.sql` yo mismo** (no existía), y lo he partido en tres scripts para que sean revisables: `0001_nucleo.sql` (Identity, Gestoria, Usuario, Auditoria, EjecucionProgramada, RLS), `0002_catalogo_normativo.sql` (esquema `cat`), `0003_clientes_y_obligaciones.sql` (Cliente, PerfilFiscal, Obligacion, ObligacionHistorial). El esquema `vf` (Veri\*Factu) y las tablas documentales/mensajería **no** están todavía: irán en scripts nuevos en las fases 3 y 4. Cada script es idempotente (`IF OBJECT_ID(...) IS NULL`).
2. **`Aserta.Aplicacion` referencia el paquete `Microsoft.EntityFrameworkCore`** (solo el paquete base, no SqlServer ni el proyecto Infraestructura) para exponer la unidad de trabajo como `IAsertaDb` con `DbSet<T>`. El ADR prohíbe repositorios genéricos y dice que el DbContext ya es la unidad de trabajo; sin esa referencia la capa de aplicación no podría consultar. El grafo de proyectos del ADR se respeta y lo comprueba `ArquitecturaTests`.
3. **`dbo.Usuario` es una tabla aparte de `AspNetUsers` (1:1 por `Id`)**, como dice el modelo. Consecuencia: el login busca en `AspNetUsers` por email (sin tenant) y **la fábrica de claims** (`FabricaClaims`) lee `dbo.Usuario` en un **ámbito de mantenimiento explícito** para meter `GestoriaId`, `ClienteId` y nombre en la cookie. Es el único punto, junto con la página de login, donde se consulta un usuario sin tenant.
4. **Identity con claves `uniqueidentifier`**. Longitudes de `AspNetUserLogins`/`AspNetUserTokens` a `nvarchar(450)` (las que genera el modelo de Identity 10 sin `MaxLengthForKeys`), para que el test de deriva de esquema cuadre. Sin tabla de passkeys: Identity 10.0.5 no la incluye en `IdentityDbContext`.
5. **Filtros EF y RLS con "modo mantenimiento"**: `HasQueryFilter(e => EsMantenimiento || e.GestoriaId == GestoriaActual)`. Sin tenant y sin mantenimiento **no se ve nada** (ni siquiera la tabla `Gestoria`, filtrada por su propio `Id`). El interceptor fija **las dos claves de `SESSION_CONTEXT` en cada apertura de conexión**, también cuando viene del pool (riesgo M1 del modelo de datos); hay test que alterna tenants sobre el mismo pool.
6. **`GestoriaId` redundante en `PerfilFiscal` y `ObligacionHistorial`**: el modelo no lo listaba, pero sin él no pueden estar bajo la política RLS ni bajo el filtro EF. Lo he añadido.
7. **`FechaCierreEjercicio` → `CierreEjercicioMes` + `CierreEjercicioDia`** (`tinyint`): una fecha con año no tiene sentido para algo recurrente. Todavía no desplaza plazos (ver huecos).
8. **Auditoría por interceptor de `SaveChanges`** sobre `Gestoria`, `Usuario`, `Cliente`, `PerfilFiscal` y `Obligacion`: JSON con todos los valores en altas y `{antes, después}` por propiedad en modificaciones. Los eventos que no son cambios de fila (login, login fallido, cierre de sesión, ejecución del motor) usan `IRegistroAuditoria`. `UsuarioId` nulo = sistema. La IP sale de `RemoteIpAddress` tras `ForwardedHeaders` (solo loopback como proxy conocido).
9. **Triggers `INSTEAD OF UPDATE, DELETE`** en `Auditoria` y `ObligacionHistorial` (error 50002). El `DENY` al login de aplicación queda para el script del esquema `vf` (fase 4), condicionado a que exista `aserta_app`, como pide el encargo.
10. **`ValueGeneratedNever()` en las claves `uniqueidentifier`**: la aplicación asigna los GUID (`Guid.NewGuid()`); el `DEFAULT NEWSEQUENTIALID()` de las tablas queda como red para inserciones por SQL. Motivo: el interceptor de auditoría necesita el `Id` antes del `INSERT`.
11. **Runner**: hash SHA-256 sobre el contenido normalizado (CRLF→LF, sin BOM, sin espacio final) para que git no lo altere; cada script en su transacción; lotes separados por `GO`; timeout de 300 s por lote; crea `dbo.MigracionAplicada` si no existe antes de leer nada. Un hash distinto en un script ya aplicado **aborta el arranque** con mensaje claro.
12. **Secretos**: cadena de conexión en `dotnet user-secrets` (id `aserta-web-dev`); el servicio la recibe por `ConnectionStrings__Aserta` desde `/etc/aserta-dev.env`. `usuarioBD.md` sigue ignorado por git (comprobado antes de cada commit).
13. **Paquetes con versiones centralizadas** (`Directory.Packages.props`): EF Core e Identity 10.0.5, `Microsoft.Data.SqlClient` 6.1.4, xunit 2.9.3.

## Desviaciones

- El modelo de datos nombra `Auditoria.EntidadId` sin tipo; he usado `nvarchar(64)` para admitir GUID y `bigint` como texto.
- `Usuario.ClienteId` tiene su FK a `Cliente` añadida en `0003` (la tabla `Cliente` no existe en `0001`).
- El índice único filtrado de `PerfilFiscal` (un solo perfil vigente) está tal cual; además he añadido `CHECK` de coherencia de fechas.

## Huecos encontrados

- `13-migraciones-y-datos.md` (convenciones del runner y seeds) no existe: he fijado las convenciones descritas arriba. Si el arquitecto las quiere distintas, el runner es un único fichero (`RunnerMigraciones.cs`).
- No hay documento `05-paginas-y-endpoints.md` ni `08-ux-ui/`: ver informe 03.
- El modelo dice que `Auditoria` se "sella" con `DENY` además del trigger; con `db_owner` no es posible (documentado en `12-infraestructura`). Queda en una barrera.

## Cómo probarlo

```bash
cd ~/proyectos/Aserta
dotnet user-secrets set "ConnectionStrings:Aserta" "Server=localhost,1433;Database=Aserta;User Id=agente_ro;Password=<usuarioBD.md>;TrustServerCertificate=True;Encrypt=True" --project src/Aserta.Web
dotnet run --project src/Aserta.Web        # aplica migraciones, siembra la demo, escucha en http://127.0.0.1:5110
```

Comprobar RLS a mano (cualquier cliente SQL con `agente_ro`): `SELECT COUNT(*) FROM dbo.Cliente` devuelve **0** sin `SESSION_CONTEXT`; con `EXEC sp_set_session_context 'EsMantenimiento', 1` devuelve todo. `SELECT * FROM dbo.MigracionAplicada` muestra los cuatro scripts con su hash.

## Estado de los tests

- `Aserta.Integracion.Tests` (21, todos en verde, contra la base `Aserta` real): doble barrera RLS (incluido `IgnoreQueryFilters()` → 0 filas y `BLOCK PREDICATE` al insertar en nombre de otro tenant), alternancia de tenants sobre el pool, solo-anexado de `Auditoria`, runner (segunda ejecución no aplica nada; hash alterado aborta; numeración sin huecos), **deriva de esquema** (modelo EF vs `INFORMATION_SCHEMA`: tablas, columnas, tipos y nulabilidad), reglas de arquitectura (Dominio sin paquetes, Aplicación sin Infraestructura/AspNet, Verifactu sin EF), CSP sin `unsafe-*`, POST sin antiforgery → 400.
- Los tests de integración crean dos gestorías de prueba (`Gestoría de prueba A/B`) en la base de desarrollo y **no las borran** (la auditoría no se puede borrar por diseño). Son idempotentes.
- No cubierto: caducidad/rotación de la cookie, bloqueo por intentos fallidos (configurado, no probado), MFA (no implementado todavía; `MfaObligatorio` existe como dato).
