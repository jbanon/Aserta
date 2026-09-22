# 00 · Visión y alcance

> **Estado:** vigente · **Versión:** 1.0 · **Fecha:** 2026-09-22 · **Autor:** Agente Arquitecto
> **Nombre del producto:** *pendiente de confirmar* (provisional **GestorFlow**; el directorio de trabajo se llama `Aserta`). Ver [decisiones-abiertas.md](decisiones-abiertas.md) · DA-01.

---

## 1. Qué es esto, en una frase

Una plataforma SaaS donde una **gestoría** lleva el control de todas las obligaciones fiscales de todos sus clientes, y donde cada **cliente** sube su documentación, aprueba sus impuestos y emite sus facturas legales — sin correos, sin WhatsApp y sin hojas de cálculo.

## 2. Para el lector que no conoce el sector

Una **gestoría** (o asesoría) es un despacho profesional que se encarga, por una cuota mensual, de la contabilidad, los impuestos y los trámites administrativos de sus clientes: autónomos, pequeñas sociedades y particulares. Un despacho mediano lleva entre 150 y 600 clientes con 3–10 personas.

Su trabajo tiene tres características que definen el producto:

1. **Es cíclico y con picos brutales.** Hacienda concentra los vencimientos: los días 1 al 20 de enero, abril, julio y octubre el despacho presenta *cientos* de declaraciones trimestrales. Fuera de esos días hay calma relativa. El producto tiene que aplanar ese pico.
2. **Depende de que el cliente entregue papeles.** El gestor no puede hacer el IVA de un cliente hasta que ese cliente le envía sus facturas. La mayor parte del tiempo perdido en una gestoría no es técnico: es **perseguir documentación**. Este es el dolor número uno y el núcleo del producto.
3. **El error se paga.** Presentar fuera de plazo es un recargo automático; presentar mal es una sanción. El socio del despacho vive con el miedo de que "se haya escapado" un modelo de un cliente. El producto vende tranquilidad: *ninguna obligación puede caerse del radar*.

### Vocabulario mínimo

| Término | Qué significa |
|---|---|
| **Modelo** | Un formulario oficial de Hacienda, identificado por un número. El "303" es la declaración trimestral de IVA; el "111", las retenciones de nóminas y profesionales. |
| **Obligación** | El deber concreto de un cliente de presentar un modelo en un periodo: *"Panadería López SL debe presentar el 303 del 3T de 2026"*. Es la unidad de trabajo del producto. |
| **Ejercicio / periodo** | Ejercicio = año fiscal. Periodo = el trozo del año que declara el modelo (1T, 2T, 3T, 4T, un mes, o el año entero). |
| **Perfil fiscal** | El conjunto de características de un cliente (forma jurídica, régimen de IVA, si tiene empleados, si alquila local, territorio…) del que **se deducen** sus obligaciones. |
| **Veri\*Factu** | Sistema obligatorio por el que el programa de facturación debe generar registros inalterables y encadenados de cada factura y remitirlos a la AEAT. Ver `docs/06-verifactu/`. |
| **AEAT** | Agencia Estatal de Administración Tributaria: Hacienda. |
| **Colaborador social** | Figura por la que una gestoría presenta declaraciones en nombre de sus clientes con su propio certificado electrónico. |

Glosario completo en [01-mapa-dominio.md](01-mapa-dominio.md) §6.

## 3. Problema y propuesta de valor

| Dolor actual del despacho | Qué hace la plataforma |
|---|---|
| El socio no sabe, a día 12 de abril, cuántos 303 quedan por presentar ni de quién falta documentación | Kanban y cuadro de mando en tiempo real, con semáforo de vencimiento |
| El gestor reclama documentos por correo/teléfono, uno a uno | Reclamación automática por cliente y periodo, con recordatorios escalados |
| El cliente no sabe qué le falta ni qué tiene que pagar | Portal móvil: *"Te faltan 2 facturas de julio"* y aprobación del borrador con un botón |
| Las obligaciones se mantienen a mano en un Excel y se olvidan altas/bajas | Motor de reglas: el perfil fiscal genera las obligaciones del ejercicio automáticamente |
| Desde 2027 los clientes necesitan un software de facturación homologado | Módulo de facturación Veri\*Factu incluido: el despacho deja de perder ese cliente y gana un servicio nuevo |
| No hay rastro de quién aprobó qué | Auditoría append-only y trazabilidad completa por obligación |

**Tesis comercial:** el calendario legal de Veri\*Factu (1 enero 2027 para sociedades, 1 julio 2027 para el resto `[VERIFICAR]` — ver `06-verifactu/especificacion.md`) obliga a cientos de miles de empresas a cambiar de herramienta de facturación **en los próximos meses**. Quien les acompaña en ese cambio es su gestoría. Entrar por la facturación y quedarse con la gestión integral es la jugada del producto. Detalle en [10-argumentario-comercial.md](10-argumentario-comercial.md).

## 4. Para quién

