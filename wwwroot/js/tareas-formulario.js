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
        const contenedorCriterio = formulario.querySelector("[data-criterio-calificacion-entrega]");
        const criterio = formulario.querySelector("select[data-criterio-calificacion-entrega]");
        const boton = formulario.querySelector("[data-accion-tarea]");
        const textoBoton = formulario.querySelector("[data-texto-accion-tarea]");
        const esEdicion = Boolean(formulario.querySelector('input[name="Id"]'));

        if (!tipo || !contenedorAdjunto || !archivo || !contenedorIntentos ||
            !intentos || !contenedorCriterio || !criterio || !boton || !textoBoton) {
            return;
        }

        const actualizar = () => {
            const estado = obtenerEstadoFormulario(
                tipo.value,
                tipo.dataset.tipoCuestionario);

            // VISIBILIDAD SEGÚN TIPO DE TAREA
            contenedorAdjunto.classList.toggle("d-none", estado.esCuestionario);
            archivo.disabled = estado.esCuestionario;

            contenedorIntentos.classList.toggle("d-none", estado.esCuestionario);
            intentos.disabled = estado.esCuestionario;

            const usaVariosIntentos = !estado.esCuestionario && intentos.value !== "1";

            contenedorCriterio.classList.toggle("d-none", !usaVariosIntentos);
            criterio.disabled = !usaVariosIntentos;

            if (!esEdicion) {
                textoBoton.textContent = estado.textoAccion;
            }

            if (estado.esCuestionario) {
                archivo.value = "";
            }
        };

        tipo.addEventListener("change", actualizar);
        intentos.addEventListener("change", actualizar);

        const restaurarBoton = () => {
            formulario.dataset.enviando = "false";
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
        };

        formulario.addEventListener("invalid", restaurarBoton, true);
        formulario.addEventListener("submit", (evento) => {
            actualizar();

            // jQuery Validate es quien pinta los mensajes unobtrusive. La
            // comprobación nativa conserva compatibilidad de teclado y campos
            // HTML5. Sólo se bloquea después de que ambas validaciones pasen.
            const jquery = window.jQuery;
            const validoUnobtrusive = !jquery || jquery(formulario).valid();
            const validoNativo = formulario.checkValidity();
            if (evento.defaultPrevented || !validoUnobtrusive || !validoNativo) {
                evento.preventDefault();
                restaurarBoton();
                formulario.querySelector(":invalid")?.focus();
                return;
            }

            if (formulario.dataset.enviando === "true") {
                evento.preventDefault();
                return;
            }

            formulario.dataset.enviando = "true";
            boton.disabled = true;
            boton.setAttribute("aria-busy", "true");
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
