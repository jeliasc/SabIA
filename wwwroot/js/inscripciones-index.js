document.addEventListener("DOMContentLoaded", function () {
    inicializarListado();
});

function inicializarListado() {
    const contenedor = document.getElementById("inscripcionesMovil");
    if (!contenedor) return;

    const boton = contenedor.querySelector("[data-mostrar-busqueda-inscripciones]");
    const panel = contenedor.querySelector("[data-contenedor-busqueda-inscripciones]");
    const entrada = document.getElementById("buscarInscripcionMovil");
    const tarjetas = Array.from(contenedor.querySelectorAll(".inscripcion-movil-item"));
    const anterior = contenedor.querySelector("[data-anterior-inscripciones-movil]");
    const siguiente = contenedor.querySelector("[data-siguiente-inscripciones-movil]");
    const contador = contenedor.querySelector("[data-contador-inscripciones-movil]");
    const vacio = contenedor.querySelector("[data-sin-inscripciones-movil]");
    let pagina = 1;

    if (!boton || !panel || !entrada || !anterior || !siguiente || !contador || !vacio) return;
    const normalizar = texto => (texto || "").toLocaleLowerCase("es")
        .normalize("NFD").replace(/[\u0300-\u036f]/g, "");
    function actualizar() {
        const filtradas = tarjetas.filter(tarjeta =>
            normalizar(tarjeta.dataset.busqueda).includes(normalizar(entrada.value.trim()))
        );
        const total = Math.max(1, Math.ceil(filtradas.length / 10));
        pagina = Math.min(pagina, total);
        tarjetas.forEach(tarjeta => tarjeta.classList.add("d-none"));
        filtradas.slice((pagina - 1) * 10, pagina * 10)
            .forEach(tarjeta => tarjeta.classList.remove("d-none"));
        vacio.classList.toggle("d-none", filtradas.length !== 0);
        contador.textContent = filtradas.length ?
            `Página ${pagina} de ${total} · ${filtradas.length} registros` : "0 registros";
        anterior.disabled = pagina === 1;
        siguiente.disabled = pagina === total;
    }
    boton.addEventListener("click", () => {
        const abrir = panel.classList.contains("d-none");
        panel.classList.toggle("d-none", !abrir);
        boton.setAttribute("aria-expanded", String(abrir));
        if (abrir) entrada.focus();
    });
    entrada.addEventListener("input", () => { pagina = 1; actualizar(); });
    anterior.addEventListener("click", () => {
        if (pagina > 1) { pagina--; actualizar(); }
    });
    siguiente.addEventListener("click", () => {
        if (!siguiente.disabled) { pagina++; actualizar(); }
    });
    actualizar();
}
