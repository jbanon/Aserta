#!/bin/bash
# Publica y reinicia la demo SGA. Uso: deploy/sga.sh publicar | reiniciar-datos | estado
set -e
RAIZ=/home/dev/proyectos/Aserta/SGA
case "${1:-estado}" in
  publicar)
    cd "$RAIZ" && dotnet publish src/Sga.Web -c Release -o publicado -nologo -v q
    sudo mkdir -p /var/lib/sga-demo/documentos && sudo chown -R dev:dev /var/lib/sga-demo
    sudo systemctl restart sga-demo && sleep 3 && curl -s http://127.0.0.1:5120/salud && echo ;;
  reiniciar-datos)
    # Vacia y vuelve a sembrar los datos ficticios (no borra la base en SQL Server; en SQLite borra el fichero)
    sudo systemctl stop sga-demo
    cd "$RAIZ/publicado" && Demo__Reiniciar=true ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://127.0.0.1:5121 Datos__Proveedor=${Datos__Proveedor:-Sqlite} Datos__Sqlite=/var/lib/sga-demo/sga-demo.db Demo__Almacen=/var/lib/sga-demo/documentos timeout 60 dotnet Sga.Web.dll >/dev/null 2>&1 || true
    sudo systemctl start sga-demo ;;
  estado)
    systemctl status sga-demo --no-pager | head -5; curl -s http://127.0.0.1:5120/salud; echo ;;
esac
