# SGA Contabilizado · demo

Demo a medida para la gestoría SGA: web pública, área de clientes (móvil y escritorio) y mesa de trabajo del gestor, con los tres perfiles de cliente que llevan (arrendadores, autónomos y sociedades) y sus cálculos reales de IVA.

- `src/Sga.Nucleo` · dominio y cálculos puros (IVA acumulado, numeración, retención, libro de gastos, calendario fiscal).
- `src/Sga.Web` · Razor Pages + htmx + CSS propio; base SQL Server (`SGA`) o SQLite.
- `tests/Sga.Tests` · pruebas contra los oráculos anonimizados de los Excel de referencia.
- `marca/` · logo vectorial y variantes. `docs/` · decisiones, sistema de diseño, arquitectura de la información, guion de demo. `informes/` · informe de cada fase, `DUDAS.md` y capturas.
- `deploy/` · servicio systemd, nginx y script de publicación.

Arranque en desarrollo: `cd src/Sga.Web && dotnet run` (usa SQLite en `SGA/sga-demo.db` según `appsettings.Development.json`; para SQL Server, `dotnet user-secrets set "ConnectionStrings:SGA" "..."` y `Datos:Proveedor = SqlServer`). Entrada: `http://127.0.0.1:5120/acceso`.

`Recursos/` contiene documentos reales de clientes de SGA y está fuera del repositorio.
