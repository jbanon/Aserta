# 07 · Cierre (fase 7)

## Qué hay
Una demo completa y enseñable en `http://127.0.0.1:5120` de este servidor (pendiente de subdominio y servicio, ver más abajo): web pública de seis páginas, área del cliente y área del gestor en móvil y escritorio, con catorce clientes ficticios y una historia de 2026 en plena campaña del tercer trimestre. Guion de 10 minutos en `docs/guion-demo.md`.

## Pruebas
| Qué | Resultado |
|---|---|
| `Sga.Tests` (6): IVA acumulado del arrendador en 3T (3.780 − 1.260 − 1.260; 21,75 − 7,25 − 7,25; 1.252,75), el 303 del 2T (base 6.000, cuota 1.260, deducible 34,50 / 7,25, resultado 1.252,75), resumen de la sociedad en 2T (27.260,10 − 11.884,95; 6.063,75 − 3.891,62; 13.203,02), retención de la factura (2.000 + 21 % − 19 % = 2.040), numeración (huecos, duplicados, fechas) y traslado de plazos por día inhábil | verde |
| Recorrido HTTP automático (`sga_flow.py`): 6 clientes y 3 gestores, 90 páginas, PDF de factura y de justificante, CSV, generar bloque, enviar, sin excepciones en el log | verde, 0 fallos |
| Aserta: `Aserta.Dominio.Tests` (75) y `Aserta.Verifactu.Tests` (29), y compilación de `Aserta.Web` tras el cambio del fichero de paquetes | verde |
| Capturas de Playwright a 390×844 y 1440×900 (33 en `informes/capturas/`), revisadas una a una; se corrigieron cuatro desbordes horizontales (matriz, libro de gastos, ficha en móvil, calendario en móvil), el contraste del anillo sobre fondo oscuro, el botón de la cabecera de la web, la alineación de los botones de la mesa de IVA, el símbolo del euro en los PDF y la frase «te faltan…» | hecho |
| Accesibilidad básica: foco visible (3 px), etiquetas en todos los campos, botones de 44 px, `aria-current` en navegación, `aria-live` para anuncios, contraste AA en los pares de color usados | hecho por construcción; sin auditoría externa |

## Cómo se mide contra los criterios del encargo
1. **Un desconocido entiende qué hace SGA:** la portada dice para quién y cómo en la primera pantalla; los perfiles llevan sus modelos explicados.
2. **El arrendador sube un ticket, ve su IVA y descarga una factura desde el móvil en menos de un minuto:** Subir (3 toques), Impuestos (1 toque), Facturas → PDF (2 toques).
3. **El gestor ve de un vistazo qué le falta a cada cliente y llega al cálculo desde una celda:** matriz con columna de documentación; celda → ficha → «Mesa de IVA».
4. **Los números coinciden con los Excel:** pruebas con los oráculos del encargo; el motor es el mismo para el cliente, el gestor y el justificante.
5. **Nadie reconoce un dato suyo, pero sí su forma de trabajar:** nombres, NIF, IBAN, direcciones e importes inventados; matriz, LIQ IVA, libros con las mismas columnas, factura con la misma composición, justificante con los mismos campos.
6. **Las cuatro vistas se ven bien:** capturas en `informes/capturas/`.

## Qué queda pendiente o dudoso (todo en `DUDAS.md`)
- **Base de datos:** la demo corre sobre SQLite porque mi reinicio borró la base `SGA` de SQL Server (S-01). Para volver a SQL Server hace falta recrearla; el código ya soporta los dos proveedores.
- **Despliegue:** DNS, servicio, nginx y certificado (S-21). Ficheros listos en `deploy/`.
- **Datos normativos:** todos los plazos y varios tipos (retención del 15 %, umbral de fraccionamiento, casillas, epígrafes IAE) están marcados `[VERIFICAR]`.
- **Textos de la web:** precios, cifras de confianza, testimonios y equipo son inventados y deben sustituirse.
- **Opcionales no hechos:** bloque «Veri*Factu listo», avisos por correo simulados, modo oscuro (S-22). Los tres son incrementales sobre lo que hay.
- **Importar Excel:** se lee el fichero, no se mapea a los libros (S-17).

## Cómo arrancarla mañana
```
cd ~/proyectos/Aserta/SGA/src/Sga.Web && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5120 dotnet run
```
y abrir `/acceso`. Para dejar los datos como al principio: variable de entorno `Demo__Reiniciar=true` en un arranque.
