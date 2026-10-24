document.addEventListener("DOMContentLoaded", function () {
    inicializar("seccionesMovil", {
        botonBusqueda: "[data-mostrar-busqueda-secciones]",
        panelBusqueda: "[data-contenedor-busqueda-secciones]",
        entradaBusqueda: "buscarSeccionMovil",
        tarjetas: ".seccion-movil-item",
        anterior: "[data-anterior-secciones-movil]",
        siguiente: "[data-siguiente-secciones-movil]",
        contador: "[data-contador-secciones-movil]",
        vacio: "[data-sin-secciones-movil]"
    });
});

function inicializar(idContenedor, selectores) {
    const contenedor = document.getElementById(idContenedor);
    if (!contenedor) return;

    const boton = contenedor.querySelector(selectores.botonBusqueda);
    const panel = contenedor.querySelector(selectores.panelBusqueda);
    const entrada = document.getElementById(selectores.entradaBusqueda);
    const tarjetas = Array.from(contenedor.querySelectorAll(selectores.tarjetas));
    const anterior = contenedor.querySelector(selectores.anterior);
    const siguiente = contenedor.querySelector(selectores.siguiente);
    const contador = contenedor.querySelector(selectores.contador);
    const vacio = contenedor.querySelector(selectores.vacio);
    let pagina = 1;

    if (!boton || !panel || !entrada || !anterior || !siguiente || !contador || !vacio) return;

    function normalizar(texto) {
        return (texto || "").toLocaleLowerCase("es").normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "");
    }
    function actualizar() {
        const filtradas = tarjetas.filter(function (tarjeta) {
            return normalizar(tarjeta.dataset.busqueda).includes(normalizar(entrada.value.trim()));
        });
        const total = Math.max(1, Math.ceil(filtradas.length / 10));
        pagina = Math.min(pagina, total);
        tarjetas.forEach(function (tarjeta) { tarjeta.classList.add("d-none"); });
        filtradas.slice((pagina - 1) * 10, pagina * 10).forEach(function (tarjeta) {
            tarjeta.classList.remove("d-none");
        });
        vacio.classList.toggle("d-none", filtradas.length !== 0);
        contador.textContent = filtradas.length === 0 ? "0 registros" :
            `Página ${pagina} de ${total} · ${filtradas.length} registros`;
        anterior.disabled = pagina === 1;
        siguiente.disabled = pagina === total;
    }

    boton.addEventListener("click", function () {
        const abrir = panel.classList.contains("d-none");
        panel.classList.toggle("d-none", !abrir);
        boton.setAttribute("aria-expanded", String(abrir));
        if (abrir) entrada.focus();
    });
    entrada.addEventListener("input", function () { pagina = 1; actualizar(); });
    anterior.addEventListener("click", function () {
        if (pagina > 1) { pagina--; actualizar(); }
    });
    siguiente.addEventListener("click", function () {
        if (!siguiente.disabled) { pagina++; actualizar(); }
    });
    actualizar();
}
