# 09 · Investigación: facturación Veri\*Factu real (opción A frente a opción C)

> **Tarea de investigación, sin código de producto** · Agente Programador · consultas realizadas el **2026-09-30**
>
> Leyenda: **[VERIFICADO]** = leído ese día en una fuente oficial (BOE, sede de la AEAT, portal de desarrolladores de la AEAT, documentación de Microsoft) o en la web pública del propio proveedor; **[NO VERIFICADO]** = no encontrado en fuente oficial, o solo en fuente secundaria (se indica cuál); **[ESTIMACIÓN]** = cifra mía, sin fuente. No hay ninguna URL ni dato de la AEAT escrito de memoria: lo que no pude confirmar está marcado como tal. Las fuentes con URL y fecha están en la sección 10.

## 0. Lo que cambia decisiones (resumen)

1. **Las URL del servicio web existen, son oficiales y ya estaban en el repositorio.** El WSDL versionado en `src/Aserta.Verifactu/Esquemas/SistemaFacturacion.wsdl` (descargado el 2026-09-22 de la AEAT) contiene los ocho `soap:address`. Lo he vuelto a descargar hoy de la AEAT y coincide. **D-34 queda resuelta** en cuanto a direcciones; sigue pendiente un certificado real para probar. [VERIFICADO]
2. **El artículo 7.i de la Orden HAC/1177/2024 no se aplica a un sistema que actúe como VERI\*FACTU.** Lo dice el artículo 3 de la misma Orden, literal: «en tanto actúen como "VERI\*FACTU", no les serán de aplicación los artículos 6.b), 6.c), 6.d), 6.e), 6.f), 7.f), 7.h), 7.i), 7.j), 8 y 9 de esta orden». La demo no implementa la comprobación previa (sección 4), pero como se declara «solo VERI\*FACTU» **no está obligada**. La partida de 3–5 días de la estimación baja a 0 (o a 1 si se quiere como defensa propia). [VERIFICADO en el BOE consolidado]
3. **La modalidad NO VERI\*FACTU es mucho más cara de lo estimado**: firma XAdES con certificado cualificado del obligado, cadena de eventos propia (10 tipos, resumen cada 6 horas y al apagar), alarma no desactivable, verificación bajo demanda, exportación con formato del anexo y servicio de requerimiento. Mi estimación: 21–37 días, no 0–15. [ESTIMACIÓN sobre requisitos VERIFICADOS]
4. **Los casos de factura que faltan son más de los previstos** (F3, rectificativas por sustitución, R2/R3, S2/N1/N2, E2–E6, destinatarios extranjeros, subsanación/rechazo previo, tercero emisor, regímenes 02–20, IGIC/IPSI). Estimación: 20–33 días completo, 12–20 recortado. La partida de 10–18 es baja. [ESTIMACIÓN]
5. **«Convenio 17» es el acuerdo de colaboración social Tipo 17 «Empresas sistemas informáticos de facturación»**, pensado para productores de software (se solicita por correo a la AEAT). Una gestoría encaja en el Tipo 1 (a través de su colegio o asociación). El apoderamiento específico para remitir por servicio web tiene código **IZ860**. [VERIFICADO]
6. **La opción C no elimina la declaración responsable de Aserta.** Verifacti y fiskaly exigen expresamente que el integrador publique la suya; la declaración de Verifacti dice que sus clientes «conservan la responsabilidad del cumplimiento normativo global». Invopop solo pide enlazar la suya. [VERIFICADO en las webs de los proveedores]
7. **Ninguno de los cuatro proveedores de API permite usar el certificado de la gestoría ni subir un PFX del cliente para VERI\*FACTU**: los cuatro remiten con su propio certificado de colaborador social y exigen un documento de representación firmado por cada NIF. [VERIFICADO]
8. **La tabla de la página de entrevista está desfasada en un punto**: el aviso de caducidad de certificados ya existe desde la fase 5 (`ServicioAlertaCertificados`, tarea diaria a 60/30/7 días, informe 08).

## 1. Entorno de pruebas y producción de la AEAT

### 1.1 Direcciones del servicio web [VERIFICADO]

Copiadas del WSDL oficial, descargado el 2026-09-30 tanto de producción (`https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tikeV1.0/cont/ws/SistemaFacturacion.wsdl`) como de pruebas (`https://prewww2.aeat.es/static_files/…/SistemaFacturacion.wsdl`); ambos ficheros contienen las mismas direcciones. Operaciones: `RegFactuSistemaFacturacion` (remisión) y `ConsultaFactuSistemaFacturacion` (consulta).

| Servicio | Puerto WSDL | Entorno | URL |
|---|---|---|---|
| sfVerifactu | SistemaVerifactu | Producción, certificado normal | `https://www1.agenciatributaria.gob.es/wlpl/TIKE-CONT/ws/SistemaFacturacion/VerifactuSOAP` |
| sfVerifactu | SistemaVerifactuSello | Producción, certificado de sello | `https://www10.agenciatributaria.gob.es/wlpl/TIKE-CONT/ws/SistemaFacturacion/VerifactuSOAP` |
| sfVerifactu | SistemaVerifactuPruebas | Pruebas | `https://prewww1.aeat.es/wlpl/TIKE-CONT/ws/SistemaFacturacion/VerifactuSOAP` |
| sfVerifactu | SistemaVerifactuSelloPruebas | Pruebas, sello | `https://prewww10.aeat.es/wlpl/TIKE-CONT/ws/SistemaFacturacion/VerifactuSOAP` |
| sfRequerimiento | (las cuatro variantes) | Solo NO VERI\*FACTU, remisión a requerimiento | mismas máquinas con la ruta `…/SistemaFacturacion/RequerimientoSOAP` |

El documento oficial «Descripción de servicios web» v1.0.3 (28/07/2025) confirma en sus anexos 7 y 8 las URL de los WSDL de pruebas y producción, y su historial indica que la v1.0.1 (23/04/2025) fue la «Publicación de URLs de producción». Los servicios están en producción desde el 23 de abril de 2025 (FAQ de desarrolladores v1.3).

**Contraste con D-34.** D-34 decía que las URL estaban vacías «[VERIFICAR] en la sede». Estaban en el propio WSDL que ya teníamos versionado (`Esquemas/LEEME.md` documenta su descarga el 2026-09-22). Fue un fallo mío de lectura, no una carencia de la AEAT. Propongo rellenar `Verifactu:UrlServicioPruebas` con la URL de prewww1 y `UrlServicioProduccion` con la de www1, y añadir las dos variantes «sello» seleccionadas según el tipo de certificado. La demo sigue con el simulador hasta que haya certificado.

### 1.2 Acceso con certificado [VERIFICADO salvo lo indicado]

