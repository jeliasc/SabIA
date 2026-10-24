document.addEventListener("DOMContentLoaded", function () {
    const contenedor = document.getElementById("historialCiclosMovil");
    if (!contenedor) return;

    const boton = contenedor.querySelector("[data-mostrar-busqueda-historial-ciclos]");
    const panel = contenedor.querySelector("[data-contenedor-busqueda-historial-ciclos]");
    const entrada = document.getElementById("buscarHistorialCicloMovil");
    const tarjetas = Array.from(contenedor.querySelectorAll(".historial-ciclo-movil-item"));
    const anterior = contenedor.querySelector("[data-anterior-historial-ciclos-movil]");
    const siguiente = contenedor.querySelector("[data-siguiente-historial-ciclos-movil]");
    const contador = contenedor.querySelector("[data-contador-historial-ciclos-movil]");
    const vacio = contenedor.querySelector("[data-sin-historial-ciclos-movil]");
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
        contador.textContent = filtradas.length ?
            `Página ${pagina} de ${total} · ${filtradas.length} registros` : "0 registros";
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
});
