# Despliegue de la demo SGA

La demo es una aplicación .NET 10 independiente de Aserta: su propio servicio (`sga-demo`), su propio puerto (5120), su propia URL y su propia base de datos.

## Lo que hace falta una vez (requiere sudo y DNS; el agente no puede hacerlo)
1. **DNS:** registro `A` de `sga.winsoft.es` apuntando a este servidor.
2. **Base de datos:** la demo funciona con SQLite sin ninguna administración (recomendado para la demo), o con SQL Server si se crea la base `SGA` y se da `db_owner` a `agente_ro`. Si se usa SQL Server, la cadena va en `/etc/sga-demo.env` (nunca en el repositorio) y en el `.service` se cambia `Datos__Proveedor` a `SqlServer`.
3. **Servicio:** `sudo cp deploy/sga-demo.service /etc/systemd/system/ && sudo systemctl daemon-reload && sudo systemctl enable sga-demo`
4. **nginx:** `sudo cp deploy/nginx-sga-demo.conf /etc/nginx/sites-available/sga-demo && sudo ln -s /etc/nginx/sites-available/sga-demo /etc/nginx/sites-enabled/ && sudo nginx -t && sudo systemctl reload nginx`
5. **TLS:** `sudo certbot --nginx -d sga.winsoft.es`
6. **Publicar:** `deploy/sga.sh publicar`

## Cada vez que cambie el código
`deploy/sga.sh publicar` (compila en `SGA/publicado/`, ignorado por git, y reinicia el servicio).

## Antes de enseñarla
`deploy/sga.sh reiniciar-datos` deja los datos ficticios como recién sembrados (el reloj de la demo está fijado al 6 de octubre de 2026 en `appsettings.json`, `Demo:Hoy`).

## Memoria
El servicio tiene `MemoryMax=450M`. No usa Chromium (los PDF se dibujan a mano), así que convive con la demo de Aserta.

## El día que `sga.es` apunte a este servidor (encargo 03)
El código ya está preparado: `Seo:DominioCanonico = https://sga.es`. Mientras la petición llegue por otro host (hoy `sga.winsoft.es`) la web se sirve con `noindex`, `robots.txt` lo deniega todo y no hay sitemap; en cuanto llegue por `sga.es` se indexa con normalidad. Pasos manuales, fuera del código:
1. DNS: registro `A` de `sga.es` (y `www.sga.es`) apuntando aquí.
2. nginx: bloque con `server_name sga.es` (misma configuración que `nginx-sga-demo.conf`) y un bloque que redirija `www.sga.es` → `https://sga.es` con 301. `www` no cuenta como canónico a propósito.
3. TLS: `sudo certbot --nginx -d sga.es -d www.sga.es`.
4. Search Console: pegar el código en `Seo:VerificacionGoogle` (o `Seo__VerificacionGoogle` en el `.service`), reiniciar y enviar `https://sga.es/sitemap.xml`.
Cuando SGA confirme dirección, teléfono y horario: cambiar `Sga:*` en `appsettings.json` y poner `Sga:DatosVerificados = true` para que desaparezcan las marcas `[VERIFICAR]`.