- **Cliente que paga:** gestorías y asesorías españolas de 3 a 30 empleados, en territorio común (Península y Baleares). *País Vasco y Navarra quedan fuera del alcance inicial: tienen sus propios sistemas, p. ej. TicketBAI, y no entran en Veri\*Factu* `[VERIFICAR]`.
- **Usuarios dentro de la gestoría:** socio/director, asesor, administrativo.
- **Usuarios finales:** los clientes de la gestoría (autónomos, sociedades, particulares) y sus empleados.

Modelo multi-tenant: `Gestoría (tenant) → Clientes → Usuarios`.

## 5. Alcance de la DEMO

Objetivo de la demo: **enseñar en 15 minutos** (guion en [11-guion-demo.md](11-guion-demo.md)) un producto creíble, vistoso y técnicamente honesto, que pueda evolucionar a producto real sin tirar nada a la basura.

### 5.1 Módulos incluidos

| Id | Módulo | Qué se demuestra | Fase |
|---|---|---|---|
| **M0** | Núcleo de plataforma | Multi-tenant con aislamiento doble (filtros EF + RLS de SQL Server), Identity + MFA, RBAC, auditoría inmutable | 1 |
| **M1** | Ficha de cliente y perfil fiscal | Alta de cliente, validación de NIF/CIF, y **motor de reglas** que deriva las obligaciones del ejercicio | 1 |
| **M4** | Kanban de obligaciones y calendario | Tarjeta por obligación, drag & drop entre estados, semáforo de vencimiento, vista calendario, carga por asesor, avisos automáticos | 2 |
| **M2** | Portal del cliente (mobile-first) | Subida de documentos desde el móvil, "qué me falta", mensajería contextual, aprobación de borradores | 3 |
| **M3** | Gestión documental (gestoría) | Bandeja de entrada, ciclo `recibido → en revisión → validado \| rechazado`, extracción OCR/IA **simulada** tras adaptador | 3 |
| **M6** | Facturación Veri\*Factu | Series, emisión F1/F2/R1–R5, huella encadenada, QR, PDF, cola de reintentos y **simulador de AEAT** con fallos provocables | 4 |
| **M5** | Exportación contable | Interfaz `ExportadorContable`, un formato real (candidato A3) + CSV genérico, histórico y antiduplicados | 5 |
| **M7** | Cuadro de mando del socio | Obligaciones por estado, vencimientos de la semana, clientes incompletos, productividad, estado de envíos Veri\*Factu | 5 |

Orden y dependencias en [backlog/00-orden-de-implementacion.md](../backlog/00-orden-de-implementacion.md).

### 5.2 Lo que la demo simula a propósito (y se dice en voz alta)

Simular no es engañar: cada simulación vive **detrás de un adaptador con el mismo contrato que la implementación real**, de modo que sustituirla sea cambiar una línea de configuración.

| Simulado | Adaptador | Qué haría en real |
|---|---|---|
| Envío a la AEAT | `IClienteAeatVerifactu` → `SimuladorAeat` | SOAP + mTLS contra preproducción/producción |
| Extracción de datos de facturas | `IExtractorDocumental` → datos precargados | OCR/LLM sobre el PDF real |
| Envío de correo/SMS de avisos | `INotificador` → bandeja en pantalla | SMTP / proveedor de SMS |
| Presentación del modelo en AEAT | — | El justificante se registra **a mano**: presentar modelos está fuera de alcance |

### 5.3 Fuera del alcance de la demo

Se diseñan los puntos de extensión, **no se construyen**:

- Presentación telemática de modelos en la AEAT.
- **Laboral:** nóminas, seguros sociales, contratos.
- **Mercantil/societario:** actas, constituciones, depósito de cuentas (se modela la obligación, no el trámite).
- Conciliación bancaria (PSD2), firma electrónica de documentos, notificaciones DEHú, trámites de tráfico.
- Facturación electrónica B2B obligatoria (Ley 18/2022 "Crea y Crece"), pendiente de desarrollo reglamentario `[VERIFICAR]`.
- **Honorarios**: facturación de la gestoría a sus propios clientes. Candidata natural a fase 2 porque reutiliza M6 completo.
- Modo **NO VERI\*FACTU** (registros firmados sin envío inmediato): decisión documentada en `adr/ADR-00X`, no se implementa.

## 6. Restricciones que condicionan todo el diseño

Son restricciones **duras**; cualquier propuesta que las incumpla se rechaza.

1. **Stack cerrado por el cliente:** .NET 10, ASP.NET Core Razor Pages, EF Core (solo como ORM), SQL Server, migraciones por scripts SQL propios, Playwright para PDF, ASP.NET Core Identity. No se discute.
2. **Sin migraciones de EF Core.** Scripts SQL idempotentes numerados + runner al arranque con control de hash. Ver [13-migraciones-y-datos.md](13-migraciones-y-datos.md).
3. **Instancia única y RAM escasa.** El servidor de desarrollo tiene 3,8 GB **compartidos con otros diez proyectos y con SQL Server**; en el momento de escribir esto quedaban ~840 MB disponibles y el swap estaba al 84 %. Esto prohíbe Redis, brokers, cachés distribuidas y procesos auxiliares, y obliga a acotar Chromium (instancia única + cola de trabajos PDF).
4. **Sin cadena de build de Node en el servidor** salvo justificación en ADR. Ver [adr/ADR-003](adr/ADR-003-interactividad-razor-pages-y-css.md).
5. **Linux + nginx + Kestrel + systemd.** Un push a `master` despliega en desarrollo; producción se despliega a mano con script y verificación.
6. **Proyecto independiente.** Nombres de servicio, puerto, subdominio y base de datos propios, sin colisionar con los proyectos existentes del servidor.