- «Descripción de servicios web» §4.3: «Protocolo: HTTPS. Mensajes: Web Service con SOAP 1.1 modo Document. Certificado: Las aplicaciones que envían información a los servicios web deberán autenticarse con certificado electrónico cualificado reconocido». El término «TLS mutua» no aparece literalmente; es la lectura técnica de «autenticarse con certificado» sobre HTTPS.
- §4.1: «La remisión a través del servicio web podrá ser efectuada por el obligado tributario, un apoderado suyo a este trámite o un colaborador social, que deberá disponer de un certificado electrónico cualificado reconocido. Todos los NIFs se tienen que validar contra la "Base de Datos Centralizada de la AEAT"».
- El WSDL separa los puertos «Sello» (www10 / prewww10); el portal de pruebas describe prewww10 como pruebas «para Contribuyentes con certificado de sello». No he encontrado una lista oficial de tipos de certificado FNMT admitidos por nombre (persona física, representante); la AEAT habla de «certificado electrónico cualificado» **[NO VERIFICADO el detalle por tipo]**.
- La cabecera del envío admite un bloque `Representante` («A rellenar solo en caso de que los registros de facturación remitidos hayan sido generados por un representante/asesor del obligado tributario», XSD `SuministroInformacion.xsd` línea 14) que la demo no rellena. Las FAQ de desarrolladores lo describen para la «colaboración social transitiva» obligado → asesor → plataforma.

### 1.3 Entorno de pruebas [VERIFICADO]

- La sede enlaza como primer elemento de «Información técnica» el **Portal de Pruebas Externas** `https://preportal.aeat.es`. Texto literal del portal: pruebas «de forma totalmente libre, con la única condición de autenticarse mediante un certificado electrónico», «sin que en ningún caso tengan trascendencia tributaria», y «solo … pruebas puntuales y en ningún caso … pruebas masivas». Equivalencias: prewww1 ≡ www1, prewww2 ≡ www2, prewww10 ≡ www10.
- **No existe certificado de pruebas ni alta previa**: vale cualquier certificado cualificado real. No hay «guía del entorno de pruebas» específica de Veri\*Factu **[VERIFICADO en negativo sobre las páginas consultadas]**.
- Servicio de cotejo del QR: producción `https://www2.agenciatributaria.gob.es/wlpl/TIKE-CONT/ValidarQR`, pruebas `https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR` (coinciden con `OpcionesVerifactu`); existe la variante `ValidarQRNoVerifactu`.
- No hay «facturas de prueba» en producción: lo emitido por un SIF en producción es real y solo se corrige con anulación o rectificativa (FAQ desarrolladores).

### 1.4 Plazos vigentes [VERIFICADO en el BOE]

Disposición final 4.ª del RD 1007/2023 en la redacción del Real Decreto-ley 15/2025, de 2 de diciembre (BOE-A-2025-24446, en vigor 04/12/2025): contribuyentes del Impuesto sobre Sociedades «antes del 1 de enero de 2027»; el resto «antes del 1 de julio de 2027». El plazo de nueve meses para productores desde la entrada en vigor de la Orden (29/10/2024) no fue retocado: venció el 29 de julio de 2025. El BOE no muestra modificaciones posteriores al 03/12/2025 ni cambios en la Orden. La convalidación del RDL por el Congreso solo la afirman fuentes secundarias **[NO VERIFICADO]**.

### 1.5 Documentos técnicos oficiales publicados [VERIFICADO]

Índice: `https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/informacion-tecnica.html`. Ficheros bajo `https://www.agenciatributaria.es/static_files/AEAT_Desarrolladores/EEDD/IVA/VERI-FACTU/`: `Veri-Factu_Descripcion_SWeb.pdf` (v1.0.3), `Veri-Factu_especificaciones_huella_hash_registros.pdf` (v0.1.2), `DetalleEspecificacTecnCodigoQRfactura.pdf` (v0.5.0), `Validaciones_Errores_Veri-Factu.pdf` (**v1.2.2, 08/04/2026**), `Espec-Tecnicas/EspecTecGenerFirmaElectRfact.pdf` (firma, v0.1.5) con `AnexosEjemplosFirmaRegFact.zip`, `DsRegistroVeriFactu.xlsx` (diseños de registro), `Descripcion_ServicioWeb_ValidacionNoVerifactu.pdf`, `FAQs-Desarrolladores.pdf` (v1.3, 04/12/2025), `EjemplosDeclaracionResponsable.pdf`. Lista de códigos de error: `https://prewww2.aeat.es/static_files/common/internet/dep/aplicaciones/es/aeat/tikeV1.0/cont/ws/errores.properties`. XSD adicionales que no tenemos: `ConsultaLR.xsd`, `RespuestaConsultaLR.xsd`, `EventosSIF.xsd`, `RespuestaValRegistNoVeriFactu.xsd`.

## 2. Carga del certificado y TLS mutua en .NET 10

### 2.1 Qué hace hoy el código

- `ClienteAeatSoap.cs` línea 44: `_http.CreateClient("aeat-" + lote.HuellaCertificado)`. En `ExtensionesServicios.cs` línea 102 solo hay `AddHttpClient()` sin nombre: **ningún cliente «aeat-…» está registrado**, así que la fábrica devuelve el manejador por defecto **sin certificado de cliente**. Contra la AEAT real el saludo TLS fallaría o devolvería un error HTTP.
- Línea 37: elige entre dos URL; faltan las dos variantes «sello».
- Línea 45: `EnvioLote(lote.NifEmisor, "", …)` pasa **razón social vacía** en la cabecera del lote (`ObligadoEmision/NombreRazon`). En la emisión se valida el XML con el nombre real, pero el lote que se envía se reconstruye sin él. Probable rechazo por la AEAT; hay que pasar el nombre del emisor al lote. **[Defecto detectado en revisión de código; no probado contra la AEAT]**
- `Certificado.MaterialCifrado` (byte[] opcional) y `Certificado.HuellaDigital` existen en el esquema desde 0008; hoy nadie los rellena (`ProveedorCertificadoDemo` resuelve certificados ficticios sin material).

### 2.2 Qué haría falta [VERIFICADO en la documentación de Microsoft]

1. **Cargar el PKCS#12** con `X509CertificateLoader.LoadPkcs12(byte[] datos, string contraseña, X509KeyStorageFlags, Pkcs12LoaderLimits)` o `LoadPkcs12Collection…` (clase disponible en .NET 9 y 10). Los constructores `new X509Certificate2(bytes, pwd)` están obsoletos desde .NET 9 (aviso SYSLIB0057) precisamente porque aceptaban cualquier formato. Usar `X509KeyStorageFlags.EphemeralKeySet` para no persistir la clave en el almacén del usuario del servicio; comportamiento en Linux a comprobar en una prueba **[a probar]**.
2. **Manejador con certificado**: `SocketsHttpHandler.SslOptions` es un `SslClientAuthenticationOptions` (disponible desde .NET Core 2.1) donde se añade `ClientCertificates` o se usa `LocalCertificateSelectionCallback`. Con `IHttpClientFactory`, la documentación recomienda `UseSocketsHttpHandler` o `ConfigurePrimaryHttpMessageHandler((handler, sp) => …)` comprobando el tipo (`HttpClientHandler.ClientCertificates` o `SocketsHttpHandler.SslOptions.ClientCertificates`), y advierte: «The number of distinct registered named clients should not be unbounded». Dos opciones válidas:
   - **A (recomendada):** una caché propia `ConcurrentDictionary<huella, HttpClient>` de clientes de larga vida con `SocketsHttpHandler { PooledConnectionLifetime = 2 min, SslOptions.ClientCertificates = [cert] }`, patrón que la misma documentación admite como alternativa a la fábrica. Se descarta al revocar o caducar el certificado.
   - **B:** registrar un `IConfigureNamedOptions<HttpClientFactoryOptions>` que, para nombres con prefijo `aeat-`, añada una acción a `HttpMessageHandlerBuilderActions` que fije el manejador con el certificado de esa huella.
