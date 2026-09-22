# 06.2 · Huella («hash») encadenada y vectores de prueba

> **Estado:** vigente · **Versión:** 1.0 · **Fecha:** 2026-09-22
> **Fuente oficial:** AEAT, *«Detalle de las especificaciones técnicas para generación de la huella o hash de los registros de facturación»*, **versión 0.1.2, de 27/08/2024**.
> PDF: `https://www.agenciatributaria.es/static_files/AEAT_Desarrolladores/EEDD/IVA/VERI-FACTU/Veri-Factu_especificaciones_huella_hash_registros.pdf`
> Página de la sede: `https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/informacion-tecnica/algoritmo-calculo-codificacion-huella-hash.html`
>
> **Los tres vectores de prueba de §5 han sido verificados computacionalmente el 2026-09-22 y reproducen exactamente los hashes publicados por la AEAT.** Son, por tanto, datos comprobados y no `[VERIFICAR]`.
> `[VERIFICAR]` antes de implementar: que la versión 0.1.2 siga siendo la vigente. Es lo único de este documento que puede haber cambiado.

---

## 1. Qué es y por qué importa

Cada registro de facturación lleva una **huella** (hash SHA-256) calculada sobre unos campos concretos del propio registro **más la huella del registro anterior del mismo emisor**. Eso convierte la secuencia de facturas de un emisor en una cadena: alterar una factura del pasado obligaría a recalcular todas las posteriores, y la AEAT ya tiene las originales.

Para el producto esto significa dos cosas:

1. **La huella es la función más crítica del sistema.** Un error de un carácter en el formato produce hashes válidos sintácticamente pero rechazados por la AEAT — y peor: puede pasar desapercibido durante semanas. Por eso esta página existe y por eso trae vectores de prueba.
2. **La cadena es por NIF emisor**, no global de la plataforma. Cada cliente de cada gestoría tiene su propia cadena independiente.

## 2. Algoritmo

**SHA-256**, único algoritmo permitido en la fecha de publicación de la especificación (Lista L12 del anexo de la orden).

**Salida:** hexadecimal, **en mayúsculas**, **64 caracteres**.

En .NET:

```csharp
var bytes = Encoding.UTF8.GetBytes(cadena);          // UTF-8, sin BOM
var hash  = Convert.ToHexString(SHA256.HashData(bytes)); // ToHexString ya devuelve MAYÚSCULAS
```

> `Convert.ToHexString` devuelve mayúsculas sin conversión adicional. **No usar** `BitConverter.ToString(...).Replace("-","")`, que devuelve mayúsculas pero es más lento y más fácil de estropear, ni `.ToLower()` en ningún punto.

## 3. Construcción de la cadena de entrada

Los campos se concatenan **en el orden indicado**, con esta estructura literal:

```
nombreCampo1=valor1&nombreCampo2=valor2&...&nombreCampoN=valorN
```

Reglas exactas, tomadas de la especificación:

| Regla | Detalle |
|---|---|
| Nombre del campo | Constante, tal y como se describe en el XML del diseño de registro. Respetar mayúsculas y minúsculas |
| Separador | `&` entre pares. `=` entre nombre y valor. **Sin espacios** |
| Valor | El mismo contenido que el campo del XML, **eliminando los espacios al inicio y al final** (`Trim`) |
| Campos numéricos | Se admiten indistintamente **una o dos posiciones decimales**; los ceros a la derecha no son relevantes. `123.1` y `123.10` son ambos válidos y producen un hash válido *(la AEAT normaliza)* |
| Campo ausente o vacío | Se escribe **el nombre del campo y el `=`, sin valor**. Ejemplo del primer registro: `...&Huella=&FechaHoraHusoGenRegistro=...` |
| Codificación | La cadena resultante se codifica en **UTF-8** antes de hashear |
| Separador decimal | Punto (`.`). **Nunca** coma — es el error clásico de una aplicación .NET corriendo con cultura `es-ES` |

