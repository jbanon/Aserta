# Aserta

Plataforma SaaS para gestorías españolas y sus clientes: obligaciones fiscales derivadas del perfil fiscal, Kanban y calendario, portal del cliente, facturación Veri\*Factu y exportación contable. **Demo en construcción por fases** (ver `docs/00-vision-y-alcance.md`).

## Estructura

```
src/Aserta.Dominio          entidades, motor de obligaciones, máquina de estados (sin dependencias)
src/Aserta.Aplicacion       casos de uso y puertos (IAsertaDb, IRelojSistema, IGestorIdentidad…)
src/Aserta.Infraestructura  EF Core, Identity, runner de migraciones, interceptores de tenant y auditoría, seed
src/Aserta.Verifactu        huella, XML, QR, cliente AEAT (fase 4)
src/Aserta.Exportacion      exportadores contables (fase 5)
src/Aserta.Web              Razor Pages, htmx, CSS propio, composición
tests/                      Dominio.Tests · Integracion.Tests · Verifactu.Tests
Scripts/Migrations/         scripts SQL idempotentes numerados; los aplica el runner al arrancar
deploy/                     unidad systemd, sitio nginx, script de despliegue
informes/                   informes del Agente Programador y DUDAS.md
docs/                       especificación del Agente Arquitecto
```

## Arranque en desarrollo

```bash
dotnet user-secrets set "ConnectionStrings:Aserta" "Server=localhost,1433;Database=Aserta;User Id=agente_ro;Password=<ver usuarioBD.md>;TrustServerCertificate=True;Encrypt=True" --project src/Aserta.Web
dotnet run --project src/Aserta.Web      # http://127.0.0.1:5110
dotnet test                              # dominio + integración (necesita la base)
```

Al arrancar se aplican las migraciones pendientes y, con `Demo:Sembrar = true` (por defecto en `Development`), se siembran datos **ficticios**. Usuarios de demo: `socio@demo.aserta.local`, `carlos@…`, `lucia@…`, `pedro@…`, `panaderia@…`; contraseña `Aserta-Demo-2026!`. Segundo factor opcional (TOTP) desde «Mi cuenta». El usuario `panaderia@…` entra en el **portal del cliente** (móvil primero).

## Reglas de trabajo

- **Un script SQL aplicado no se edita nunca**: toda corrección es un script nuevo. El runner aborta el arranque si cambia el hash de uno aplicado.
- La contraseña de la base de datos no entra en ningún fichero versionado (`usuarioBD.md` está en `.gitignore`).
- Dominio y base de datos en español, sin tildes ni `ñ` en identificadores.
- Datos normativos marcados `[VERIFICAR]` (y plazos con `Confirmado = 0`) no se resuelven inventando: se contrastan con la AEAT.
