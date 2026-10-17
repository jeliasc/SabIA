// CONTENIDO CON IA
// Muestra el selector de planificación o el campo para subir un PDF,
// según el origen del documento elegido. Se carga desde la página
// completa (Index o Generar), porque los scripts definidos dentro de
// una vista parcial no se ejecutan.

(function () {
    "use strict";

    const iniciar = () => {
        document.querySelectorAll("[data-origen-ia-formulario]").forEach(formulario => {
            if (formulario.dataset.origenIaListo === "true") return;
            formulario.dataset.origenIaListo = "true";

            const plan = formulario.querySelector("[data-origen-planificacion]");
            const archivo = formulario.querySelector("[data-origen-archivo]");
            const selectorPlan = formulario.querySelector("[name='PlanificacionId']");
            const selectorArchivo = formulario.querySelector("[name='Documento']");
            const opcionArchivo = formulario.querySelector("[data-origen-ia='archivo']");

            if (!plan || !archivo || !selectorPlan || !selectorArchivo || !opcionArchivo) return;

            const actualizar = () => {
                const local = opcionArchivo.checked;

                plan.classList.toggle("d-none", local);
                archivo.classList.toggle("d-none", !local);

                // Los campos deshabilitados no se envían: así el servidor
                // recibe solo el origen elegido.
                selectorPlan.disabled = local;
                selectorArchivo.disabled = !local;

                if (selectorPlan.tomselect) {
                    if (local) {
                        selectorPlan.tomselect.clear(true);
                        selectorPlan.tomselect.disable();
                    } else {
                        selectorPlan.tomselect.enable();
                    }
                } else if (local) {
                    selectorPlan.value = "";
                }

                if (!local) selectorArchivo.value = "";
            };

            formulario.querySelectorAll("[data-origen-ia]").forEach(opcion =>
                opcion.addEventListener("change", actualizar));

            actualizar();
        });
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciar, { once: true });
    } else {
        iniciar();
    }
})();