3. **Selección de endpoint por tipo de certificado** (normal / sello) y por entorno; tiempo de espera explícito (la AEAT puede tardar; el `TiempoEsperaEnvio` ya se respeta).
4. **Confianza en la CA del servidor**: comprobar que el sistema tiene el almacén de CA raíz actualizado (`ca-certificates`) para las máquinas `aeat.es` y `agenciatributaria.gob.es`; primera prueba contra prewww1 con un certificado real.
5. **Bloque `Representante`** en la cabecera cuando remita la gestoría (sección 1.2) y, en el registro, `EmitidaPorTerceroODestinatario` = «T» + bloque `Tercero` si es la gestoría quien **expide** la factura en nombre del cliente (sección 6).
6. Esfuerzo de desarrollo de lo anterior: 2–4 días; el resto de la partida es depurar contra la AEAT real, que exige un certificado. [ESTIMACIÓN]

### 2.3 Riesgos de guardar certificados de clientes finales

Un certificado cualificado con su clave privada **es la identidad legal del cliente ante la AEAT y otras administraciones**: sirve para presentar declaraciones, recibir notificaciones o firmar. Custodiarlo convierte a Aserta en objetivo y en responsable. Riesgos concretos:

- Filtración de la base o de las copias: `MaterialCifrado` debe ir con cifrado de sobre (clave por certificado, envuelta por una clave maestra fuera de la base; idealmente KMS o HSM, que hoy no hay). La contraseña del PFX no debe persistir; la clave descifrada solo en memoria y con `Dispose`.
- Uso indebido interno: cada uso ya se registra en `vf.AccesoCertificadoLog`; faltaría alerta sobre usos fuera del worker de envío y sobre volúmenes anómalos.
- Baja del cliente: procedimiento de destrucción del material con evidencia, y revocación si hubo incidente.
- RGPD y contrato: encargo del tratamiento con cláusula específica sobre custodia de claves; notificación de brechas en 72 horas.
- Condiciones del prestador del certificado (FNMT u otros) sobre la cesión de la clave privada a terceros: **[NO VERIFICADO]**, comprobar antes de ofrecerlo.

**Recomendación:** no custodiar certificados de clientes. Remitir con el certificado de la gestoría (representante de persona jurídica) como colaboradora social o apoderada, que es lo que ya diseña ADR-002 y lo que hacen los cuatro proveedores de API con el suyo. Esa vía necesita un solo certificado, custodiado por la gestoría, y un documento de representación por cliente (sección 3).

## 3. Colaboración social y apoderamiento [VERIFICADO salvo lo indicado]

### 3.1 «Convenio 17»

No existe un «Convenio 17» como tal. Es el **acuerdo de colaboración social Tipo 17 «Empresas sistemas informáticos de facturación»**: «Acuerdos con entidades suministradoras de software» para el suministro electrónico de registros de facturación (SII), asientos contables (SILICIE) y «remisión de ficheros que contienen los registros de facturación generados por sistemas de emisión de facturas» en representación de terceros. La FAQ de Veri\*Factu dice: «Para la firma del acuerdo … deberán ponerse en contacto con la Agencia Tributaria a través de la dirección comunicacion.sepri@correo.aeat.es». La AEAT publica la relación de firmantes por comunidad (Madrid: 142 empresas a 03/08/2026, entre ellas Invopop, fiskaly y Wolters Kluwer). **No se publica plazo de firma ni requisitos técnicos** más allá del certificado cualificado del colaborador.

Es un convenio para **productores de software** (sería el de Aserta en la opción A). Una **gestoría** encaja en el **Tipo 1 «Colegios y asociaciones de profesionales de la gestión tributaria»**: adhesión individual a través de su colegio o asociación.

### 3.2 Pasos para la gestoría (Tipo 1)

1. Pertenecer a un colegio o asociación con acuerdo y figurar en la relación de colegiados que esa entidad comunica a la AEAT (trámite del colegio, plazo no publicado).
2. Certificado cualificado de persona física o de representante de persona jurídica; los empleados pueden actuar como «colaboradores delegados» con su propio certificado.
3. Trámite en sede «Alta y gestión en el censo de colaboradores sociales» (procedimiento ZC01): identificarse, teléfono y correo, seleccionar la asociación, opcionalmente delegados, «Firmar y enviar»; el justificante es inmediato. Alternativa presencial en la Delegación.
4. Comprobación posterior en «Comprobación en el censo de colaboradores sociales».
5. Por cada cliente, **modelo de representación del Anexo II** de la Resolución de 18 de diciembre de 2024 (BOE-A-2024-27600), firmado a mano con copia del DNI o con firma electrónica cualificada o avanzada eIDAS; «no se admitirán modalidades de aceptación de condiciones del servicio». El documento «debe quedar en poder del colaborador social y no debe aportarse a la AEAT, salvo requerimiento». Si además remite una plataforma de software, el cliente lo autoriza en el mismo Anexo II y la gestoría firma el Anexo III con el proveedor.

La FAQ oficial confirma que la colaboración social ampara la remisión de registros Veri\*Factu: «se puede utilizar cualquiera de las vías actuales de identificación admitidas por la AEAT, incluyendo la representación, el apoderamiento y la colaboración social, haciendo uso de un certificado cualificado por parte del tercero».

### 3.3 Alternativa: apoderamiento electrónico

Procedimiento ZP01 (Registro de apoderamientos). Códigos específicos publicados por la AEAT: **IZ860 «Remisión y consulta de registros de facturación por servicio web»** (el que interesa a un software propio), IZ861 consulta, IZ862/IZ863 para la aplicación gratuita de la AEAT, IZ864 aportar autorización; o el poder general **GENERALLEY58**. Se otorga por internet con certificado, DNIe o Cl@ve del cliente (efecto inmediato para trámites que no sean de notificación), por comparecencia o por documento público. Vigencia máxima 5 años, prorrogable en los dos meses previos; revocable en cualquier momento con efecto desde su comunicación. La FAQ exige que el apoderamiento esté «inscrito en el registro de apoderamientos».

### 3.4 Comparación práctica

| | Colaborador social (Tipo 1) | Apoderado (IZ860) |
|---|---|---|
| Habilitación | Una vez (censo) | Un poder por cliente, inscrito por el cliente |
| Documento del cliente | Anexo II firmado, custodiado por la gestoría | El propio poder; nada más |
| Certificado que remite | El de la gestoría o delegado | El de la gestoría |
| Fricción por cliente | Baja (papel o firma electrónica) | Media (el cliente debe hacer el trámite con su certificado o Cl@ve) |
| Responsabilidad | El obligado la conserva; el colaborador responde de la autenticidad de la firma del otorgante | El obligado la conserva |

