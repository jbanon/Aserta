# ADR-002 · Con qué certificado se remiten los registros Veri\*Factu a la AEAT

- **Estado:** Aceptada *(pendiente de validación jurídica antes de producción — ver §7)*
- **Fecha:** 2026-09-22
- **Decide:** Agente Arquitecto · **Valida:** asesor legal / usuario
- **Afecta a:** M6, seguridad, modelo de datos, argumentario comercial, contrato con la gestoría
- **Relacionado:** [DA-06](../decisiones-abiertas.md), [09-seguridad-rgpd.md](../09-seguridad-rgpd.md), [06-verifactu/envio-y-cola-reintentos.md](../06-verifactu/envio-y-cola-reintentos.md)

---

## 1. Contexto

El envío de registros de facturación a la AEAT se hace contra un **servicio web SOAP con autenticación mutua TLS**: el cliente presenta un **certificado electrónico** y la AEAT decide, a partir de él, **quién está enviando y en nombre de quién**.

Quien tiene la obligación de expedir la factura es el **obligado tributario emisor** (el cliente de la gestoría: la panadería, el fontanero, la SL). Pero quien opera el software es la **gestoría**. Y quien lo ha construido somos **nosotros**. Hay por tanto tres candidatos posibles a poner el certificado, y la elección no es técnica: es jurídica, operativa y comercial a la vez.

Esta es, después de la huella encadenada, **la decisión de mayor riesgo del proyecto**. Elegir mal no rompe la demo: rompe el producto en producción, cuando ya hay clientes reales facturando.

> **Límite de esta decisión.** No puedo confirmar contra fuente oficial el detalle de qué modalidades de representación admite exactamente el servicio de Veri\*Factu ni los nombres de los elementos del XSD que identifican al remitente y al obligado. Todo lo marcado `[VERIFICAR]` debe comprobarse en la **documentación técnica publicada por la AEAT en su sede electrónica** (esquemas XSD, manual del servicio web) y en el **RD 1007/2023** y la **Orden HAC/1177/2024** antes de escribir una línea de la implementación real. El simulador de la demo no depende de esto.

## 2. Opciones

### Opción A · Certificado del propio cliente (obligado tributario)

La gestoría carga en la plataforma el certificado de cada uno de sus clientes y los envíos de cada uno van con el suyo.

| | |
|---|---|
| **A favor** | Es la situación conceptualmente más limpia: el obligado se identifica a sí mismo. No requiere apoderamiento ni alta como colaborador social. Menor exposición jurídica para la gestoría y para nosotros |
| **En contra** | **Operativamente inviable a escala.** Una gestoría con 400 clientes custodiaría 400 ficheros PFX con sus 400 contraseñas, cada uno con su fecha de caducidad y su renovación. Es una pesadilla de gestión y un objetivo jugoso para un atacante. Muchos autónomos **no tienen** certificado, o lo tienen en un pendrive, o es el de su gestor. Y custodiar el certificado de un tercero tiene implicaciones de responsabilidad que conviene mirar de frente `[VERIFICAR]` |
| **Coste real** | Alertas de caducidad × 400, soporte, y el día que caduque uno, las facturas de ese cliente se quedan en la cola |

### Opción B · Certificado de la gestoría, actuando en representación *(recomendada)*

La gestoría usa **su** certificado (de persona jurídica o de representante) para remitir los registros de todos sus clientes, amparada en la figura de **colaborador social** o en un **apoderamiento** inscrito en el registro de apoderamientos de la AEAT `[VERIFICAR]`.

| | |
|---|---|
| **A favor** | **Es exactamente lo que la gestoría ya hace hoy** para presentar el 303 o el 111 de sus clientes: la figura le resulta familiar y ya tiene la infraestructura de apoderamientos montada. Un solo certificado por tenant: custodia, renovación y alertas manejables. Operativamente es la única opción que escala |
| **En contra** | Exige que **exista y esté vigente** el apoderamiento o la adhesión al convenio de colaboración social **para este trámite concreto** — que un despacho pueda presentar el 303 de un cliente no implica automáticamente que pueda remitir sus registros de facturación `[VERIFICAR]`. La gestoría asume responsabilidad por los envíos. Si el certificado caduca, **se para la facturación de toda la cartera**: es un punto único de fallo con consecuencias grandes |
| **Requisito de producto** | La plataforma debe **registrar y exigir** la existencia del apoderamiento por cliente antes de permitir emitir en su nombre, con fecha de alta y documento de respaldo. Sin eso, la gestoría estaría enviando sin cobertura y no lo sabría |

