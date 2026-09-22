# 12 · Infraestructura y despliegue

> **Estado:** parcial · **Versión:** 0.2 · **Fecha:** 2026-09-22
> Contiene **hechos verificados** sobre el servidor de desarrollo (marcados ✅) y **propuestas pendientes de confirmar** (marcadas ⏳). Ninguna IP ni dominio está inventado.
> Pendiente de redactar: unidad systemd completa, bloque nginx, script de despliegue y checklist de verificación.

---

## 1. Servidor de desarrollo — estado comprobado

Comprobado el 2026-09-22 sobre la máquina Hetzner donde trabajan los agentes.

| Elemento | Valor | |
|---|---|---|
| SO | Linux, nginx como proxy inverso, proyectos en `~/proyectos/`, scripts en `~/scripts/` | ✅ |
| Runtime | **.NET 10** (10.0.112) instalado | ✅ |
| RAM total | **3,8 GB**, compartidos con otros diez proyectos. En la inspección: ~840 MB disponibles y **swap al 84 %** | ✅ |
| SQL Server | **2022 (RTM-CU25), Developer Edition**, en contenedor Docker `sqlserver`, publicado en `0.0.0.0:1433` | ✅ |
| Puertos ocupados | 22, 80, 443, 1433, **5102**, **5104**, **5187** | ✅ |
| Convenciones observadas | servicio systemd en kebab-case (`portal-persycom.service`), sitio nginx `<proyecto>-dev`, script en `~/scripts/<proyecto>.sh` | ✅ |

## 2. Base de datos

| Elemento | Valor | |
|---|---|---|
| Servidor | Instancia SQL Server local, puerto 1433 | ✅ |
| Base de datos | **`Aserta`** (ya creada y **vacía**: 0 tablas) | ✅ |
| Usuario | **`agente_ro`** | ✅ |
| Permisos reales | **`db_owner`** sobre `Aserta`. **No es de solo lectura pese al sufijo `_ro` del nombre.** Sin roles de servidor: no puede crear logins ni cambiar configuración de la instancia | ✅ |
| Credenciales | En `usuarioBD.md`, en la raíz del proyecto. **Fichero excluido del repositorio por `.gitignore`** | ✅ |
| Cifrado de conexión | El servidor presenta certificado autofirmado ⇒ la cadena de conexión necesita `TrustServerCertificate=True` en desarrollo | ✅ |

### 2.1 Cómo se configura la cadena de conexión

**La contraseña no entra nunca en `appsettings.json` ni en ningún fichero versionado.** En desarrollo se usa el gestor de secretos de .NET:

```
dotnet user-secrets init --project src/Aserta.Web
dotnet user-secrets set "ConnectionStrings:Aserta" \
  "Server=localhost,1433;Database=Aserta;User Id=agente_ro;Password=<la de usuarioBD.md>;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=False"
```

En el servicio systemd, la misma cadena viaja como variable de entorno `ConnectionStrings__Aserta` desde un fichero de entorno con permisos `600`, fuera del directorio de despliegue.

## 3. Verificación de las decisiones de arquitectura contra la base de datos real

Se probaron sobre la base `Aserta` y se eliminaron los objetos de prueba. **Resultados:**

| Prueba | Resultado |
|---|---|
| **RLS con `SESSION_CONTEXT`** (RD-04) | ✅ **Funciona.** Con dos filas de dos tenants distintos, el tenant A ve exactamente 1 fila y el tenant B exactamente 1, mientras la tabla contiene 2. Y funciona **también para `db_owner`**: RLS no se salta por ser propietario |
| **`BLOCK PREDICATE`** | ✅ **Funciona.** Insertar una fila con el `GestoriaId` de otro tenant se rechaza |
| **Triggers `INSTEAD OF UPDATE, DELETE`** (RD-06) | ✅ **Funcionan, incluso para `db_owner`.** `UPDATE` y `DELETE` sobre la tabla protegida lanzan el error esperado |
| **`DENY UPDATE, DELETE` al usuario de la aplicación** | ❌ **No disponible con la configuración actual.** SQL Server responde: *«Cannot grant, deny, or revoke permissions to sa, dbo, entity owner, information_schema, sys, or yourself»* |

### 3.1 Consecuencia: la segunda barrera de inalterabilidad no está disponible