Para Aserta: la vía con menos fricción por cliente es la colaboración social de la gestoría con Anexos II; el apoderamiento IZ860 es la alternativa cuando el cliente no puede o no quiere firmar el modelo. Ambas se registran hoy en `vf.Apoderamiento` (habría que distinguir la vía y guardar el documento).

## 4. Artículo 7.i de la Orden HAC/1177/2024 y la demo

### 4.1 Texto literal [VERIFICADO, BOE consolidado, texto sin modificaciones desde 28/10/2024]

Artículo 7.i): «Salvo cuando se trate del primer registro de facturación, cada vez que el sistema informático vaya a generar un nuevo registro de facturación, de alta o de anulación, antes deberá comprobar que se cumplen los siguientes requisitos: 1.º El último registro de facturación generado está correctamente encadenado. 2.º La fecha y hora de generación del último registro de facturación generado no es superior en más de un minuto a la fecha y hora actuales que se utilizarán para fechar el registro de facturación a generar.»

Artículo 7.j): si detecta algo que vulnere la trazabilidad, «deberá avisar de ello, procediendo de la misma forma que se indica en el artículo 6.f)», es decir, alarma «que no deberá desactivarse hasta que no se pueda volver a garantizar la integridad» y registro de evento.

**Artículo 3**: «se presumirá que los sistemas informáticos que tengan la consideración de "Sistemas de emisión de facturas verificables" o "VERI\*FACTU", cumplen por diseño ciertos requisitos … y, en tanto actúen como "VERI\*FACTU", no les serán de aplicación los artículos 6.b), 6.c), 6.d), 6.e), 6.f), 7.f), 7.h), 7.i), 7.j), 8 y 9 de esta orden.» Siguen aplicando en VERI\*FACTU: 6.a) (huella), 7.a)–e) y 7.g) (encadenamiento, hora exacta con margen de un minuto para el usuario, huso).

Qué exige el 7.i cuando aplica: comprobar **solo el último registro** (no toda la cadena), y que su fecha y hora no vaya más de un minuto por delante de la actual. La comprobación de toda o parte de la cadena es una capacidad bajo demanda (6.e) y la navegación anterior/siguiente con indicación de encadenamiento correcto es el 7.h; ambos también excluidos en VERI\*FACTU.

### 4.2 Qué hace la demo, con líneas de `src/Aserta.Verifactu/Servicios/ServicioEmision.cs`

| Requisito | Estado | Dónde |
|---|---|---|
| Huella anterior tomada bajo bloqueo de la fila del emisor | Sí | L88 `IniciarEmisionAsync` (UPDLOCK, HOLDLOCK en `RepositorioFacturacion.cs` L48) y L108–112 usan `cadena.UltimaHuella` |
| 7.i.1º «el último registro generado está correctamente encadenado» | **No** | L116 y L196–203 (`UltimoRegistroAsync`) recuperan el registro anterior solo para copiar su identificador de factura al XML; **no comparan** `ultimo.Huella` con `cadena.UltimaHuella` ni recalculan `CalcularHuella(ultimo.CadenaHuella)` |
| 7.i.2º fecha/hora del último registro no más de un minuto por delante de la actual | **No** | `cadena.FechaUltimoRegistroUtc` se escribe (L134, L174) pero nunca se compara con `_reloj.AhoraUtc` antes de generar |
| Anulación: mismas dos comprobaciones | **No** | L155–164, misma estructura |
| 7.h navegación anterior/siguiente con indicación de encadenamiento | Parcial | `Pages/Facturacion/Factura.cshtml.cs` L47 recalcula la huella de cada registro desde `CadenaHuella` y la vista (L61) muestra si coincide; no comprueba que `HuellaAnterior` sea la huella real del registro previo ni ofrece saltar al anterior o siguiente |
| 6.e verificación de toda o parte de la cadena bajo demanda | No | No existe |
| 6.f / 7.j alarma persistente y registro de evento | No | No existe registro de eventos |

**Conclusión:** la demo **no cumple el 7.i**, pero **no está obligada** mientras actúe solo como VERI\*FACTU (art. 3), que es lo que declara. Aun así recomiendo la versión barata como defensa propia, dentro de la misma transacción bloqueada: recalcular la huella del último registro y compararla con `cadena.UltimaHuella`, comprobar la marca de tiempo, y si falla marcar la cadena como bloqueada y avisar (nunca generar sobre una cadena corrupta). Coste: **0,5–1 día** con test. [ESTIMACIÓN] Si algún día se ofrece NO VERI\*FACTU, esto es solo una pieza del paquete de la sección 5.

## 5. Modalidad NO VERI\*FACTU

### 5.1 Requisitos [VERIFICADO, RD 1007/2023 y Orden HAC/1177/2024]

- **Firma electrónica** de cada registro de facturación y de evento (art. 12 RD; art. 14 Orden): «XAdES Enveloped Signature» según ETSI EN 319 132, «con una clave privada asociada a un certificado electrónico cualificado de firma electrónica en vigor» de un prestador de la lista de confianza de la UE; la firma «deberá almacenarse en el registro». Detalles técnicos en el documento de la AEAT `EspecTecGenerFirmaElectRfact.pdf` (v0.1.5). Un sistema VERI\*FACTU está exento (art. 16.3 RD).
- **Registro de eventos** (art. 8.3 RD; art. 9 Orden): al menos los eventos 01–09 (inicio y fin como NO VERI\*FACTU, lanzamiento y detección de anomalías en registros y en eventos, restauración de copia, exportación de registros y de eventos), un **registro resumen cada 6 horas** de funcionamiento y **antes de apagarse** (tipo 10), 90 para voluntarios. Los eventos forman **su propia cadena** con huella y firma (agrupación `EventoAnterior`; huella sobre productor, sistema, versión, instalación, NIF, tipo, huella anterior y fecha-hora-huso, art. 13.1.c), formato XML UTF-8 del apartado 5 del anexo (XSD `EventosSIF.xsd`).
- **Alarma no desactivable y evento** ante cualquier anomalía (6.f, 7.j); **verificación bajo demanda** de hash, firma y cadena (6.b, 6.d, 6.e); **navegación** anterior/siguiente con indicación (7.h); **comprobación previa** 7.i.
- **Conservación y exportación** (art. 8.2.c RD; art. 8 Orden): exportar «todos los registros de facturación generados en un periodo» a soporte externo en formato legible manteniendo la estructura de los artículos 10 y 11, con proceso independiente de las copias de seguridad; la exportación genera evento 08/09.
- **Remisión a requerimiento** (art. 14.2 RD; art. 18 Orden) por el servicio `RequerimientoSOAP` con `RefRequerimiento` en la cabecera, hasta 1.000 registros por fichero; validación previa opcional con el servicio de validación de registros no Veri\*Factu.
- **Declaración responsable** en ambas modalidades; en NO VERI\*FACTU debe indicar los «tipos de firma utilizados» (art. 15.1.g Orden). La actual dice «solo VERI\*FACTU» y habría que reemitirla.
- Cadenas independientes por obligado también para eventos (art. 2 Orden).

