/* =============================================================================
   Aserta · app.js (ADR-003 §2.3)
   - htmx sin eval (CSP estricta): allowEval = false, nada de hx-on:.
   - Antiforgery global: la cabecera RequestVerificationToken en cada peticion htmx.
   - Region aria-live para anunciar resultados.
   - Tema claro/oscuro con data-tema en <html>.
   - Menus desplegables accesibles (sin librerias).
   ============================================================================= */
(function () {
  "use strict";

  // --- Tema -------------------------------------------------------------------
  const CLAVE_TEMA = "aserta.tema";
  function aplicarTemaGuardado() {
    try {
      const t = localStorage.getItem(CLAVE_TEMA);
      if (t === "claro" || t === "oscuro") document.documentElement.dataset.tema = t;
    } catch (_) { /* almacenamiento no disponible */ }
  }
  aplicarTemaGuardado();

  function alternarTema() {
    const html = document.documentElement;
    const oscuroSistema = window.matchMedia("(prefers-color-scheme: dark)").matches;
    const actual = html.dataset.tema || (oscuroSistema ? "oscuro" : "claro");
    const nuevo = actual === "oscuro" ? "claro" : "oscuro";
    html.dataset.tema = nuevo;
    try { localStorage.setItem(CLAVE_TEMA, nuevo); } catch (_) { }
    anunciar("Tema " + nuevo + " activado");
  }

  // --- Anuncios para lectores de pantalla ------------------------------------------
  function anunciar(texto) {
    const region = document.getElementById("anuncios");
    if (!region) return;
    region.textContent = "";
    window.setTimeout(function () { region.textContent = texto; }, 30);
  }
  window.aserta = window.aserta || {};
  window.aserta.anunciar = anunciar;

  // --- htmx: configuracion global -----------------------------------------------------
  function tokenAntiforgery() {
    const campo = document.querySelector('input[name="__RequestVerificationToken"]');
    return campo ? campo.value : null;
  }

  document.addEventListener("DOMContentLoaded", function () {
    if (window.htmx) {
      htmx.config.allowEval = false;
      htmx.config.includeIndicatorStyles = false;
      htmx.config.defaultSwapStyle = "outerHTML";
      htmx.config.scrollBehavior = "instant";
    }

    document.body.addEventListener("htmx:configRequest", function (e) {
      const token = tokenAntiforgery();
      if (token) e.detail.headers["RequestVerificationToken"] = token;
    });

    document.body.addEventListener("htmx:responseError", function (e) {
      const estado = e.detail.xhr ? e.detail.xhr.status : 0;
      const mensaje = estado === 400 ? "La acción no se ha podido realizar." :
                      estado === 403 ? "No tiene permiso para esta acción." :
                      "Error del servidor (" + estado + "). Inténtelo de nuevo.";
      mostrarAviso(mensaje, "error");
    });

    document.body.addEventListener("htmx:sendError", function () {
      mostrarAviso("Sin conexión con el servidor.", "error");
    });

    // Un fragmento puede pedir un anuncio con la cabecera X-Aserta-Anuncio
    document.body.addEventListener("htmx:afterRequest", function (e) {
      const xhr = e.detail.xhr;
      if (!xhr) return;
      const texto = xhr.getResponseHeader("X-Aserta-Anuncio");
      if (texto) { anunciar(decodeURIComponent(texto)); mostrarAviso(decodeURIComponent(texto), "ok"); }
    });

    // Boton de tema
    document.querySelectorAll("[data-accion='alternar-tema']").forEach(function (b) {
      b.addEventListener("click", alternarTema);
    });

    // Confirmaciones sin hx-confirm (que usa eval): data-confirmar="texto"
    document.body.addEventListener("submit", function (e) {
      const form = e.target;
      if (form instanceof HTMLFormElement && form.dataset.confirmar && !window.confirm(form.dataset.confirmar)) e.preventDefault();
    });
    document.body.addEventListener("htmx:confirm", function (e) {
      const el = e.detail.elt;
      const texto = el && el.dataset ? el.dataset.confirmar : null;
      if (!texto) return;
      e.preventDefault();
      if (window.confirm(texto)) e.detail.issueRequest(true);
    });

    // Enfocar el primer campo con error
    const primerError = document.querySelector(".input-validation-error");
    if (primerError) primerError.focus();

    inicializarMenus(document);
    inicializarFilasEnlace(document);
  });

  document.body.addEventListener("htmx:afterSettle", function (e) {
    inicializarMenus(e.target);
    inicializarFilasEnlace(e.target);
  });

  // --- Avisos flotantes ----------------------------------------------------------------
  function mostrarAviso(texto, tipo) {
    let cont = document.getElementById("avisos-flotantes");
    if (!cont) {
      cont = document.createElement("div");
      cont.id = "avisos-flotantes";
      cont.className = "avisos-flotantes";
      document.body.appendChild(cont);
    }
    const el = document.createElement("div");
    el.className = "alerta alerta-" + (tipo || "info");
    el.setAttribute("role", "status");
    el.textContent = texto;
    cont.appendChild(el);
    window.setTimeout(function () { el.remove(); }, 4500);
  }
  window.aserta.mostrarAviso = mostrarAviso;

  // --- Menus desplegables accesibles ------------------------------------------------------
  function inicializarMenus(raiz) {
    raiz.querySelectorAll("[data-menu]").forEach(function (menu) {
      if (menu.dataset.menuListo) return;
      menu.dataset.menuListo = "1";
      const boton = menu.querySelector("[data-menu-boton]");
      const lista = menu.querySelector("[data-menu-lista]");
      if (!boton || !lista) return;

      function cerrar() { lista.hidden = true; boton.setAttribute("aria-expanded", "false"); }
      function abrir() {
        document.querySelectorAll("[data-menu-lista]:not([hidden])").forEach(function (l) { l.hidden = true; });
        lista.hidden = false;
        boton.setAttribute("aria-expanded", "true");
        const primero = lista.querySelector("button:not(:disabled), a");
        if (primero) primero.focus();
      }
      boton.addEventListener("click", function (e) { e.stopPropagation(); lista.hidden ? abrir() : cerrar(); });
      menu.addEventListener("keydown", function (e) {
        if (e.key === "Escape") { cerrar(); boton.focus(); }
        if (e.key === "ArrowDown" || e.key === "ArrowUp") {
          const items = Array.from(lista.querySelectorAll("button:not(:disabled), a"));
          if (!items.length) return;
          e.preventDefault();
          const i = items.indexOf(document.activeElement);
          const siguiente = e.key === "ArrowDown" ? (i + 1) % items.length : (i - 1 + items.length) % items.length;
          items[siguiente].focus();
        }
      });
      document.addEventListener("click", function (e) { if (!menu.contains(e.target)) cerrar(); });
    });
  }

  // --- Filas de tabla que navegan (data-href) -----------------------------------------------
  function inicializarFilasEnlace(raiz) {
    raiz.querySelectorAll("tr[data-href]").forEach(function (fila) {
      if (fila.dataset.enlaceListo) return;
      fila.dataset.enlaceListo = "1";
      fila.classList.add("fila-enlace");
      fila.addEventListener("click", function (e) {
        if (e.target.closest("a, button, input, select, label")) return;
        window.location.href = fila.dataset.href;
      });
    });
  }
})();
