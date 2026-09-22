# Índice de documentación

> Estado de los entregables de arquitectura. **Actualizado:** 2026-09-22
> El punto de entrada del Agente Programador es `HANDOFF-PROGRAMADOR.md` (raíz del repositorio), **no** este índice.

| Fichero | Contenido | Estado |
|---|---|---|
| [00-vision-y-alcance.md](00-vision-y-alcance.md) | Qué es, para quién, alcance de la demo, restricciones, criterios de éxito | ✅ v1.0 |
| [01-mapa-dominio.md](01-mapa-dominio.md) | Actores, tipos de cliente, ciclo de trabajo, invariantes, glosario | ✅ v1.0 |
| [decisiones-abiertas.md](decisiones-abiertas.md) | Registro de decisiones pendientes y recomendaciones por defecto | ✅ vivo |
| [adr/ADR-001-…](adr/ADR-001-arquitectura-general-y-estructura-de-solucion.md) | Arquitectura general y estructura de solución | ✅ Aceptada |
| [adr/ADR-002-…](adr/ADR-002-certificado-y-representacion-verifactu.md) | Certificado y representación para el envío Veri\*Factu | ✅ Aceptada (validación legal pendiente) |
| [adr/ADR-003-…](adr/ADR-003-interactividad-razor-pages-y-css.md) | htmx + SortableJS y estrategia de CSS | ✅ Aceptada |
| [04-modelo-datos.md](04-modelo-datos.md) | Entidades, ERD, invariantes, índices | ✅ v1.0 |
| [06-verifactu/especificacion.md](06-verifactu/especificacion.md) | Registros de alta y anulación, tipos de factura, inalterabilidad | ✅ v1.0 |
| [06-verifactu/huella-y-vectores-prueba.md](06-verifactu/huella-y-vectores-prueba.md) | Cadena de huella SHA-256 y **vectores de prueba verificados** | ✅ v1.0 |
| [06-verifactu/envio-y-cola-reintentos.md](06-verifactu/envio-y-cola-reintentos.md) | Envío SOAP/mTLS, outbox, estados, simulador | ✅ v1.0 |
| [06-verifactu/qr-y-pdf.md](06-verifactu/qr-y-pdf.md) | QR normalizado, plantilla PDF, cola de Playwright | ✅ v1.0 |
| [06-verifactu/declaracion-responsable.md](06-verifactu/declaracion-responsable.md) | Contenido y versionado de la declaración | ✅ v1.0 |
| 02-calendario-fiscal.md | Modelo de plazos + datos semilla por ejercicio | ⏳ **siguiente** |
| 03-arquitectura.md | Diagramas C4 en Mermaid | ⏳ pendiente |
| 05-paginas-y-endpoints.md | Inventario de Razor Pages y endpoints | ⏳ pendiente |
| 07-integraciones-contables.md | `IExportadorContable` y formato elegido | ⏳ pendiente |
| 08-ux-ui/design-system.md | Paleta, tipografía, componentes, tono | ⏳ pendiente |
| 08-ux-ui/pantallas.md | Inventario de pantallas por rol con wireframes | ⏳ pendiente |
| 08-ux-ui/flujos-usuario.md | Flujos completos por rol | ⏳ pendiente |
| 09-seguridad-rgpd.md | Encargado/subencargado, cifrado, retención, auditoría | ⏳ pendiente |
| 10-argumentario-comercial.md | Valor por módulo para el socio | ⏳ pendiente |
| 11-guion-demo.md | Historia de 15 minutos | ⏳ pendiente |
| 12-infraestructura-despliegue.md | systemd, nginx, puertos, secretos, despliegue | ⏳ pendiente |
| 13-migraciones-y-datos.md | Convenciones SQL, runner, alineación con EF, seeds | ⏳ **siguiente** |

## Convenciones de esta documentación

- **`[VERIFICAR]`** marca todo dato normativo que no he podido confirmar contra fuente oficial. Va siempre acompañado de dónde comprobarlo. **Ningún `[VERIFICAR]` puede llegar a producción sin resolver.**
- Las **reglas de dominio** se citan como `RD-nn` y viven en `01-mapa-dominio.md` §5.
- Las **decisiones abiertas** se citan como `DA-nn` y viven en `decisiones-abiertas.md`.
- Toda decisión relevante se argumenta en un **ADR**; si se cambia, el ADR anterior pasa a estado *Sustituido* y apunta al nuevo.
- Todos los datos de ejemplo son **ficticios**.