## 7. Criterios de éxito de la demo

La demo está lista cuando, sin tocar código y con datos semilla, se puede:

1. Dar de alta un cliente con un perfil fiscal y ver **cómo aparecen solas** sus obligaciones del ejercicio en el Kanban y en el calendario.
2. Mover una tarjeta por todo el flujo hasta `Cerrado`, con su historial completo visible.
3. Entrar desde un móvil como cliente, ver *"te faltan N documentos"*, subir una foto de un ticket y verla llegar a la bandeja del gestor.
4. Aprobar un borrador de 303 como cliente y ver el cambio de estado y la fecha de conformidad en el lado gestoría.
5. Emitir una factura Veri\*Factu, obtener PDF con QR **en menos de 3 segundos**, y a continuación **tirar el simulador de la AEAT**, emitir tres facturas más, enseñar la cola de pendientes y ver cómo se vacía sola al restaurarlo.
6. Exportar un periodo a fichero contable y comprobar que una segunda exportación no duplica.
7. Ver el cuadro de mando del socio coherente con todo lo anterior.
8. Mostrar la pantalla de **declaración responsable** del sistema de facturación.

Presupuesto de rendimiento de la demo: arranque < 20 s, cualquier página < 500 ms, PDF < 3 s, consumo en reposo de la aplicación ≤ 350 MB sin contar Chromium.

## 8. Supuestos

- La gestoría es **encargada del tratamiento** de los datos de sus clientes; la plataforma es **subencargada**. Ver [09-seguridad-rgpd.md](09-seguridad-rgpd.md).
- Todos los datos de la demo son **ficticios**: NIF/CIF sintácticamente válidos pero inventados, nombres de empresa inventados.
- La demo funciona sobre territorio común y ejercicios **2026 y 2027**.
- El calendario fiscal se carga como **datos semilla por ejercicio**, nunca en código, y debe revalidarse cada año contra el calendario oficial del contribuyente de la AEAT `[VERIFICAR]`.

## 9. Riesgos y mejoras sugeridas

| # | Riesgo | Impacto | Mitigación propuesta |
|---|---|---|---|
| R1 | **RAM del servidor de desarrollo.** Chromium + SQL Server + diez proyectos en 3,8 GB con swap casi lleno | Alto: la demo puede morir por OOM justo al generar un PDF | Chromium con instancia única, `--single-process` descartado pero sí flags de memoria mínima, cola de 1 trabajo, `max server memory` de SQL Server limitado, `MemoryMax` en la unidad systemd; y **presupuesto de memoria por componente documentado** en `12-infraestructura-despliegue.md`. Recomendación firme: servidor propio para producción |
| R2 | **Datos normativos que caducan.** Plazos, campos del XSD, límites de envío y fechas de obligatoriedad cambian | Alto: una demo con un dato legal mal puesto pierde credibilidad ante un socio de gestoría | Todo dato normativo no verificado va marcado `[VERIFICAR]` con la fuente a consultar; ningún plazo en código; revisión anual del seed |
| R3 | **Alcance de la demo demasiado ancho.** M0–M7 es mucho para una demo | Medio | Orden de implementación por valor de demostración (Kanban antes que portal, portal antes que Veri\*Factu); cada fase debe dejar la demo *enseñable* por sí sola |
| R4 | **El motor de obligaciones es el corazón y es fácil hacerlo mal.** Si se codifica en C# en vez de en datos, cada cambio normativo es un despliegue | Alto a medio plazo | Reglas como filas en base de datos + tests de dominio con casos por tipo de cliente. Es el componente que más tests debe tener junto con la huella Veri\*Factu |
| R5 | **Sin migraciones de EF, el modelo C# y la base de datos pueden divergir** silenciosamente | Medio | Test de integración que compara el modelo EF con `INFORMATION_SCHEMA` y falla ante cualquier deriva. Ver ADR-001 §Consecuencias |
| R6 | La inalterabilidad de Veri\*Factu choca con el instinto de "arreglar" datos en la demo | Medio | Triggers `INSTEAD OF UPDATE, DELETE` desde el primer script SQL, y un botón de *reset* de datos de demo que **recrea** en vez de editar |

### Mejoras sugeridas más allá del encargo

- **Plantillas de perfil fiscal** ("autónomo con local y un empleado") para que el alta de cliente sea un clic: acelera el alta masiva al migrar una cartera entera, que es el momento de la verdad comercial.
- **Simulación de carga de trabajo**: *"si acepto 20 clientes nuevos, ¿qué le pasa a mi abril?"*. Diferenciador real para el socio y barato sobre el modelo de datos propuesto.
- **Modo "tour guiado"** en la demo: un botón que rellene el escenario y cuente la historia, para que el comercial no dependa de su memoria.
