# 06.5 · Declaración responsable del productor del sistema

> **Estado:** vigente · **Versión:** 1.0 · **Fecha:** 2026-09-22
> **Decisión asociada:** [DA-02](../decisiones-abiertas.md) — cerrada el 2026-09-22: en la demo se usan **datos ficticios en configuración**, sustituibles sin desplegar.
> `[VERIFICAR]` **obligatorio antes de cualquier uso real**: el contenido mínimo exigido, en el **RD 1007/2023** (artículo de la declaración responsable `[VERIFICAR]` número exacto) y en la **Orden HAC/1177/2024**.

---

## 1. Qué es y por qué no es una pantalla decorativa

El RSIF exige que el **productor** de un sistema informático de facturación —quien lo desarrolla y comercializa— suscriba una **declaración responsable** en la que manifiesta que el sistema cumple los requisitos reglamentarios. Es una declaración **jurídica** con consecuencias para quien la firma.

Dos cosas importan al diseñar el producto:

1. **La declaración debe estar accesible desde la propia aplicación.** No en un PDF en una carpeta: en una pantalla del sistema.
2. **Lo declarado tiene que coincidir con lo que el software dice de sí mismo** en cada registro de facturación que remite a la AEAT (bloque `SistemaInformatico`: `NIF`, `IdSistemaInformatico`, `Version`, `NumeroInstalacion` — ver [especificacion.md](especificacion.md) §3.3). Si la declaración dice versión `1.4` y los registros dicen `1.3`, hay una incoherencia detectable automáticamente por la Administración.

## 2. Contenido

Elementos que debe recoger `[VERIFICAR]` la lista literal y completa contra la norma:

| Bloque | Campo | Origen |
|---|---|---|
| Productor | Razón social | Configuración |
| Productor | NIF | Configuración |
| Productor | Domicilio | Configuración |
| Sistema | Nombre del sistema informático de facturación | Configuración (`Aserta`) |
| Sistema | **Identificador** del sistema (`IdSistemaInformatico`) | Configuración |
| Sistema | **Versión** | Configuración, **igual** a la que viaja en los registros |
| Sistema | Número de instalación | Por instalación |
| Sistema | Componentes / módulos que lo integran | Configuración |
| Sistema | Indicación de si permite sólo VERI\*FACTU o ambos modos | Constante: **sólo VERI\*FACTU** (DA-11) |
| Suscripción | Fecha y lugar | Configuración |

## 3. Diseño

### 3.1 Todo en configuración, nada en código

Sección `DeclaracionResponsable` de `appsettings`, leída con opciones tipadas. Razones:

- la declaración **cambia con cada versión** del software: si estuviera incrustada, cada corrección de texto sería un despliegue;
- permite tener datos ficticios en la demo y reales en producción **sin ramas de código**;
- el arranque puede **verificar la coherencia** entre la declaración y el bloque `SistemaInformatico` que se usará en los registros, y **abortar** si no coinciden. Esa comprobación es la razón principal de que ambos valores salgan del mismo sitio.

### 3.2 Pantalla

- Ruta pública dentro de la aplicación, accesible **sin autenticación** y enlazada desde el pie y desde el módulo de facturación.
- Legible e imprimible; el PDF se genera con la misma cola de Playwright que las facturas.
- **En la demo, un aviso destacado e imposible de pasar por alto:** *"Datos de demostración. Este sistema no está en producción y esta declaración no tiene validez."* Una declaración responsable con datos inventados y sin ese aviso es un documento que aparenta lo que no es, y eso no se enseña a un cliente.

### 3.3 Versionado

Cada versión del software tiene su declaración. Se conserva el **histórico** (tabla `vf.DeclaracionResponsableHistorico`: versión, fecha de suscripción, contenido, hash), de modo que dada una factura de hace dos años se pueda mostrar la declaración vigente cuando se emitió. Es la misma lógica de inalterabilidad que rige todo el módulo.

## 4. Definición de hecho

1. La pantalla renderiza todos los campos y es accesible sin autenticación.
2. Test: si `Version` de la declaración y la del bloque `SistemaInformatico` difieren, **la aplicación no arranca**.
3. Test: en entorno distinto de producción, la pantalla muestra el aviso de datos de demostración.
4. El histórico conserva la declaración de cada versión desplegada.

## 5. Riesgos y mejoras sugeridas

| # | Riesgo | Propuesta |
|---|---|---|
| D1 | **Olvidar sustituir los datos ficticios** antes de un uso real | Punto de control explícito en el checklist de despliegue de `12-infraestructura-despliegue.md`, y comprobación al arrancar: si el entorno es `Production` y los datos son los de demostración, **no arranca** |
| D2 | La versión declarada se desincroniza de la desplegada | Ambas salen del mismo origen y se comprueban al arrancar (§4.2) |
| D3 | El contenido mínimo exigido no está verificado contra la norma | `[VERIFICAR]` marcado en cabecera. Es el único fichero de esta carpeta cuyo **contenido normativo** no se ha podido contrastar contra documentación oficial de la AEAT, porque la AEAT publica el detalle técnico pero el contenido de la declaración está en la norma |
| D4 | Quién firma la declaración es una decisión con consecuencias legales | Ligada a DA-02. Debe revisarla la misma persona que valide el [ADR-002](../adr/ADR-002-certificado-y-representacion-verifactu.md) |
