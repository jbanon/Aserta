// SGA · comportamiento minimo sin dependencias: menu movil, lateral plegable, htmx con antifalsificacion, previsualizacion de fotos.
(function () {
  const $ = (s, r) => (r || document).querySelector(s);
  const $$ = (s, r) => Array.from((r || document).querySelectorAll(s));

  // Menu de la web publica
  const botonMenu = $("[data-menu]");
  if (botonMenu) botonMenu.addEventListener("click", () => {
    const menu = $("#menu-movil"); const abierto = menu.classList.toggle("abierto");
    botonMenu.setAttribute("aria-expanded", abierto ? "true" : "false");
  });

  // Lateral plegable en escritorio (se recuerda en este navegador)
  const app = $(".app");
  const plegar = $("[data-plegar]");
  try { if (app && localStorage.getItem("sga.lateral") === "plegado") app.classList.add("plegado"); } catch (_) {}
  if (plegar && app) plegar.addEventListener("click", () => {
    const plegado = app.classList.toggle("plegado");
    plegar.setAttribute("aria-expanded", plegado ? "false" : "true");
    try { localStorage.setItem("sga.lateral", plegado ? "plegado" : "abierto"); } catch (_) {}
  });

  // htmx: token antifalsificacion en cada peticion
  document.body.addEventListener("htmx:configRequest", (e) => {
    const t = $("input[name='__RequestVerificationToken']");
    if (t) e.detail.headers["RequestVerificationToken"] = t.value;
  });

  // Subida: nombre y previsualizacion de la foto elegida
  $$("input[type=file][data-previsualizar]").forEach((input) => {
    input.addEventListener("change", () => {
      const f = input.files && input.files[0]; if (!f) return;
      const destino = $(input.dataset.previsualizar);
      if (!destino) return;
      destino.hidden = false;
      const nombre = $("[data-nombre]", destino); if (nombre) nombre.textContent = f.name + " · " + Math.round(f.size / 1024) + " KB";
      const img = $("img", destino);
      if (img && f.type.startsWith("image/")) { img.src = URL.createObjectURL(f); img.hidden = false; } else if (img) img.hidden = true;
      const enviar = $("[data-enviar]"); if (enviar) enviar.disabled = false;
    });
  });

  // Confirmaciones sencillas
  $$("[data-confirmar]").forEach((el) => el.addEventListener("click", (e) => { if (!confirm(el.dataset.confirmar)) e.preventDefault(); }));

  // Anuncios accesibles tras acciones htmx
  document.body.addEventListener("htmx:afterOnLoad", (e) => {
    const a = e.detail.xhr && e.detail.xhr.getResponseHeader("X-Sga-Anuncio");
    if (a) { const zona = $("#anuncios"); if (zona) zona.textContent = decodeURIComponent(a); }
  });
})();