### 5.2 Esfuerzo si se añadiera [ESTIMACIÓN]

| Pieza | Días | Nota |
|---|---|---|
| Firma XAdES Enveloped conforme a la especificación de la AEAT, con certificado del obligado o del representante | 4–8 | .NET trae XMLDSig (`SignedXml`) pero no XAdES; hay que construir `QualifyingProperties` a mano o con librería de terceros, y validar contra los ejemplos oficiales |
| Registro de eventos: entidades, cadena propia, huella, firma, 10 tipos, resumen cada 6 h y al apagar, consulta en pantalla | 6–10 | El «al apagarse» obliga a un apagado ordenado del servicio |
| Alarma persistente, verificación bajo demanda (hash, firma, cadena, fechas), navegación 7.h, comprobación previa 7.i | 3–5 | |
| Exportación por periodo con formato del anexo (facturación y eventos) y eventos 08/09 | 2–4 | |
| Servicio de requerimiento (`RequerimientoSOAP`, `RefRequerimiento`) y validación previa | 3–5 | Sin certificado real no se puede probar |
| Custodia del certificado de firma (sección 2.3), declaración responsable nueva, tests | 3–5 | |
| **Total** | **21–37** | frente a 0–15 en la estimación |

Además la firma exige un certificado cualificado **del obligado o de su representante**: reabre el problema de custodia de la sección 2.3 para cada cliente, salvo que firme la gestoría como representante (a confirmar jurídicamente **[NO VERIFICADO]**).

## 6. Casos de factura que faltan [reglas VERIFICADAS en «Validaciones y errores» v1.2.2, XSD, anexo de la Orden; esfuerzo ESTIMACIÓN]

Lo que la demo tiene: F1, F2, R1/R4/R5 por diferencias (I), `ClaveRegimen` 01, S1, E1, tipos 21/10/4/0, recargo, retención (la retención no viaja en el registro; es dato de la factura).

| Caso | Campos y reglas de la AEAT (código de error) | Días |
|---|---|---|
| Rectificativa **por sustitución** (S) | `ImporteRectificacion` obligatorio si S (1118) y prohibido si I (1119), con `BaseRectificada`, `CuotaRectificada` y opcional `CuotaRecargoRectificado`; `FacturasRectificadas` hasta 1.000 `IDFacturaRectificada` (1117, 1154); en S el desglose lleva los importes correctos definitivos | 2–3 |
| **R2 / R3** | Sin bloques nuevos; R3 solo destinatarios con NIF o `IDType` 07 (1191); R2 NIF, 07 o 02 (1192); la comprobación cuota = base × tipo se exceptúa en I, R2 y R3 (1142, 1143) | 1–2 |
| **F3** sustitutiva de simplificadas | Siempre con destinatario (1189); `FacturasSustituidas` solo en F3 (1116), no obligatoria según validaciones pero la FAQ 27 pide identificar las F2 canjeadas; no se anulan las F2; corrección de F3 con F3 negativa + nueva | 2–3 |
| **F2**: tope y variantes | La AEAT valida Σ(base + cuota) ≤ 3.000 € con tolerancia de 10 € (1150); la demo comprueba solo Σ(cantidad × precio) en L219 (**ajustar a base + cuota**); exenciones del tope con `NumRegistroAcuerdoFacturacion` o `FacturaSinIdentifDestinatarioArt61d`; la AEAT **no valida** el umbral de 400 € del art. 4 del reglamento de facturación: es responsabilidad del SIF | 1 |
| **Calificación S2 (inversión), N1/N2 (no sujeta), exenciones E2–E6** | Exactamente uno de `CalificacionOperacion` u `OperacionExenta` por línea (1195, 1196); S2 con tipo y cuota = 0 explícitos y solo F1/F3/R1–R4 (1198, 1197); N1/N2 y exentas sin tipo, cuota ni recargo (1237, 1238); E5 con destinatario solo `IDOtro` (1289) | 2–3 |
| **Destinatarios extranjeros y no censados** | `IDOtro` = `CodigoPais` + `IDType` (02 NIF-IVA, 03 pasaporte, 04 documento del país, 05 certificado de residencia, 06 otro, 07 no censado) + `ID`; país obligatorio salvo 02 (1111); ES solo con 03 o 07 (1126, 1234); NIF español no censado → aceptado con errores 2001, se subsana con 07; formato NIF-IVA por Estado miembro | 2–3 |
| **Regímenes** 02 exportación (solo exenta, 1286), 03 REBU (solo S1, 1200), 05/09 agencias (eximen de la comprobación de totales), 07 criterio de caja (1203), 08 con N2 (1252), 11 arrendamiento solo 21 % (1206), 14/15 con `FechaOperacion` (1147, 1173), 17 OSS, 19 REAGYP, 20 simplificado | 0,5–1 cada uno | 4–7 |
| **06 grupo de entidades** con `BaseImponibleACoste` (1202, 1209, 1257) | Poco probable en gestoría pequeña; opcional | 1 |
| **IGIC / IPSI** (`Impuesto` 03 / 02, listas L8B con claves 20 y 21, exenciones E7/E8; IPSI claves 01, 08, 11, 18, 19, 20) | Solo si hay clientes en Canarias, Ceuta o Melilla | 2–3 |
| **Subsanación y rechazo previo** | `Subsanacion` S/N y `RechazoPrevio` N/S/X (1153, 1161): reenvío tras rechazo sin marcas; subsanación de registro aceptado con errores S + N; subsanación rechazada S + S; registro inexistente S + X | 2–3 |
| **Tercero emisor** (la gestoría expide en nombre del cliente) | `EmitidaPorTerceroODestinatario` = T + bloque `Tercero` obligatorio (1186, 1187), NIF distinto del emisor (1188); D exige destinatario (1158) | 1–2 |
| `Macrodato` (obligatorio S si \|total\| ≥ 100.000.000, 1138), `Cupon` (solo R1/R5, 1157), `RefExterna` (60), `FechaOperacion` genérica (1134, 1125, 1146) | 1 |
| **Tipos históricos** | Permitidos 0, 2, 4, 5, 7,5, 10, 21 según `FechaOperacion` o expedición (1124, 1194, 1235, 1236): 5 % (01/07/2022–30/09/2024), 2 % y 7,5 % (01/10–31/12/2024); recargos 0,26 / 0,5 / 0,62 / 1 / 1,4 / 1,75 / 5,2 emparejados con su tipo (1162–1170) | 0,5–1 |
| **Total completo** | | **20–33** |
| **Total recortado** (sin IGIC/IPSI, 06, 14/15, 17, 19, 20) | | **12–20** |

Tipos vigentes a 2026-09-30 [VERIFICADO, AEAT «Tipos impositivos en el IVA 2026» de 26/02/2026]: 21 %, 10 %, 4 % y 0 % solo para donativos de mecenazgo; recargos 5,2 / 1,4 / 0,5 / 1,75 (tabaco). Energía: 10 % del 22/03/2026 al 30/06/2026; la nota de la AEAT de 30/06/2026 sobre el RDL 18/2026 dice que gas y electricidad volvieron al 21 % y prevé reactivar rebajas si suben los precios; situación de septiembre de 2026 **[NO VERIFICADO]**.