### Opción C · Certificado de sello electrónico del productor del software (nosotros)

Todos los envíos de todos los clientes de todas las gestorías salen con nuestro certificado.

| | |
|---|---|
| **A favor** | Cero fricción de alta: el cliente factura desde el minuto uno. Operativamente trivial: un certificado |
| **En contra** | **Nos convierte en parte de la relación tributaria de miles de obligados que no son clientes nuestros.** Concentra toda la responsabilidad en el productor del software: si hay un incidente, un envío erróneo o un uso indebido, el rastro lleva a nuestro sello. Un único certificado comprometido afecta a **toda** la base de clientes. Y no está claro que la figura del productor como remitente universal sea la contemplada por la norma `[VERIFICAR]` — el RSIF distingue con cuidado los papeles de *productor* del sistema y *obligado* que lo usa |
| **Veredicto** | **Descartada como modo por defecto.** El beneficio (comodidad) no compensa ni de lejos la concentración de riesgo jurídico |

## 3. Decisión

1. **Modo por defecto: Opción B** — certificado de la gestoría actuando en representación del obligado.
2. **Soporte de la Opción A** como alternativa por cliente, para las gestorías que prefieran (o cuyos clientes exijan) que cada uno envíe con el suyo. El diseño lo contempla desde el principio: **el certificado se resuelve por obligado emisor**, no es un ajuste global.
3. **Opción C descartada** como modo de envío. El certificado de sello del productor se reserva, si acaso, para el entorno de pruebas y para el sellado interno de nuestros propios artefactos, nunca para remitir en nombre de terceros.
4. En la demo se usa el **simulador**, que no valida certificados. El modelo de datos y las pantallas se construyen igualmente como si fueran reales, porque enseñar la gestión de certificados y las alertas de caducidad **es parte del argumentario** ante un socio de gestoría.

### 3.1 Consecuencia de diseño: el certificado se resuelve, no se configura

```
IProveedorCertificado
    ObtenerAsync(GestoriaId, NifObligado) -> CertificadoResuelto
```

Resolución en cascada:

1. ¿hay certificado **propio del obligado** cargado y vigente? → se usa (Opción A);
2. si no, ¿hay certificado **de la gestoría** vigente **y apoderamiento registrado para ese obligado**? → se usa (Opción B);
3. si no → el envío **no se intenta**: la factura se emite igualmente (la emisión nunca depende de la AEAT, ver `envio-y-cola-reintentos.md`) pero el registro queda en estado `BLOQUEADO_SIN_CERTIFICADO`, con alerta al gestor. **Nunca se cae al certificado del productor por descarte**, que es justo el error que convertiría la opción C en la realidad sin que nadie lo hubiera decidido.

### 3.2 Consecuencias técnicas