El diseño ([ADR-001](adr/ADR-001-arquitectura-general-y-estructura-de-solucion.md) §2.7, [04-modelo-datos.md](04-modelo-datos.md) §6.1) preveía **dos** barreras para la inalterabilidad de las facturas: triggers **y** `DENY` al usuario de la aplicación. Con un único usuario `db_owner`:

- la barrera de **triggers funciona** y es la principal — está verificada;
- la barrera de **`DENY` no se puede aplicar**, porque un miembro de `db_owner` no puede denegarse permisos a sí mismo y, aunque se le denegaran, `db_owner` los ignora;
- además, un `db_owner` **puede deshabilitar un trigger** (`DISABLE TRIGGER`). La garantía es, por tanto, *fuerte frente a errores de programación* pero **no frente a un uso deliberado** desde la propia aplicación.

**Decisión del usuario (2026-09-22): se trabaja así y no pasa nada.** Para la demo, la barrera de triggers es suficiente, está verificada y no se pide ningún cambio. Queda anotado aquí para que la decisión sea consciente y no un descuido, y para retomarlo si algún día el proyecto va a producción.

El script de migración escribirá igualmente el `DENY ... ON SCHEMA::vf`, **condicionado a que el login exista**, de modo que la segunda barrera entre en vigor por sí sola el día que se cree un usuario de aplicación restringido. Coste cero hoy.

### 3.2 Pendiente con la instancia (requiere `sysadmin`)

| # | Petición | Estado |
|---|---|---|
| 1 | Login **`aserta_app`** restringido (sin `db_owner`) para el runtime, dejando `agente_ro` solo para migraciones | **No se pide ahora.** Anotado para antes de producción |
| 2 | Fijar **`max server memory`** de SQL Server | ⚠️ **Sí conviene.** Está en el valor por defecto (**ilimitado**); hoy consume 437 MB pero crecerá hasta ocupar la RAM disponible de una máquina de 3,8 GB con el swap al 84 %. Propuesta: **1.024–1.536 MB** |

## 4. Nombres y puertos del proyecto  ⏳

Propuesta sin colisión con lo existente, pendiente de confirmar el subdominio (requiere DNS):

| Elemento | Propuesta |
|---|---|
| Servicio systemd | `aserta-dev.service` |
| Puerto Kestrel (loopback) | **5110** |
| Sitio nginx | `/etc/nginx/sites-available/aserta-dev` |
| Subdominio | ⏳ **pendiente de confirmar** |
| Base de datos | `Aserta` ✅ (ya creada) |
| Almacén documental | `/var/lib/aserta/documentos`, fuera del directorio de despliegue |
| Script de despliegue | `~/scripts/aserta.sh` |

## 5. Presupuesto de memoria  ⏳

Con 3,8 GB compartidos, cada componente necesita techo explícito. Valores **a medir y ajustar**, no definitivos:

| Componente | Techo propuesto | Cómo se impone |
|---|---|---|
| SQL Server | 1.024–1.536 MB | `max server memory` (pendiente, §3.2) |
| Aplicación Aserta | ~350 MB en reposo | `MemoryMax` en la unidad systemd |
| Chromium (PDF) | ~600 MB de pico | Instancia única + cola de un consumidor + reinicio periódico del navegador |

## 6. Riesgos y mejoras sugeridas

| # | Riesgo | Propuesta |
|---|---|---|
| I1 | **`max server memory` ilimitado** en una máquina con swap al 84 % | Petición §3.2.2. Es el riesgo operativo más inmediato del proyecto |
| I2 | La aplicación corriendo como `db_owner` deja la inalterabilidad en una sola barrera | **Aceptado por el usuario para la demo.** Los triggers están verificados y funcionan. Revisar antes de producción (§3.2.1) |
| I3 | **Credenciales en claro en `usuarioBD.md`** dentro del directorio del proyecto | Ya excluido por `.gitignore`. Verificar en cada `git status` antes de publicar el repositorio en GitHub. Para producción, fichero de entorno con permisos `600` |
| I4 | Certificado autofirmado de SQL Server obliga a `TrustServerCertificate=True` | Aceptable en desarrollo sobre `localhost`. En producción, certificado válido y `TrustServerCertificate=False` |
| I5 | Desplegar con instancia única implica corte de servicio | Irrelevante en la demo; documentar la ventana para producción |
