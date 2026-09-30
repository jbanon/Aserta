# 06 · Área del gestor (fase 6)

## Qué hice
Capturas `gestor-*`. Todo en móvil (consultar, resolver, aprobar) y escritorio (la mesa completa).

- **Hoy:** anillo del avance del trimestre, mis modelos sin presentar, incidencias, documentos por revisar; «hoy: qué me toca» (modelos por vencimiento, incidencias, documentos), próximos vencimientos y reparto del trimestre por persona.
- **Matriz de control:** sustituye al Excel. Un cliente por fila; columnas IVA (303, 349), IRPF (130), retenciones (111, 115, 123), Sociedades (202), contabilidad (Libros, CC.AA.); filtros por trimestre, persona, tipo de cliente y modelo; barra de avance; leyenda; celdas con los mismos estados (NP gris, pendiente blanco, en curso amarillo con las iniciales de quien lo lleva, presentado verde con la fecha, trimestre futuro rayado); columna de documentación y de avance. Cada celda lleva a la ficha.
- **Ficha de cliente:** tipo, actividades (IAE) con la principal, modelos que aplican y NP, quién emite, forma de pago, empleados, banco; pestañas Trimestre (modelos con «Lo cojo yo» / «Presentar» / justificante, IVA según libros, documentación con «Reclamar lo que falta», incidencias), Documentos (con «Revisado»), Facturas, Presentaciones.
- **Mesa de cálculo de IVA:** la hoja LIQ IVA: según libros, menos lo liquidado en trimestres anteriores, a liquidar; desglose por tipo; resultado con «Presentar 303»; libros de emitidas y recibidas con sumas; **Exportar a Monitor (CSV)**; **Importar un Excel** (lee de verdad el `.xlsx` y cuenta filas por hoja; no sustituye los libros).
- **Facturas de alquiler:** por arrendador, las facturas del trimestre con estado; **Generar bloque** del trimestre (una por contrato y mes que falte, numeración correlativa, «en Monitor»); **Enviar** (simulado, con aviso al cliente); PDF con el aspecto de la factura real.
- **Libro de gastos por actividad:** una tabla por IAE con las columnas del Excel; las facturas comunes se imputan a la actividad principal y se marcan.
- **Comprobación de numeración:** huecos, duplicados y fechas fuera de orden; «Avisar al cliente» abre la incidencia con el detalle.
- **Conciliación bancaria:** movimientos contra facturas; sugerencia por número en el concepto o por importe y fecha; conciliar, ignorar, «Preguntar al cliente».
- **Presentar (simulado):** pantalla de confirmación con las casillas del 303, aviso claro de simulación, y justificante PDF con expediente, CSV, número de justificante y presentador «Colaborador», como el real.
- **Incidencias:** lista (abiertas, las mías, todas), detalle con conversación, responder, resolver, conceder o denegar el fraccionamiento (con aviso al cliente).
- **Calendario:** mes con fin de plazo, último día de domiciliación y festivos; próximos 90 días con clientes afectados. Todos los plazos llevan `[VERIFICAR]`.

## Decisiones
- Estados de la matriz: cuatro (NP, pendiente, en curso, presentado). El «nombre en la celda» es la persona asignada, no la que lo terminó (S-02).
- La columna 130 se añade al Excel real porque los apuntes la mencionan para profesionales (S-14).
- Los anuales de 2025 presentados durante 2026 aparecen en la hoja de 2026 (S-15).
- Nada se envía a Hacienda: el justificante lo dice en rojo.

## Cómo probarlo
Guion completo en `docs/guion-demo.md` (recorrido de 4 minutos como Lucía).