> **Regla de implementación obligatoria:** toda conversión de número o fecha a texto en este módulo usa `CultureInfo.InvariantCulture` de forma explícita. Un despliegue en un servidor con locale español y un `decimal.ToString()` sin cultura rompe **todas** las huellas y el síntoma es "la AEAT nos acepta con errores", que nadie relaciona con el locale. Es exactamente el tipo de fallo que este documento existe para prevenir.

### 3.1 Campos del **registro de alta**, en orden

| # | Campo | Ruta en el XML |
|---|---|---|
| 1 | `IDEmisorFactura` | `RegistroAlta/IDFactura/IDEmisorFactura` |
| 2 | `NumSerieFactura` | `RegistroAlta/IDFactura/NumSerieFactura` |
| 3 | `FechaExpedicionFactura` | `RegistroAlta/IDFactura/FechaExpedicionFactura` |
| 4 | `TipoFactura` | `RegistroAlta/TipoFactura` |
| 5 | `CuotaTotal` | `RegistroAlta/CuotaTotal` |
| 6 | `ImporteTotal` | `RegistroAlta/ImporteTotal` |
| 7 | `Huella` | `RegistroAlta/Encadenamiento/RegistroAnterior/Huella` — **la del registro anterior** |
| 8 | `FechaHoraHusoGenRegistro` | `RegistroAlta/FechaHoraHusoGenRegistro` |

### 3.2 Campos del **registro de anulación**, en orden

| # | Campo | Ruta en el XML |
|---|---|---|
| 1 | `IDEmisorFacturaAnulada` | `RegistroAnulacion/IDFactura/IDEmisorFacturaAnulada` |
| 2 | `NumSerieFacturaAnulada` | `RegistroAnulacion/IDFactura/NumSerieFacturaAnulada` |
| 3 | `FechaExpedicionFacturaAnulada` | `RegistroAnulacion/IDFactura/FechaExpedicionFacturaAnulada` |
| 4 | `Huella` | `RegistroAnulacion/Encadenamiento/RegistroAnterior/Huella` |
| 5 | `FechaHoraHusoGenRegistro` | `RegistroAnulacion/FechaHoraHusoGenRegistro` |

> Obsérvese que el registro de anulación **no** incluye importes ni tipo de factura. Son cinco campos, no ocho.

### 3.3 Registros de evento

La especificación define un tercer subconjunto para **registros de evento** (`RegistroEvento`), con 9 campos: `NIF`, `ID`, `IdSistemaInformatico`, `Version`, `NumeroInstalacion`, `NIF` del obligado, `TipoEvento`, `HuellaEvento` del evento anterior y `FechaHoraHusoGenEvento`.

**Fuera del alcance de la demo:** los registros de evento pertenecen al modo **NO VERI\*FACTU**, que no implementamos (DA-11). Se documenta aquí para que quede constancia de que existe y de dónde sacarlo si algún día se añade.

### 3.4 Dónde se escribe la huella calculada

| Tipo de registro | Campo destino |
|---|---|
| Alta | `RegistroAlta/Huella` |
| Anulación | `RegistroAnulacion/Huella` |
| Evento | `RegistroEvento/Evento/HuellaEvento` |

## 4. El primer registro de la cadena

Cuando es el primer registro de facturación del sistema para ese emisor:

- se indica `PrimerRegistro = "S"` (`PrimerEvento` en los registros de evento);
- **no** es necesario informar el bloque `RegistroAnterior`;
- en la cadena de la huella, el campo `Huella` aparece igualmente, **con el nombre y el `=` y sin valor**: `...&Huella=&FechaHoraHusoGen...`;
- **sí es obligatorio** calcular e informar la huella del propio registro. Es un error frecuente pensar que el primer registro no lleva huella: lleva la suya, lo que no lleva es huella anterior.

## 5. Vectores de prueba  ✅ verificados

> Estos tres casos son los ejemplos oficiales de la especificación (§6.1, 6.2 y 6.3 del PDF de la AEAT). **Se han recalculado con SHA-256 sobre UTF-8 y coinciden exactamente con los valores publicados.** Deben entrar tal cual en `Aserta.Verifactu.Tests` como test de referencia. Si uno de ellos falla, la implementación está mal y **no se despliega**.

### Caso 1 — Alta, primer registro del SIF

