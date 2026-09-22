# 04 · Segundo factor (TOTP) y página de cuenta

> **Fase 1 · tarea 4** · Agente Programador · 2026-09-22

## Qué he construido

Enrolamiento del segundo factor con aplicación autenticadora (clave + QR + confirmación con un código), paso de verificación en el login, códigos de recuperación de un solo uso, página "Mi cuenta" (estado del MFA, regenerar códigos, desactivar, cambiar contraseña) y un middleware que obliga a configurar el MFA a los usuarios con `MfaObligatorio` antes de dejarles usar nada más.

## Decisiones que he tomado yo

1. **TOTP con el proveedor `Authenticator` de Identity**, sin SMS ni correo (el `INotificador` es una bandeja en pantalla en la demo; un segundo factor por correo simulado no sería un segundo factor).
2. **QR con QRCoder** (paquete puro C#, sin dependencias nativas) como `data:` URI PNG; la CSP ya permitía `img-src data:`. El helper `GeneradorQr` se reutilizará para el QR de las facturas en la fase 4.
3. **"Recordar este equipo" durante 30 días** (cookie de Identity `TwoFactorRememberMe`) opcional en la verificación.
4. **8 códigos de recuperación**, mostrados una sola vez al activar; se pueden regenerar desde "Mi cuenta". Cuenta de restantes visible.
5. **`MfaObligatorio` se aplica por middleware** leyendo dos claims de la cookie (`aserta:mfa` y `aserta:mfa-activo`, este último añadido en `FabricaClaims`); al activar el MFA se hace `RefreshSignInAsync` para renovar los claims sin cerrar sesión. Un usuario obligado no puede desactivarlo (la página lo oculta y el handler devuelve 403).
6. **Autorización de `/Cuenta`**: he cambiado `AllowAnonymousToFolder("/Cuenta")` por `AllowAnonymousToPage` para Entrar, Salir, Denegado y Mfa/Verificar; `Perfil` y `Mfa/Configurar` exigen sesión por la `FallbackPolicy`. Detectado porque `AllowAnonymous` de carpeta prevalece sobre `AuthorizePage`.
7. **Cada evento queda en auditoría**: activación, desactivación, regeneración de códigos, cambio de contraseña, inicio de sesión con MFA (con marca de si fue por código de recuperación).
8. **Bloqueo por intentos** también en el segundo factor (`lockoutOnFailure` implícito en `TwoFactorAuthenticatorSignInAsync`).

## Desviaciones

- No hay flujo de "he perdido el móvil y los códigos": lo resuelve el socio desde Usuarios… todavía no. Falta un botón "Restablecer segundo factor" para el `SocioDirector` (añadido a huecos).
- `Usuarios/Alta` permite marcar `MfaObligatorio`; no hay edición posterior de ese flag.

## Huecos encontrados

- La documentación no dice si el MFA debe ser obligatorio por rol (p. ej. siempre para `SocioDirector`) o por usuario. He dejado el interruptor por usuario, que es lo que modela `dbo.Usuario.MfaObligatorio`.

## Cómo probarlo

1. Entrar como `carlos@demo.aserta.local`, pulsar el nombre en la cabecera → **Mi cuenta** → **Activar segundo factor**. Escanear el QR con Google/Microsoft Authenticator (o introducir la clave a mano) y confirmar con el código. Guardar los códigos de recuperación.
2. Cerrar sesión y volver a entrar: tras la contraseña pide el código. Probar uno incorrecto (mensaje), uno correcto (entra), y "usar un código de recuperación".
3. En **Mi cuenta**: restantes 7, regenerar, desactivar (queda auditado en **Auditoría** con acción `Configuracion`).
4. Obligatoriedad: como socio, crear un usuario con "Exigir segundo factor"; al entrar con él, cualquier página redirige a la configuración hasta activarlo.

Flujo completo verificado con un script (enrolar, código malo, código bueno, código de recuperación, regenerar, desactivar, obligatoriedad); sin excepciones en el log.

## Estado de los tests

- No hay test automatizado del flujo MFA (requiere generar TOTP en el test; es factible con `Rfc6238AuthenticationService` interno de Identity o una implementación propia de 20 líneas). Queda pendiente; la verificación ha sido con script externo.
- Toda la suite sigue en verde: 67 dominio + 21 integración.
