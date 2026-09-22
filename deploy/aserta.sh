#!/bin/bash
# Despliegue de Aserta en el servidor de desarrollo.
# Copia sugerida: ~/scripts/aserta.sh   (convencion de la maquina)
#
#   aserta.sh publicar        compila y publica en ~/proyectos/Aserta/publicado y reinicia el servicio
#   aserta.sh si-hay-cambios  hace fetch; si master remoto avanzo, hace pull y publica (para cron: "push a master despliega")
#   aserta.sh estado          estado del servicio y salud HTTP
#
# Requisitos una sola vez (sudo):
#   sudo cp deploy/aserta-dev.service /etc/systemd/system/ && sudo systemctl daemon-reload && sudo systemctl enable aserta-dev
#   sudo install -m 600 -o dev /dev/null /etc/aserta-dev.env  y escribir: ConnectionStrings__Aserta=Server=localhost,1433;Database=Aserta;User Id=agente_ro;Password=...;TrustServerCertificate=True;Encrypt=True
#   sudo mkdir -p /var/lib/aserta/documentos && sudo chown dev:dev /var/lib/aserta/documentos
#   permitir a dev reiniciar el servicio sin contrasena: en /etc/sudoers.d/aserta ->  dev ALL=(root) NOPASSWD: /bin/systemctl restart aserta-dev, /bin/systemctl status aserta-dev
set -euo pipefail
REPO="$HOME/proyectos/Aserta"
DESTINO="$REPO/publicado"
SERVICIO="aserta-dev"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

publicar() {
  cd "$REPO"
  echo "[aserta] publicando $(git rev-parse --short HEAD)…"
  dotnet publish src/Aserta.Web/Aserta.Web.csproj -c Release -o "$DESTINO.nuevo" --nologo -v q
  # La cadena de conexion NO esta en appsettings: viene del EnvironmentFile del servicio
  rm -rf "$DESTINO.anterior"
  [ -d "$DESTINO" ] && mv "$DESTINO" "$DESTINO.anterior"
  mv "$DESTINO.nuevo" "$DESTINO"
  sudo systemctl restart "$SERVICIO"
  for i in $(seq 1 60); do
    sleep 2
    if curl -fsS -o /dev/null http://127.0.0.1:5110/salud; then echo "[aserta] OK: servicio sano tras $((i*2))s"; return 0; fi
  done
  echo "[aserta] ERROR: el servicio no responde; revisar: journalctl -u $SERVICIO -n 100" >&2
  return 1
}

si_hay_cambios() {
  cd "$REPO"
  git fetch -q origin master
  local local_rev remote_rev
  local_rev=$(git rev-parse HEAD); remote_rev=$(git rev-parse origin/master)
  if [ "$local_rev" != "$remote_rev" ]; then
    echo "[aserta] cambios en master ($local_rev -> $remote_rev)"
    git pull -q --ff-only origin master
    publicar
  fi
}

estado() {
  systemctl status "$SERVICIO" --no-pager | head -12 || true
  curl -sS -o /dev/null -w "salud HTTP %{http_code}\n" http://127.0.0.1:5110/salud || true
}

case "${1:-}" in
  publicar) publicar ;;
  si-hay-cambios) si_hay_cambios ;;
  estado) estado ;;
  *) echo "uso: $0 publicar | si-hay-cambios | estado"; exit 1 ;;
esac