## 7. Proveedores de API Veri\*Factu [VERIFICADO en sus webs el 2026-09-30, salvo «no encontrado»]

| | Verifacti (Bilbabit SL) | Invopop (Invopop S.L.) | verifactuapi.es (Invocash Solutions S.L.) | fiskaly SIGN ES (fiskaly GmbH / Iberia S.L.) |
|---|---|---|---|---|
| Documentación | `verifacti.com/docs` (Swagger), guía rápida, FAQ, ejemplos en C#, VB6 y otros; sin SDK oficial | `docs.invopop.com`; modelo GOBL (JSON) con flujos; SDK solo Go y Ruby; **sin .NET** | `app.verifactuapi.es/docs`, Postman; repositorio `verifactu-net` con ejemplos, sin paquete NuGet | `workspace.fiskaly.com/sign-es`; API REST con jerarquía organización → taxpayer → signer → client; SDK .NET solo para el producto alemán |
| Qué devuelven | QR en Base64 síncrono, XML; **no PDF** | **PDF A4 con QR**, XML firmado | `url_qr`, `qr_image`, huella, `estado_aeat`, XML; **no PDF** | URL de cotejo y leyenda; PDF no encontrado |
| Sandbox | Gratis, 1 NIF, contra pruebas AEAT | Gratis (plan Dev, aceptación simulada) | Gratis e ilimitado | TEST gratis; no llega a la AEAT |
| Precio (literal) | 0 € con 1 NIF de prueba; **desde 2,9 €/NIF/mes** hasta 100 NIF; 3.000 facturas/NIF/mes incluidas, exceso 2 €/1.000; −10 % anual; sin permanencia | Dev 0 €/mes (200 «pops»); **Pro 500 €/mes**; paquetes 1.000 pops 40 €, 5.000 pops 100 €; una factura Veri\*Factu ≈ 2 pops; «gobierno» extra 300 €/mes | **No público** | **No público** |
| Si la AEAT cae | «Lógica de reintento»; sin detalle de cola; términos: reparación de fallo crítico en 8 h laborables | No encontrado; los términos dicen que no garantizan disponibilidad; SLA solo Enterprise | No encontrado; webhooks y «backup de datos durante 5 años» | Comprobación de disponibilidad, retransmisión automática con marca `incident`, **facturación offline con QR local** documentada |
| Custodia de la cadena y exportación | Verifacti; `POST /verifactu/export`; **al cancelar, los XML se eliminan a los 30 días salvo el último** | Invopop; XML y PDF adjuntos por entrada; exportación masiva no encontrada | La plataforma; XML en cada respuesta | fiskaly; «el contribuyente debe descargar y guardar» al menos mensualmente por la exportación |
| Declaración responsable | Propia (Bilbabit, IdSistema «A1», solo VERI\*FACTU); dice que actúa «como componente integrado en los SIF de los CLIENTES, quienes conservan la responsabilidad del cumplimiento normativo global»; **exige que el integrador publique la suya** (dan plantilla) | Propia (Invopop, IdSistema «01», versión 1.0, firmada 16/01/2025, «exclusivamente VERI\*FACTU» aunque ahora ofrecen NO VERI\*FACTU); al integrador solo le pide **enlazar** la suya | No encontrada; la API tiene campos de sistema informático por emisor | Propia por versión; el integrador debe mostrar «una extensión de la declaración responsable» |
| Certificados | No se sube PFX; representación por NIF a Verifacti (firma remota o PDF) | No se sube PFX; acuerdo de representación por NIF | Un único certificado de la plataforma; otorgamiento por NIF | Gestionado por fiskaly; «certificado externo no habilitado» en Veri\*Factu |
| Certificado de la gestoría multi-NIF | No | No | No | No |
| NO VERI\*FACTU | No («nula demanda») | Sí, documentado | Campo sin documentar | No encontrado |
| Empresa | Getxo (Bizkaia) | Madrid, 2021, YC W23 | Ascó (Tarragona), 2024 | Viena, 2019; filial Madrid |

La AEAT **no certifica ni publica una lista de SIF**; ningún proveedor «aparece en una lista de la AEAT» salvo la de firmantes del Tipo 17 (Invopop y fiskaly figuran en la de Madrid; Verifacti se declara colaborador social).

Implicaciones para la opción C1: (a) Aserta seguiría necesitando su propia declaración responsable con Verifacti y fiskaly; (b) todos requieren un documento de representación por NIF, tramitado por su API o su portal, no el Anexo II de la gestoría; (c) el PDF sigue siendo nuestro salvo con Invopop; (d) el riesgo de dependencia es real: precios no públicos en dos de cuatro y borrado de XML tras la baja en Verifacti, así que hay que **descargar y conservar cada XML** en Aserta; (e) ninguno tiene SDK .NET, el cliente HTTP se escribe a mano.

## 8. Holded, a3 y Sage Despachos [VERIFICADO en sus webs el 2026-09-30, salvo «no encontrado»]

| | Holded | Wolters Kluwer a3 | Sage Despachos Connected |
|---|---|---|---|
| API pública | Sí: API v2 REST, «356 endpoints», Bearer con permisos por clave, documentación abierta en `holded.com/developers`; la v1 queda obsoleta | Sí: `a3developers.wolterskluwer.es/doc/` sin login para a3innuva Contabilidad, a3innuva Nómina, a3factura / a3innuva ERP y «Sistemas Informáticos de Facturación»; no para a3ERP ni a3ASESOR eco/con | **No**: en el foro oficial, Sage responde «Actualmente no hay nada a través de Api» y remite al «motor de importación» (documentación por correo) |
| Coste y plan | «The API is available on all paid plans»; planes 15 / 29 / 59 / 99 / 199 €/mes; cuota de llamadas 500 / 2.000 / 7.500 / 30.000 / 100.000 al mes y 60–600 por minuto; HTTP 429 al agotar; «Todos los planes incluyen Verifactu» | Requiere el **módulo Conectia** contratado por el cliente (precio no público); clave por producto; a3innuva Contabilidad exige además un cliente OAuth «que actualmente gestionamos internamente desde Wolters Kluwer» | n/a |
| Integradores externos | Sí, cualquier cuenta de pago; programa Solution Partners con formación gratuita; términos generales prohíben revender el servicio | Sí para un cliente concreto; partner («AppPartner», a3Marketplace certificado) solo para apps multi-cliente; proceso y coste de certificación no públicos | Sage afirma que se pueden hacer «montajes sin ser partner» con importaciones; programa Tech Partner con contacto comercial, condiciones no descargables (403) |
| Veri\*Factu vía API | Emisión sí: crear factura con `approveDoc: true` la aprueba y envía; Holded es colaborador social; **la API no devuelve QR ni estado AEAT como campos** (el QR va en el PDF) | Sí y completo en a3factura: `POST api/saleInvoices` devuelve `qrUrl`; `GET api/registeredInvoices/{id}/footPrint` (QR), `/xml`, `/sendResponse` (estado); escenarios SIF 1 (a3factura visible) y 2 (invisible, tu app asume la capa visual) | Solo interna («Facturación Certificada»); delegación desde terceros no encontrada |
| Asientos y facturas recibidas | `POST /api/v2/ledger-entries`, contactos, compras, webhooks | a3innuva Contabilidad: Diario (crear/eliminar apunte), facturas emitidas y recibidas, clientes, cobros, webhooks | No |
| Importación por fichero | Excel (facturas en borrador) | **a3ASESOR eco/con: formato `SUENLACE.DAT` documentado públicamente** (PDF «Enlace contable de entrada. Descripción de registros», registros de 256 bytes, tipos 0/1/2/3/4/9/V/B/C/A/D); a3innuva: Importia (Excel, solo clientes) | XML («Configuración > Importación/Exportación > Datos contables XML»; esquema no publicado, se obtiene exportando desde una instalación), MDB, TXT, Excel/CSV con guías |