Datos de entrada:

| Campo | Valor |
|---|---|
| `IDEmisorFactura` | `89890001K` |
| `NumSerieFactura` | `12345678/G33` |
| `FechaExpedicionFactura` | `01-01-2024` |
| `TipoFactura` | `F1` |
| `CuotaTotal` | `12.35` |
| `ImporteTotal` | `123.45` |
| `Huella` | *(sin contenido: no hay registro anterior)* |
| `FechaHoraHusoGenRegistro` | `2024-01-01T19:20:30+01:00` |

Cadena de entrada (199 caracteres, en una sola línea sin saltos):

```
IDEmisorFactura=89890001K&NumSerieFactura=12345678/G33&FechaExpedicionFactura=01-01-2024&TipoFactura=F1&CuotaTotal=12.35&ImporteTotal=123.45&Huella=&FechaHoraHusoGenRegistro=2024-01-01T19:20:30+01:00
```

Huella esperada:

```
3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60
```

### Caso 2 — Alta, segundo registro (encadenado)

| Campo | Valor |
|---|---|
| `IDEmisorFactura` | `89890001K` |
| `NumSerieFactura` | `12345679/G34` |
| `FechaExpedicionFactura` | `01-01-2024` |
| `TipoFactura` | `F1` |
| `CuotaTotal` | `12.35` |
| `ImporteTotal` | `123.45` |
| `Huella` | `3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60` |
| `FechaHoraHusoGenRegistro` | `2024-01-01T19:20:35+01:00` |

Cadena de entrada (263 caracteres):

```
IDEmisorFactura=89890001K&NumSerieFactura=12345679/G34&FechaExpedicionFactura=01-01-2024&TipoFactura=F1&CuotaTotal=12.35&ImporteTotal=123.45&Huella=3C464DAF61ACB827C65FDA19F352A4E3BDC2C640E9E9FC4CC058073F38F12F60&FechaHoraHusoGenRegistro=2024-01-01T19:20:35+01:00
```

Huella esperada:

```
F7B94CFD8924EDFF273501B01EE5153E4CE8F259766F88CF6ACB8935802A2B97
```

### Caso 3 — Anulación, encadenada

| Campo | Valor |
|---|---|
| `IDEmisorFacturaAnulada` | `89890001K` |
| `NumSerieFacturaAnulada` | `12345679/G34` |
| `FechaExpedicionFacturaAnulada` | `01-01-2024` |
| `Huella` | `F7B94CFD8924EDFF273501B01EE5153E4CE8F259766F88CF6ACB8935802A2B97` |
| `FechaHoraHusoGenRegistro` | `2024-01-01T19:20:40+01:00` |

Cadena de entrada (232 caracteres):

```
IDEmisorFacturaAnulada=89890001K&NumSerieFacturaAnulada=12345679/G34&FechaExpedicionFacturaAnulada=01-01-2024&Huella=F7B94CFD8924EDFF273501B01EE5153E4CE8F259766F88CF6ACB8935802A2B97&FechaHoraHusoGenRegistro=2024-01-01T19:20:40+01:00
```

Huella esperada:

```
177547C0D57AC74748561D054A9CEC14B4C4EA23D1BEFD6F2E69E3A388F90C68
```

### 5.1 Observaciones aprovechables de los vectores

- **La fecha va en formato `DD-MM-AAAA`** (`01-01-2024`), no ISO. En cambio `FechaHoraHusoGenRegistro` sí es ISO 8601 con huso (`2024-01-01T19:20:30+01:00`). Son dos formatos distintos en la misma cadena: es el error número uno de una primera implementación.
- **El `NumSerieFactura` puede contener `/`** y otros caracteres especiales, y **no se escapa** en la cadena de la huella (a diferencia de la URL del QR, donde sí se codifica). Otro error clásico: reutilizar la cadena codificada del QR para el hash.
- El huso horario del ejemplo es `+01:00`; en verano España es `+02:00`. **El desfase se escribe, no se normaliza a UTC.**

## 6. Cómo se implementa aquí

### 6.1 Contrato

