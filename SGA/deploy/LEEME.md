# Despliegue de la demo SGA

La demo es una aplicación .NET 10 independiente de Aserta: su propio servicio (`sga-demo`), su propio puerto (5120), su propia URL y su propia base de datos.

## Lo que hace falta una vez (requiere sudo y DNS; el agente no puede hacerlo)
1. **DNS:** registro `A` de `sga-demo.winsoft.es` apuntando a este servidor.
2. **Base de datos:** la demo funciona con SQLite sin ninguna administración (recomendado para la demo), o con SQL Server si se crea la base `SGA` y se da `db_owner` a `agente_ro`. Si se usa SQL Server, la cadena va en `/etc/sga-demo.env` (nunca en el repositorio) y en el `.service` se cambia `Datos__Proveedor` a `SqlServer`.
3. **Servicio:** `sudo cp deploy/sga-demo.service /etc/systemd/system/ && sudo systemctl daemon-reload && sudo systemctl enable sga-demo`
4. **nginx:** `sudo cp deploy/nginx-sga-demo.conf /etc/nginx/sites-available/sga-demo && sudo ln -s /etc/nginx/sites-available/sga-demo /etc/nginx/sites-enabled/ && sudo nginx -t && sudo systemctl reload nginx`
5. **TLS:** `sudo certbot --nginx -d sga-demo.winsoft.es`
6. **Publicar:** `deploy/sga.sh publicar`

## Cada vez que cambie el código
`deploy/sga.sh publicar` (compila en `SGA/publicado/`, ignorado por git, y reinicia el servicio).

## Antes de enseñarla
`deploy/sga.sh reiniciar-datos` deja los datos ficticios como recién sembrados (el reloj de la demo está fijado al 6 de octubre de 2026 en `appsettings.json`, `Demo:Hoy`).

## Memoria
El servicio tiene `MemoryMax=450M`. No usa Chromium (los PDF se dibujan a mano), así que convive con la demo de Aserta.
