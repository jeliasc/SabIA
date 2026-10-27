document.addEventListener("DOMContentLoaded", function () {
    const selectorIntento = document.querySelector("[data-selector-intento]");

    if (!selectorIntento) {
        return;
    }

    const seccionesIntentos = document.querySelectorAll("[data-intento-id]");

    function mostrarIntentoSeleccionado() {
        const intentoSeleccionado = selectorIntento.value;

        seccionesIntentos.forEach(function (seccion) {
            seccion.hidden = seccion.dataset.intentoId !== intentoSeleccionado;
        });
    }

    selectorIntento.addEventListener("change", mostrarIntentoSeleccionado);

    // Sincronizar el contenido visible con el intento seleccionado.
    mostrarIntentoSeleccionado();
});