# Dudas y supuestos de la demo SGA

Cada duda lleva el supuesto que he aplicado para no parar. Javier o SGA pueden cambiarlo; digo dónde afecta.

| # | Duda | Supuesto aplicado | Afecta a | Estado |
|---|---|---|---|---|
| S-01 | **Base de datos `SGA` borrada por mí.** Al recrear los datos, el reinicio de EF Core (`EnsureDeleted`) eliminó la base `SGA` que Javier había creado, y `agente_ro` no tiene permiso para crearla. Lo he corregido: el reinicio ahora solo vacía tablas. | La demo corre sobre **SQLite** (`SGA/sga-demo.db`, `Datos:Proveedor = Sqlite`), sin pérdida funcional. Para volver a SQL Server: `CREATE DATABASE SGA` + `db_owner` a `agente_ro` y `Datos:Proveedor = SqlServer`. | Base de datos, despliegue | **Necesita acción de Javier** (si se quiere SQL Server) |
| S-02 | Significado de las celdas del Excel de control: vacío, `NP`, nombre. Cómo marcan «presentado». | Vacío = pendiente; nombre = quien lo lleva (en curso); presentado = verde con fecha; NP = no procede. | Matriz | Confirmar con SGA |
| S-03 | «5 documentos» del arrendador. | Tres facturas de alquiler del trimestre, la factura de SGA y el justificante del 303. En el área del arrendador no se le pide nada. | Área del cliente (arrendador) | Confirmar |
| S-04 | Facturas de SGA por honorarios: periodicidad e importes. | Arrendadores trimestral (45 €), profesionales y sociedades mensual (50–210 €). Solo se muestran. | Facturas de SGA | Cifras inventadas |
| S-05 | Monitor. | Solo se nombra: marca «en Monitor» en las facturas y «Exportar a Monitor (CSV)» en la mesa de IVA. | Mesa de IVA, alquileres | Confirmar si interesa integración |
| S-06 | Umbral para ofrecer el fraccionamiento del pago. | 1.000 € (`ServicioIva.UmbralFraccionamiento`), marcado `[VERIFICAR]`. El trámite real ante Hacienda no se simula: solo la decisión de SGA. | Cuánto voy a pagar, incidencias | Confirmar |
| S-07 | Formato del número de las facturas de alquiler. | «26/8» (año/mes), como la factura real; «26/8-2» si hubiera dos en el mes. Las facturas de profesionales a los que factura SGA usan «26/001». | Emisión | Confirmar |
| S-08 | Plazos de presentación y domiciliación. | Copiados del catálogo de Aserta (patrones día 20, 30 de enero, etc.) con traslado por festivo; **ninguno confirmado** contra la AEAT. Todos con la marca «pendiente de confirmar». | Calendario, línea de tiempo, avisos | `[VERIFICAR]` |
| S-09 | Retención del 15 % en las facturas de profesionales a empresas (traductora, fotógrafo, arquitecta, abogada). | Aplicada con la marca `[VERIFICAR]` en el código; el arrendador lleva el 19 % de la factura real. | Datos ficticios | `[VERIFICAR]` |
| S-10 | Epígrafes IAE de los clientes ficticios (861.2 alquiler de locales, 774 traductores, 973.1 fotografía, 411 arquitectos, 751 publicidad, 731 abogados, 504.1 instalaciones eléctricas, 961.1 audiovisual, 419.1 panadería, 653.3 ferretería, 843 consultoría, 612.9 comercio mayor). | Elegidos como ejemplo verosímil, sin contrastar con el listado oficial. | Ficha de cliente | `[VERIFICAR]` |
| S-11 | Casillas del 303 en el resumen del justificante (07, 09, 27, 28, 29, 45, 46, 71). | Tomadas del justificante de referencia; solo se rellena el tipo general. | Justificante PDF, presentar | `[VERIFICAR]` |
| S-12 | Precios «orientativos» de la web (45 €/trimestre, 50 €/mes, 150 €/mes) y datos de confianza («desde 2011», «respuesta en 48 h», «colaboradores sociales»). | Inventados para que la web tenga la estructura que convence (ver informe 01); SGA debe sustituirlos. | Web pública | Sustituir antes de publicar |
| S-13 | Testimonios y equipo (Marta Serrano Gil, Lucía Ferrer Campos, Raúl Ibáñez Mora). | Ficticios; sustituyen a los nombres reales de los apuntes y del justificante. | Web, área | Sustituir |
| S-14 | Modelo 130 en la matriz. | El Excel real no tiene columna 130; la he añadido (grupo IRPF) porque los apuntes dicen que algunos profesionales lo presentan. | Matriz | Confirmar |
| S-15 | Los anuales (Libros, CC.AA., 200, 390, 347, 190) en la hoja del año en curso. | Se muestran los del ejercicio anterior presentados durante el año, como parece hacer el Excel («CONTABILIDAD: Libros, CC.AA.» en la hoja 2026). | Matriz, ficha | Confirmar |
| S-16 | Clave de consulta del banco de las sociedades. | Solo se indica que existe y la fecha de la última descarga; la clave nunca se guarda ni se simula la conexión. El extracto de la demo está precargado. | Conciliación | Confirmar el alcance deseado |
| S-17 | Importación de un Excel de cálculo en la mesa de IVA. | Se lee el fichero de verdad (hojas y filas con números) pero no se mapea a los libros: el cálculo sigue usando los datos registrados. | Mesa de IVA | Decidir si se implementa el mapeo |
| S-18 | Documentos «esperados» por trimestre (3 facturas, 3 tickets, 3 recibos, 1 extracto…). | Regla fija por tipo de cliente en `RequisitosDocumentales`; en producto sería configurable por cliente. | «Te faltan…» | Confirmar |
| S-19 | Cuota de autónomos y seguro: sin IVA, deducibles en IRPF. | Así se cargan (tipo 0 %, categoría Seguridad Social / servicios exteriores). | Libro de gastos | `[VERIFICAR]` |
| S-20 | Web pública: mapa. | Plano esquemático dibujado en SVG (sin servicios externos, por la política de seguridad de contenidos). En la web real iría un mapa. | Contacto | Decidir |
| S-21 | Despliegue: subdominio `sga.winsoft.es`, servicio `sga-demo` en el puerto 5120. | Ficheros en `SGA/deploy/`; DNS, systemd, nginx y certificado los tiene que instalar Javier (sin sudo no puedo). Mientras tanto la demo corre en `127.0.0.1:5120` en este servidor. | Despliegue | **Necesita acción de Javier** |
| S-22 | Bloque opcional «Veri*Factu listo», avisos por correo y modo oscuro. | No hechos: el tiempo se ha ido en las cuatro vistas y los cálculos. Explicado en el informe 07. | Opcionales | Abierto |