Consecuencia directa para D-38 (formato A3 de la exportación contable): **sí existe especificación pública para a3ASESOR eco/con** (`SUENLACE.DAT`). Para a3innuva la vía es la API o Importia. Con un fichero de ejemplo de la gestoría se puede implementar el exportador A3 real en 3–5 días. [ESTIMACIÓN]

## 9. Revisión de la estimación de la página de entrevista

La tabla no venía pegada en el mensaje; la he tomado de la sección «Estimación» del artefacto «Entrevista Aserta» (actualizado el 30/09/2026). Días de una persona senior. Mis correcciones son [ESTIMACIÓN] apoyadas en lo verificado arriba.

### Opción A

| Paquete | Estimado | Mi lectura | Motivo |
|---|---|---|---|
| Cliente real AEAT (mTLS, endpoints, respuestas) | 6–10 | **Algo alto: 4–8** | Las URL ya están (sección 1); la lectura de respuestas existe; queda cargar el certificado, el manejador, las variantes «sello», el nombre del emisor en el lote y depurar contra prewww1. El cuello de botella no son días sino **tener un certificado real** |
| Certificados y representaciones | 6–10 | **Algo alto: 4–7** si no se custodian PFX; **6–10** si sí | El aviso de caducidad ya está hecho (fase 5). Si se sigue la recomendación (certificado de la gestoría + Anexo II / IZ860), queda alta de cliente, documento firmado, vía y vigencia. La adhesión al censo y los poderes son trámites del usuario, no desarrollo |
| Comprobación previa de la cadena (7.i) | 3–5 | **Alto: 0–1** | No aplica en VERI\*FACTU (art. 3 de la Orden). 0,5–1 día como defensa propia |
| Modalidad NO VERI\*FACTU | 0–15 | **Bajo: 0 o 21–37** | Firma XAdES, cadena de eventos con resumen de 6 horas, alarma, exportación, requerimiento (sección 5). El «0 si solo VERI\*FACTU» es correcto |
| Casos de factura completos | 10–18 | **Bajo: 20–33** completo, **12–20** recortado | Sección 6; el recorte depende de la cartera de la gestoría (Canarias, grupos, obras públicas) |
| Estados AEAT, subsanación, conciliación | 5–8 | **Adecuado; 6–10** si se añade `ConsultaFactuSistemaFacturacion` | La subsanación (S/N/X) tiene reglas propias; la operación de consulta permite conciliar lo aceptado con lo enviado y necesita `ConsultaLR.xsd` |
| PDF conforme, correo, plantillas | 4–7 | Adecuado | |
| Series, importación, API para terceros | 3–6 | **Algo bajo: 5–8** si la API es pública | Autenticación, claves, límites y documentación son la mitad del trabajo |
| Endurecimiento | 6–10 | Adecuado | Añadir descarga y conservación de XML propios aunque se envíen |
| Documentación, versiones, declaración responsable | 3–5 | Adecuado | El contenido mínimo está en el art. 15 de la Orden (sección 5 del informe de la investigación normativa) |
| Pruebas automáticas | 6–10 | Adecuado | Hoy 29 de Veri\*Factu y 35 de integración (el artefacto dice 6) |
| Pruebas con la AEAT en preproducción | 5–10 | Adecuado, **bloqueado** | Sin certificado cualificado real no empieza; el portal de pruebas prohíbe pruebas masivas |
| Resiliencia | 3–5 | Adecuado | |
| Piloto 1–3 clientes | 8–15 | Adecuado | Cuenta como comercializar (plazo del productor vencido el 29/07/2025) |
| Revisión externa | 3–5 | Adecuado | |
| **Total A** | **71–139** | **≈ 73–145 solo VERI\*FACTU; ≈ 94–182 con NO VERI\*FACTU** | Neto: −3 a −5 (7.i), −2 a −5 (cliente y certificados), +10 a +15 (casos), +2 a +4 (API y consulta); NO VERI\*FACTU aparte |

En días el total casi no cambia; cambia su composición: menos «fontanería» con la AEAT y más casuística fiscal. Y hay dos dependencias externas que ningún número recoge: el certificado real y, si se va por colaboración social, la adhesión al censo.

### Opción C1

| Paquete | Estimado | Mi lectura | Motivo |
|---|---|---|---|
| Integrar la API del proveedor | 8–14 | Adecuado | Sin SDK .NET en ninguno; Verifacti e Invopop tienen webhooks; los modelos de datos difieren (GOBL en Invopop) |
| Alta de NIF y representación | 4–7 | **Algo alto: 3–5** | Los cuatro tienen flujo de representación por API o portal; no es el Anexo II de la gestoría |
| Aislar o retirar código propio | 4–6 | **Algo alto: 2–4** | El módulo ya está separado (`Aserta.Verifactu` con puertos) |
| PDF, numeración, QR y estados | 3–5 | Adecuado | La numeración sigue siendo nuestra; Invopop es el único que devuelve PDF |
| Pruebas en sandbox del proveedor | 5–8 | Adecuado | |
| Resiliencia (proveedor caído, no duplicar) | 3–5 | **Algo bajo: 4–7** | Si el proveedor no responde no hay QR, luego no hay factura entregable: hay que decidir bloquear o encolar, con idempotencia; solo fiskaly documenta el modo offline |
| Piloto | 5–8 | Adecuado | |
| Seguridad, contrato, documentación | 2–4 | **Bajo: 3–6** | Falta la **declaración responsable propia** que exigen Verifacti y fiskaly, la conservación local de XML y la cláusula de salida (borrado a 30 días en Verifacti) |
| **Total C1** | **34–57** | **≈ 33–61** | Prácticamente igual. La diferencia real con A no está en los días de desarrollo sino en quién carga con la huella, el envío, la casuística y las actualizaciones de la AEAT |

### Notas sobre la sección de costes del artefacto

