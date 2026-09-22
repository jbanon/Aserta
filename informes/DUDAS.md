# Dudas para el arquitecto (lista viva)

> Formato: duda · **supuesto con el que he seguido** · a qué afecta · estado.
> Ninguna es bloqueante; todas tienen un supuesto aplicado. Se consultan en bloque al cerrar cada fase.

## Fase 1

| # | Duda | Supuesto aplicado | Afecta a | Estado |
|---|---|---|---|---|
| D-01 | ¿El perfil fiscal que aplica a un periodo es el vigente al inicio, al final o cualquiera? (04-modelo-datos §3.2 `[VERIFICAR]`) | **Al inicio del periodo**; si no había perfil entonces (alta a mitad de periodo), el primero que empiece dentro del periodo. Aislado en `MotorObligaciones.ResolverPerfil`. | Motor, tests `MotorObligacionesTests` | Abierta |
| D-02 | RD-10: ¿"estaba de alta en el periodo" significa todo el periodo o algún día? | **Algún día** (solapamiento). Un autónomo de baja el 15 de mayo conserva el 2T. | Motor | Abierta |
| D-03 | ¿Puede el motor **reactivar** una obligación `NoAplica` que vuelve a proceder? El diagrama la pinta como final. | **Sí, solo el motor** (evento `Reactivada` → `PendienteDocumentacion`). El menú manual no lo ofrece. | Máquina de estados | Abierta |
| D-04 | Plazos 2026/2027: he cargado el patrón general con `Confirmado = 0` y sin buscar el calendario oficial (regla 3 del encargo). ¿Quién y cuándo los contrasta? ¿Debo ocultar los no confirmados en la demo o mostrarlos con marca (como ahora)? | **Mostrar con marca** "sin confirmar" en catálogo y en el detalle de la obligación. | Catálogo, demo | Abierta |
| D-05 | Domiciliación = 5 días naturales antes del fin del plazo (15/25) y con traslado por inhábil. | Aplicado así `[VERIFICAR]`. | Semáforo (RD-02) | Abierta |
| D-06 | Periodos `1P/2P/3P` para el 202 (no estaban en el modelo). Rangos: ene–mar, abr–sep, oct–nov. | Añadidos al `CHECK` de `Periodo` y a `Periodo.Rango`. | Modelo de datos, catálogo | Abierta |
| D-07 | 390 exonerado para IVA mensual (SII/REDEME). ¿Correcto? | Regla `390-ANUAL` exige `PeriodicidadIva = Trimestral`. | Catálogo | Abierta |
| D-08 | `FechaCierreEjercicio` como mes+día. Los plazos del 200/CCAA/LIBROS asumen cierre 31-dic; para otros cierres haría falta un modelo de plazos relativos. ¿Lo hago en esta fase o lo dejo como extensión? | **Extensión**: el dato se guarda, no desplaza plazos. | Catálogo, motor | Abierta |
| D-09 | `Aplicacion` referencia el paquete `Microsoft.EntityFrameworkCore` para exponer `IAsertaDb` con `DbSet<T>` (no referencia Infraestructura). ¿Aceptable frente a la regla 2 de ADR-001? | **Sí**: la regla habla de referencias entre proyectos; sin esto habría que inventar un repositorio, que el ADR prohíbe. | Arquitectura | Abierta |
| D-10 | MFA: el encargo dice "Identity + MFA" en M0 y "Identity con roles" en la fase 1. ¿Implemento el enrolamiento TOTP (app autenticadora, QR con QRCoder) en esta fase? | **Hecho en la tarea 4** (informe 04): TOTP con app autenticadora, códigos de recuperación, obligatoriedad por usuario. | Login | Resuelta por decisión propia (revisable) |
| D-11 | Despliegue "push a master despliega": sin `sudo` ni receptor de webhooks. Propongo cron cada 2 min con `aserta.sh si-hay-cambios`. ¿Vale? ¿Quién instala la unidad systemd y `/etc/aserta-dev.env`? | Ficheros listos en `deploy/`; la web corre a mano en 5110 mientras tanto. | Despliegue | **Necesita acción del usuario** |
| D-12 | Subdominio del entorno de desarrollo (DA-12). | `aserta-dev.CAMBIAR-DOMINIO` en el nginx de `deploy/`. | nginx, certbot | Necesita acción del usuario |
| D-13 | Los tests de integración crean dos gestorías de prueba en la base `Aserta` y no las borran (la auditoría es inborrable). ¿Base separada `Aserta_Test`? `agente_ro` no puede crear bases. | Conviven con la demo; nombres inequívocos "Gestoría de prueba A/B". | Tests | Abierta |
| D-14 | `Scripts/Seed/` queda vacía: el seed de demo es C# (`SembradorDemo`) porque necesita `UserManager` para las contraseñas. | Documentado en el informe 03. | Estructura | Abierta |
| D-15 | Design system: sin `08-ux-ui/design-system.md` he definido tokens propios y la fuente Inter (OFL). ¿Sustituir cuando exista el documento? | Sí; los componentes solo leen tokens. | Front | Abierta |
| D-16 | `CLAUDE.md` dice "estructura pendiente de definir". ¿Lo actualizo yo con la estructura real de ADR-001? | No lo toco: es del arquitecto. | Documentación | Abierta |
| D-17 | `git push origin master` falla: GitHub responde *"Invalid username or token"* con el token embebido en la URL del remoto (`origin`). ¿Token caducado, sin permiso `repo`, o el repositorio `jbanon/Aserta` no existe todavía (DA-15)? | Los commits quedan en `master` local (7 commits de la fase 1); reintentaré el push en cuanto el remoto funcione. | Despliegue automático, revisión del arquitecto | **Necesita acción del usuario** |

## Fase 2

| # | Duda | Supuesto aplicado | Afecta a | Estado |
|---|---|---|---|---|
| D-18 | ¿Quién recibe los avisos de vencimiento: el asesor responsable, el socio, o ambos? | **El asesor** de la obligación (o el primer usuario de gestoría activo si no tiene asesor). | Avisos, planificador | Abierta |
| D-19 | El mapa de dominio §3.1 dice que las columnas del Kanban son "configurables". | Estados fijos (los de la máquina de estados); solo cambiaría el texto visible. No implementado. | Tablero | Abierta |
| D-20 | El seed simula el histórico de las obligaciones pasadas (cerradas con historial completo y borrador). ¿Aceptable como dato de demo o prefiere el arquitecto que todo nazca pendiente? | Simulado, con 1 de cada 23 vencida a propósito. Marcador `SeedHistoricoDemo` en `EjecucionProgramada`. | Demo | Abierta |
| D-21 | Tabla `dbo.Aviso` (script `0005`) no está en `04-modelo-datos.md`. | Añadida con RLS; columnas en el informe 05. | Modelo de datos | Abierta |
| D-22 | Umbrales del semáforo: verde > 7 días, ámbar ≤ 7, rojo ≤ 3, vencido < 0 (sobre la fecha de domiciliación). No estaban especificados. | Constantes en `Semaforo` (`DiasAmbar`, `DiasRojo`). | Kanban, calendario, avisos | Abierta |
