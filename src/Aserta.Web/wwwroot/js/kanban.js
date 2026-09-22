/* =============================================================================
   Aserta · Kanban (ADR-003 §2.3): SortableJS emite el movimiento, el servidor
   valida la transicion y devuelve la tarjeta repintada. Si la rechaza, la tarjeta
   vuelve a su columna con mensaje visible y anunciado. Sin eval, sin inline.
   ============================================================================= */
(function () {
  "use strict";
  const kanban = document.getElementById("kanban");
  if (!kanban || !window.Sortable) return;
  const urlMover = kanban.dataset.urlMover;

  function token() { const c = document.querySelector('input[name="__RequestVerificationToken"]'); return c ? c.value : ""; }
  function columna(estado) { return kanban.querySelector('.kanban-lista[data-columna="' + estado + '"]'); }
  function recontar() {
    kanban.querySelectorAll(".kanban-lista").forEach(function (l) {
      const c = kanban.querySelector('[data-contador="' + l.dataset.columna + '"]');
      if (c) c.textContent = l.children.length;
    });
  }

  // Coloca una tarjeta (recien repintada por el servidor) en la columna que dice su data-estado
  function recolocar(tarjeta) {
    const destino = columna(tarjeta.dataset.estado);
    if (destino && tarjeta.parentElement !== destino) destino.prepend(tarjeta);
    recontar();
    if (window.htmx) htmx.process(tarjeta);
  }

  // --- Arrastrar y soltar --------------------------------------------------------------
  kanban.querySelectorAll(".kanban-lista").forEach(function (lista) {
    new Sortable(lista, {
      group: "kanban",
      animation: 150,
      ghostClass: "tarjeta-fantasma",
      dragClass: "tarjeta-arrastrando",
      filter: "button, a, [data-menu-lista]",
      preventOnFilter: false,
      onEnd: function (e) {
        const tarjeta = e.item, origen = e.from, destino = e.to;
        const estadoDestino = destino.dataset.columna;
        if (origen === destino && e.oldIndex === e.newIndex) return;
        recontar();

        const datos = new FormData();
        datos.append("id", tarjeta.dataset.id);
        datos.append("destino", estadoDestino);
        datos.append("orden", e.newIndex);
        datos.append("__RequestVerificationToken", token());

        fetch(urlMover, { method: "POST", body: datos, headers: { "HX-Request": "true", "RequestVerificationToken": token() }, credentials: "same-origin" })
          .then(function (r) { return r.text().then(function (t) { return { ok: r.ok, status: r.status, texto: t, anuncio: r.headers.get("X-Aserta-Anuncio") }; }); })
          .then(function (r) {
            if (r.ok) {
              const tmp = document.createElement("template");
              tmp.innerHTML = r.texto.trim();
              const nueva = tmp.content.firstElementChild;
              tarjeta.replaceWith(nueva);
              recolocar(nueva);
              if (r.anuncio) window.aserta.anunciar(decodeURIComponent(r.anuncio));
            } else {
              // Rechazada por el servidor: vuelve a su sitio
              const ref = origen.children[e.oldIndex] || null;
              origen.insertBefore(tarjeta, ref);
              recontar();
              const msg = r.status === 400 && r.texto ? r.texto : "No se ha podido mover la tarjeta (" + r.status + ").";
              window.aserta.mostrarAviso(msg, "error");
              window.aserta.anunciar(msg);
              tarjeta.classList.add("tarjeta-rechazada");
              setTimeout(function () { tarjeta.classList.remove("tarjeta-rechazada"); }, 1200);
            }
          })
          .catch(function () {
            origen.insertBefore(tarjeta, origen.children[e.oldIndex] || null);
            recontar();
            window.aserta.mostrarAviso("Sin conexión con el servidor; la tarjeta no se ha movido.", "error");
          });
      }
    });
  });

  // --- Menu "Mover a…" (htmx): tras el swap, la tarjeta nueva se recoloca en su columna ----
  document.body.addEventListener("htmx:afterSwap", function (e) {
    const t = e.detail.target;
    if (t && t.classList && t.classList.contains("tarjeta")) { recolocar(t); t.focus(); }
    // hx-swap=outerHTML: el elemento nuevo es el siguiente hermano del target antiguo en algunos navegadores
    const nuevas = kanban.querySelectorAll(".tarjeta");
    nuevas.forEach(function (n) { if (n.parentElement && n.parentElement.dataset.columna !== n.dataset.estado) recolocar(n); });
  });
  document.body.addEventListener("htmx:responseError", function (e) {
    const t = e.detail.target;
    if (t && t.classList && t.classList.contains("tarjeta")) t.focus();
  });

  // --- Dialogo de detalle -------------------------------------------------------------
  const dialogo = document.getElementById("dialogo-obligacion");
  if (dialogo) {
    document.body.addEventListener("htmx:afterSwap", function (e) {
      if (e.detail.target && e.detail.target.id === "dialogo-contenido" && !dialogo.open) dialogo.showModal();
    });
    dialogo.addEventListener("click", function (e) {
      if (e.target === dialogo || e.target.closest("[data-cerrar-dialogo]")) dialogo.close();
    });
    kanban.addEventListener("keydown", function (e) {
      const tarjeta = e.target.closest && e.target.closest(".tarjeta");
      if (!tarjeta || e.target !== tarjeta) return;
      if (e.key === "Enter") { const b = tarjeta.querySelector("[data-detalle]"); if (b) b.click(); }
      if (e.key === "m" || e.key === "M") { const b = tarjeta.querySelector("[data-menu-boton]"); if (b) b.click(); }
    });
  }
})();
