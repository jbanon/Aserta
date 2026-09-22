# Despliegue en el servidor de desarrollo

Estado: **ficheros preparados, instalación pendiente** (requiere `sudo`, que el Agente Programador no tiene).

| Fichero | Destino | Qué hace |
|---|---|---|
| `aserta-dev.service` | `/etc/systemd/system/aserta-dev.service` | Servicio Kestrel en `127.0.0.1:5110`, `MemoryMax=700M`, `TimeoutStartSec=180` (runner de migraciones) |
| `nginx-aserta-dev.conf` | `/etc/nginx/sites-available/aserta-dev` | Proxy inverso. Subdominio pendiente (DA-12) |
| `aserta.sh` | `~/scripts/aserta.sh` | `publicar`, `si-hay-cambios` (para cron), `estado` |

## Pasos una sola vez

```bash
sudo cp deploy/aserta-dev.service /etc/systemd/system/
sudo install -m 600 -o dev /dev/null /etc/aserta-dev.env
#   contenido de /etc/aserta-dev.env (una línea, con la contraseña de usuarioBD.md):
#   ConnectionStrings__Aserta=Server=localhost,1433;Database=Aserta;User Id=agente_ro;Password=...;TrustServerCertificate=True;Encrypt=True
sudo mkdir -p /var/lib/aserta/documentos && sudo chown dev:dev /var/lib/aserta/documentos
sudo systemctl daemon-reload && sudo systemctl enable aserta-dev
echo 'dev ALL=(root) NOPASSWD: /bin/systemctl restart aserta-dev, /bin/systemctl status aserta-dev' | sudo tee /etc/sudoers.d/aserta
cp deploy/aserta.sh ~/scripts/aserta.sh && chmod +x ~/scripts/aserta.sh
~/scripts/aserta.sh publicar
```

## "Un push a master despliega en desarrollo"

No hay receptor de webhooks en la máquina. La forma más simple y sin servicios extra es un cron que sondea el remoto:

```
*/2 * * * * ~/scripts/aserta.sh si-hay-cambios >> ~/scripts/aserta.log 2>&1
```

Cuando el subdominio esté decidido: `sudo cp deploy/nginx-aserta-dev.conf /etc/nginx/sites-available/aserta-dev`, editar `server_name`, enlazar en `sites-enabled`, `sudo nginx -t && sudo systemctl reload nginx`, y `sudo certbot --nginx -d <subdominio>`.