```csharp
// Aserta.Verifactu — sin dependencias de EF, HTTP ni ASP.NET
string ConstruirCadenaAlta(DatosHuellaAlta datos);        // devuelve la cadena literal
string ConstruirCadenaAnulacion(DatosHuellaAnulacion d);
string CalcularHuella(string cadena);                      // SHA-256 → 64 hex mayúsculas
```

Separar la construcción de la cadena del cálculo del hash no es purismo: permite que los tests comparen **la cadena** además del hash, y cuando algo falla el mensaje de error dice qué carácter sobra en vez de "el hash no coincide".

### 6.2 Persistencia de la prueba

`vf.RegistroFacturacion` guarda `CadenaHuella` (el texto exacto que se hasheó) y `FechaHoraHusoGenRegistroTexto` (el literal ISO con huso). Ver [04-modelo-datos.md](../04-modelo-datos.md) §6.3. Esto permite **reverificar la cadena completa de un emisor** años después con un script de tres líneas, sin depender de que el formateo actual siga siendo idéntico.

### 6.3 Concurrencia (RD-05)

La huella anterior se lee de `vf.CadenaEmisor` con `WITH (UPDLOCK, HOLDLOCK)` dentro de la transacción de emisión, y se actualiza antes de confirmarla. Dos emisiones simultáneas del mismo NIF se serializan; de NIF distintos, no compiten. Nivel de aislamiento: `READ COMMITTED` (el bloqueo explícito ya aporta lo necesario y evita el coste de `SERIALIZABLE`). La fila del emisor se crea al dar de alta su primera serie de facturación, **nunca dentro de la transacción de emisión**.

### 6.4 Validación propia

La especificación es explícita: si en una remisión VERI\*FACTU **la huella no coincide con el cálculo de la AEAT, el registro se marca «Aceptado con errores»**. Es decir, el error no rechaza la factura pero queda registrado como defectuoso. Consecuencia de producto: el cuadro de mando del socio debe mostrar los «aceptados con errores» **en rojo y no en ámbar**, porque significan que algo estructural está mal en nuestra implementación y va a afectar a todas las facturas siguientes.

## 7. Definición de hecho para este componente

1. Los **tres vectores oficiales** pasan como test unitario.
2. Test de cadena: 50 facturas seguidas del mismo emisor forman una cadena verificable de principio a fin recalculando desde `CadenaHuella`.
3. Test de concurrencia: 20 emisiones simultáneas del mismo NIF producen `NumeroEnCadena` 1…20 sin huecos ni duplicados, y la cadena verifica.
4. Test de cultura: los mismos vectores pasan con `CultureInfo.CurrentCulture` forzada a `es-ES`, a `en-US` y a `InvariantCulture`.
5. Test de primer registro: `PrimerRegistro = "S"`, sin bloque `RegistroAnterior`, y **con** huella propia informada.

## 8. Riesgos y mejoras sugeridas

| # | Riesgo | Propuesta |
|---|---|---|
| H1 | **Cultura del servidor.** Es el fallo más probable y el más silencioso | Test de cultura (§7.4) + `InvariantCulture` explícito. Además, arrancar la aplicación con `DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY` documentado en la unidad systemd |
| H2 | La especificación puede publicar una versión nueva (0.1.2 es de agosto de 2024) | Anotar versión y fecha en el propio código (constante `VersionEspecHuella`), revisarla en cada release y dejarla visible en la pantalla de declaración responsable |
| H3 | Los decimales admiten una o dos posiciones; nuestra implementación debe elegir **una** forma y ser consistente | Fijar **siempre dos decimales** (`F2` con `InvariantCulture`). Que la AEAT admita ambas no significa que debamos generar unas veces una y otras otra |
| H4 | Un fallo en la cadena rompe **todos** los registros posteriores del emisor | Verificación de la cadena al arrancar y un trabajo diario que reverifica las cadenas modificadas ese día. Detectar en horas, no en meses |
| H5 | Reordenar una cola de envío no puede reordenar la cadena | La cadena se fija **en la emisión**, no en el envío. El orden de envío es indiferente para la huella. Está bien así por diseño, pero conviene que quede dicho para que nadie "optimice" reordenando |