| Aspecto | Decisión |
|---|---|
| Cliente HTTP | **Un `HttpClient` por certificado**, gestionado por `IHttpClientFactory` con clientes nombrados por huella digital del certificado. Mezclar certificados en un mismo `HttpClientHandler` produce reutilización de conexiones TLS con el certificado equivocado: es un fallo silencioso y grave |
| Carga del PFX | `X509CertificateLoader` (PKCS#12). El fichero **no se guarda en el directorio de despliegue** |
| Custodia | PFX cifrado en reposo con **ASP.NET Core Data Protection**, con las claves persistidas fuera del directorio de la aplicación y protegidas a nivel de sistema de ficheros. **La contraseña del PFX nunca en base de datos en claro** |
| Acceso | Todo acceso al material del certificado queda **auditado** (quién, cuándo, para qué obligado) |
| Caducidad | Alertas a **60 / 30 / 7 días** (`AlertaCaducidadCertificadosWorker`). Un certificado caducado en la opción B para la facturación de toda la cartera: la alerta no es un detalle, es una función crítica |
| Entornos | URLs de **preproducción** y **producción** por configuración. Nunca embebidas en código; valores oficiales `[VERIFICAR]` en la sede de la AEAT |
| Representación en el registro | El registro debe identificar al obligado emisor y, cuando proceda, al remitente/representante. **Nombres exactos de los elementos del XSD:** `[VERIFICAR]` contra el esquema oficial. **Prohibido inventarlos** |

### 3.3 Consecuencias de modelo de datos

Entidades nuevas (detalle en `04-modelo-datos.md`):

- `Certificado` — titular (NIF), tipo (obligado / gestoría), huella digital, fechas de validez, referencia al material cifrado, estado.
- `Apoderamiento` — gestoría, cliente obligado, alcance, fecha de alta y de fin, documento de respaldo. **Requisito para operar en modo B.**
- `AccesoCertificadoLog` — append-only.

### 3.4 Consecuencias comerciales

- **A favor del producto:** la gestión centralizada de certificados y apoderamientos, con avisos de caducidad, es en sí misma una función vendible. Hoy eso vive en una carpeta compartida y en la cabeza de una persona.
- **A tener preparado en la demo:** la pregunta *"¿y quién firma esto?"* la va a hacer el socio de cada gestoría. La respuesta —"usted, con su certificado, como ya hace con el 303, y nosotros le controlamos los apoderamientos y las caducidades"— es más tranquilizadora que "nosotros lo enviamos por usted".
- **Riesgo comercial de la opción B:** la gestoría asume responsabilidad. Hay que reflejarlo en el contrato de prestación de servicio.

## 4. Consecuencias negativas asumidas

| Consecuencia | Mitigación |
|---|---|
| Punto único de fallo: el certificado de la gestoría | Alertas escalonadas, estado visible en el cuadro de mando del socio, y posibilidad de caer a certificados de obligado (opción A) cliente a cliente |
| Exige registrar apoderamientos, lo que añade fricción al alta | Se convierte en función de valor (control de vencimientos de apoderamientos). Alta masiva por importación |
| Custodiar material criptográfico de terceros sin un KMS | Data Protection + permisos de sistema de ficheros + auditoría. **Documentado como riesgo residual** en `09-seguridad-rgpd.md`: es la mitigación razonable disponible, no equivale a un HSM |

## 5. Alternativa que conviene no perder de vista

Si el marco normativo lo permite `[VERIFICAR]`, la opción **más robusta a largo plazo** no está en esta lista: que **el propio obligado autorice el envío una sola vez** de forma delegada, sin que nadie custodie su PFX. Si la AEAT ofrece o llega a ofrecer un mecanismo de ese tipo para este servicio, sustituye a A y B y elimina el problema de custodia de raíz. Conviene revisarlo antes de producción.

## 6. Cómo se verifica

1. Test: emitir para un obligado sin certificado ni apoderamiento → la factura **se emite** y el registro queda `BLOQUEADO_SIN_CERTIFICADO`.
2. Test: dos obligados con certificados distintos en la misma ráfaga de envío → cada petición usa el `HttpClient` correcto *(verificado con un manejador de prueba que inspecciona el certificado presentado)*.
3. Test: la contraseña del PFX no aparece en claro en base de datos ni en logs.
4. Test: un certificado a 30 días de caducar genera exactamente una alerta, no una por ejecución del worker.

## 7. Pendiente antes de producción  `[VERIFICAR]`

- [ ] Confirmar qué modalidades de representación admite el servicio de Veri\*Factu y con qué requisitos formales (colaboración social vs. apoderamiento específico).
- [ ] Confirmar los elementos del XSD que identifican obligado y remitente, y cómo se declara la representación.
- [ ] Confirmar los tipos de certificado admitidos (persona jurídica, representante, sello).
- [ ] Confirmar URLs de preproducción y producción, y el procedimiento de alta en el entorno de pruebas.
- [ ] Revisión por asesor legal del reparto de responsabilidad entre productor del software, gestoría y obligado.