- Verifacti: precios confirmados (2,9 €/NIF/mes, 3.000 facturas, exceso 2 €/1.000, −10 % anual). Invopop: Pro 500 €/mes + paquetes de pops confirmados; a 2 pops por factura, 1.000 pops (40 €) son unas 500 facturas. fiskaly y verifactuapi.es: precio no público (BeeL no investigado).
- Holded API: incluida en todo plan de pago, pero la cuota del plan de 15 € (500 llamadas/mes) no sirve para una gestoría. Sage Despachos: sin API, confirmado en foro oficial. a3: portal accesible hoy, Conectia obligatorio, precio no público.
- «Cómo entrar como colaborador social»: es el **Tipo 17** para empresas de software y el **Tipo 1** para la gestoría; los modelos son los de la Resolución de 18/12/2024 (Anexos I, II, III).

## 10. Fuentes consultadas (todas el 2026-09-30)

**AEAT, oficiales**
- WSDL producción: `https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tikeV1.0/cont/ws/SistemaFacturacion.wsdl`; WSDL pruebas: mismo camino en `https://prewww2.aeat.es`.
- Índice técnico: `https://sede.agenciatributaria.gob.es/Sede/iva/sistemas-informaticos-facturacion-verifactu/informacion-tecnica.html` y sus subpáginas (wsdl-servicios-web, esquemas, disenos-registro, documento-validaciones-errores, algoritmo-calculo-codificacion-huella-hash, especificaciones-tecnicas-firma-electronica-registros-evento, caracteristicas-qr-especificaciones-servicio-cotejo-factura).
- Documentos: `Veri-Factu_Descripcion_SWeb.pdf` v1.0.3, `Validaciones_Errores_Veri-Factu.pdf` v1.2.2, `FAQs-Desarrolladores.pdf` v1.3, `DetalleEspecificacTecnCodigoQRfactura.pdf`, `DsRegistroVeriFactu.xlsx`, `errores.properties`, `SuministroInformacion.xsd` (rutas en la sección 1.5).
- Portal de pruebas externas: `https://preportal.aeat.es`.
- FAQ Veri\*Factu: `…/preguntas-frecuentes/colaboracion-social.html`, `…/cuestiones-generales-cumplimiento-delegacion.html`, `…/sistemas-verifactu.html`, `…/certificacion-sistemas-informaticos-declaracion-responsable.html`; nota de ampliación de plazo `…/nota-informativa-ampliacion-plazo-adaptacion-facturacion.html`.
- Colaboración social: `https://sede.agenciatributaria.gob.es/Sede/colaborar-agencia-tributaria/colaboracion-social-presentacion-declaraciones/tipos-acuerdo-colaboracion-social.html`, `…/relacion-entidades-acuerdo.html` (+ tipo-17 Madrid), `…/preguntas-frecuentes-sobre-colaboracion-social.html`, `…/colaboracion-social-delegada.html`; ayuda `…/ayuda/consultas-informaticas/colaboracion-social-ayuda-tecnica/alta-gestion-censo-colaboradores-sociales.html` y `…/comprobar-si-censado-colaborador-social.html`; procedimiento `https://sede.agenciatributaria.gob.es/Sede/procedimientoini/ZC01.shtml`; modelos `…/todas-gestiones/otros-servicios/colaboracion-social/colaboracion-social/modelos-autorizacion-tramites-colaboradores-sociales.html`.
- Apoderamientos: `https://sede.agenciatributaria.gob.es/Sede/procedimientoini/ZP01.shtml`, `…/colaborar-agencia-tributaria/registro-apoderamientos.html` (+ prorroga, revocacion, poderdantes-apoderados), ayuda `…/otros-servicios-ayuda-tecnica/alta-poder-tramites-tributarios.html`, relación de trámites `https://www2.agenciatributaria.gob.es/L/inwinvoc/es.aeat.dit.adu.itti.apoder.ApoderaAyudaW`, aplicación gratuita `…/aplicacion-gratuita-verifactu-aeat/acceso-identificacion.html`.
- Tipos de IVA: `https://sede.agenciatributaria.gob.es/static_files/Sede/Tema/IVA/IVA_reperc/Tipos_IVA_2026_26_02_2026.pdf`; recargo de equivalencia (Manual IVA 2024, capítulo 6); noticia RDL 18/2026 de 30/06/2026.

**BOE**
- RD 1007/2023 consolidado (última actualización 03/12/2025): `https://www.boe.es/buscar/act.php?id=BOE-A-2023-24840`.
- Orden HAC/1177/2024 consolidada (sin modificaciones): `https://www.boe.es/buscar/act.php?id=BOE-A-2024-22138` y PDF consolidado con el anexo.
- RDL 15/2025: `https://www.boe.es/buscar/act.php?id=BOE-A-2025-24446`; RD 254/2025: `…?id=BOE-A-2025-6600`.
- Resolución de 18/12/2024 (modelos de representación): `https://www.boe.es/buscar/doc.php?id=BOE-A-2024-27600`; LGT art. 92 (`BOE-A-2003-23186`); RD 1065/2007 arts. 79–81 (`BOE-A-2007-15984`); Orden HAC/1398/2003 (`BOE-A-2003-11044`).

**Microsoft**
- `https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificateloader`
- `https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0057`
- `https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.ssloptions`
- `https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory`

**Proveedores de API** (webs propias): verifacti.com (`/precios`, `/docs`, `/guia-rapida`, `/preguntas-frecuentes`, `/terminos`, declaración responsable en `storage.googleapis.com/verifacti_non_sensitive_prod/declaracion/actual.pdf`); invopop.com (`/pricing`, `/legal-documents`) y docs.invopop.com (`/guides/es-verifactu`, `/guides/es-verifactu-supplier`, `/guides/es-noverifactu`, `/compliance/spain`, `/get-started/pricing`); verifactuapi.es (`/quickstart`, `app.verifactuapi.es/docs`, blog de otorgamiento, `github.com/NemonInvocash`); fiskaly (`workspace.fiskaly.com/sign-es/…`, `developer.fiskaly.com/sign-es/…`, `fiskaly.com/signes/verifactu`, `github.com/fiskaly`).

**Holded, a3, Sage** (webs propias): `holded.com/es/desarrolladores`, `/developers/api-reference`, `/es/desarrolladores/limite-de-tasa`, `/es/precios`, `/solution-partners`, `help.holded.com` artículos 6896051, 11508249 y 7003911; `a3developers.wolterskluwer.es/doc/` (conectia, a3innuva-contabilidad, a3factura, sif), `a3marketplace.wolterskluwer.es`, `media.a3software.com/a3responde/files/2731-Enlace_contable_descripcion_registros_WEB.pdf`, `taasupportportal.wolterskluwer.com` artículos 3554295 y 3554440; `communityhub.sage.com/es/sage-despachos-connected` hilos 211812, 240411, 237485 y wiki «Verifactu - Ley antifraude», `es-kb.sage.com` soluciones 230808084344680, 230801081741393, 241108115118963, `marketplacepartners.sage.com`.

**No accesibles hoy** (403 o bloqueo): `sage.com/es-es/partners/*`, `wolterskluwer.com/es-es/solutions/conectia`, `fiskaly.com/pricing`, `support.fiskaly.com`.
