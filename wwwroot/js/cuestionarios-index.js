document.addEventListener("DOMContentLoaded", function () {
    const contenedor = document.getElementById("cuestionariosMovil");

    if (!contenedor) {
        return;
    }

    const botonBusqueda = contenedor.querySelector("[data-mostrar-busqueda-cuestionarios]");
    const panelBusqueda = contenedor.querySelector("[data-contenedor-busqueda-cuestionarios]");
    const entradaBusqueda = document.getElementById("buscarCuestionarioMovil");
    const tarjetas = Array.from(contenedor.querySelectorAll(".cuestionario-movil-item"));
    const botonAnterior = contenedor.querySelector("[data-anterior-cuestionarios-movil]");
    const botonSiguiente = contenedor.querySelector("[data-siguiente-cuestionarios-movil]");
    const contador = contenedor.querySelector("[data-contador-cuestionarios-movil]");
    const mensajeVacio = contenedor.querySelector("[data-sin-cuestionarios-movil]");
    const registrosPorPagina = 10;
    let paginaActual = 1;

    if (!botonBusqueda || !panelBusqueda || !entradaBusqueda || !botonAnterior ||
        !botonSiguiente || !contador || !mensajeVacio) {
        return;
    }

    function normalizar(texto) {
        return (texto || "").toLocaleLowerCase("es").normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "");
    }
    function actualizarListado() {
        const termino = normalizar(entradaBusqueda.value.trim());
        const filtradas = tarjetas.filter(function (tarjeta) {
            return normalizar(tarjeta.dataset.busqueda).includes(termino);
        });
        const totalPaginas = Math.max(1, Math.ceil(filtradas.length / registrosPorPagina));
        paginaActual = Math.min(paginaActual, totalPaginas);
        tarjetas.forEach(function (tarjeta) {
            tarjeta.classList.add("d-none");
        });
        filtradas.slice(
            (paginaActual - 1) * registrosPorPagina,
            paginaActual * registrosPorPagina
        ).forEach(function (tarjeta) {
            tarjeta.classList.remove("d-none");
        });
        mensajeVacio.classList.toggle("d-none", filtradas.length !== 0);
        contador.textContent = filtradas.length === 0
            ? "0 registros"
            : `Página ${paginaActual} de ${totalPaginas} · ${filtradas.length} registros`;
        botonAnterior.disabled = paginaActual === 1;
        botonSiguiente.disabled = paginaActual === totalPaginas;
    }
    botonBusqueda.addEventListener("click", function () {
        const abrir = panelBusqueda.classList.contains("d-none");
        panelBusqueda.classList.toggle("d-none", !abrir);
        botonBusqueda.setAttribute("aria-expanded", String(abrir));
        if (abrir) entradaBusqueda.focus();
    });
    entradaBusqueda.addEventListener("input", function () {
        paginaActual = 1;
        actualizarListado();
    });
    botonAnterior.addEventListener("click", function () {
        if (paginaActual > 1) { paginaActual--; actualizarListado(); }
    });
    botonSiguiente.addEventListener("click", function () {
        if (!botonSiguiente.disabled) { paginaActual++; actualizarListado(); }
    });
    actualizarListado();
});