## Encargo 02 (fotos y vídeo)

| # | Duda | Supuesto aplicado | Afecta a | Estado |
|---|---|---|---|---|
| S-23 | Fotos de stock (Pexels) en la web pública. | Tratadas con velo de marca y sin describirlas como oficina o equipo de SGA. Los originales quedan en `Recursos/`, fuera de git; en el repositorio solo los WebP recortados. | Web pública | **Aceptado por Javier** (2026-09-30) |
| S-24 | El servicio systemd `sga-demo` quedó parado por un `pkill` mío; sin sudo no puedo arrancarlo. He publicado la versión nueva y la sirvo a mano en el 5120 con la misma configuración. | Volver al servicio: `pkill -f publicado/Sga.Web.dll && sudo systemctl start sga-demo`. | Despliegue | **Necesita acción de Javier** |
| S-25 | Vídeo explicativo de 526 KB en el repositorio (supera por poco los «cientos de KB»). | Se sirve con `preload="none"`. Alternativas: recortar a 12 s o dejarlo fuera de git. | Peso del repositorio | **Aceptado por Javier**: se queda como está |
| S-26 | Sin `.mp4`: el ffmpeg de Playwright solo codifica VP8 (WebM). | WebM lo reproducen los navegadores actuales, incluido Safari 16+. | Vídeo | Abierto |
| S-27 | Cifras de la franja bajo el hero (48 h, 21/44, 15 años, 14 clientes). | Ilustrativas y marcadas como tales en el pie. | Web pública | **Aceptado por Javier** para la demo; sustituir solo si la web pasa a ser real |
| S-28 | Teléfono y WhatsApp de la barra móvil apuntan al número ficticio de `appsettings`. | Cambiar en `Sga:Telefono`. | Web pública | **Aceptado por Javier** para la demo |
