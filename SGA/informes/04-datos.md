# 04 · Datos (fase 4)

## Modelo
`Sga.Nucleo/Modelo`: Gestor, Cliente (tipo, quién emite, forma de pago, empleados, banco, honorarios), Actividad (IAE, principal), ContratoAlquiler, Obligacion (celda de la matriz: cliente × modelo × periodo, con estado, gestor, resultado y lo liquidado), Presentacion (justificante), FacturaEmitida, FacturaRecibida (con categoría del libro de gastos y actividad), Documento, MovimientoBancario, Incidencia y Mensaje, FacturaHonorarios, Aviso. Esquema creado por EF Core (`docs/decisiones.md`, D2).

## Datos ficticios y su historia
Tres gestores y catorce clientes, todos inventados (nombres, NIF válidos pero falsos, IBAN con dígitos de control correctos, direcciones ficticias):

| Perfil | Clientes | Historia en la demo |
|---|---|---|
| Arrendadores (3) | Ernesto Valdés (1 local, 1.850 €/mes), Carmen Ruiz (2 locales), Joaquín Peña (1 oficina) | Ernesto: 303 del 3T ya presentado. Carmen: facturas del 3T sin generar (para enseñar «Generar bloque»). Joaquín: la de septiembre generada y sin enviar. |
| Profesionales (6) | Nuria Campos (traductora, 2 actividades, le factura SGA), Álvaro Sanz (fotógrafo), Pilar Domínguez (arquitecta con empleada y despacho alquilado), Marcos Leal (diseñador, factura él), Beatriz Núñez (abogada, factura ella), Tomás Iglesias (electricista, factura él) | Nuria: le faltan un ticket, un recibo y el extracto. Marcos: hueco (2026-017) y duplicada (2026-021). Tomás: IVA alto por una obra, fraccionamiento solicitado. Beatriz y Pilar: casi todo presentado. |
| Sociedades (5) | Luz Norte Producciones (audiovisual, clave de consulta del banco), Panadería Los Robles, Ferretería Aguilar (operaciones con la UE), Nexo Consultores (alquila oficina), Distribuciones Vega y Soto | Luz Norte: extracto con siete movimientos sin cuadrar. Panadería: faltan nóminas. Ferretería: trimestre cerrado. Vega y Soto: sin extracto. |

Línea de tiempo de 2026: 1T y 2T presentados con justificante; 3T en campaña (6 de octubre) con presentados, en curso y pendientes; 4T futuro; anuales del ejercicio 2025 presentados a lo largo del año; pagos fraccionados 1P presentado, 2P en campaña, 3P futuro. 327 facturas emitidas, 673 recibidas, 449 obligaciones, 5 incidencias, avisos.

Los resultados del 303 de 1T y 2T se calculan con el mismo motor que la mesa de IVA sobre las facturas sembradas, así que los números cuadran entre sí.

## Regla de oro
Ningún dato de `Recursos/` entra en la demo. Los importes de referencia (2.000 € de alquiler, 447.800 € de base de la sociedad) solo viven en los tests como oráculos anonimizados; los datos de la demo usan otros importes con las mismas mecánicas.

## Cómo reiniciar
`Demo:Reiniciar = true` (o `sga.sh reset` en desarrollo, `deploy/sga.sh reiniciar-datos` en el servidor) vacía las tablas y vuelve a sembrar. No borra la base (ver DUDAS S-01).
