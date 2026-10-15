(() => {
    "use strict";

    const obtenerEstadoFormulario = (tipoSeleccionado, tipoCuestionario) => {
        const esCuestionario = String(tipoSeleccionado) === String(tipoCuestionario);

        return {
            esCuestionario,
            mostrarAdjunto: !esCuestionario,
            textoAccion: esCuestionario
                ? "Configurar cuestionario"
                : "Guardar borrador"
        };
    };

    const inicializar = () => {
        const formulario = document.querySelector("[data-formulario-tarea]");
        if (!formulario) {
            return;
        }

        const tipo = formulario.querySelector("[data-tipo-tarea]");
        const contenedorAdjunto = formulario.querySelector("[data-adjunto-tarea]");
        const archivo = formulario.querySelector("[data-archivo-tarea]");
        const contenedorIntentos = formulario.querySelector("[data-intentos-entrega]");
        const intentos = formulario.querySelector("[data-maximo-intentos-entrega]");
        const boton = formulario.querySelector("[data-accion-tarea]");
        const textoBoton = formulario.querySelector("[data-texto-accion-tarea]");

        if (!tipo || !contenedorAdjunto || !archivo || !contenedorIntentos ||
            !intentos || !boton || !textoBoton) {
            return;
        }

        const actualizar = () => {
            const estado = obtenerEstadoFormulario(
                tipo.value,
                tipo.dataset.tipoCuestionario);

            contenedorAdjunto.hidden = !estado.mostrarAdjunto;
            archivo.disabled = estado.esCuestionario;
            contenedorIntentos.hidden = estado.esCuestionario;
            intentos.disabled = estado.esCuestionario;
            textoBoton.textContent = estado.textoAccion;

            if (estado.esCuestionario) {
                archivo.value = "";
            }
        };

        tipo.addEventListener("change", actualizar);
        formulario.addEventListener("submit", (evento) => {
            actualizar();

            if (!evento.defaultPrevented && formulario.checkValidity()) {
                boton.disabled = true;
                boton.setAttribute("aria-busy", "true");
            }
        });

        actualizar();
    };

    if (typeof module !== "undefined" && module.exports) {
        module.exports = { obtenerEstadoFormulario };
    }

    if (typeof document !== "undefined") {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", inicializar);
        } else {
            inicializar();
        }
    }
})();
