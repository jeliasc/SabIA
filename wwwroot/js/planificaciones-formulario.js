
document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    const formulario = document.querySelector(
        "form[data-formulario-planificacion]"
    );

    if (!formulario) return;

    const tipo = formulario.querySelector("[data-tipo-planificacion]");
    const periodo = formulario.querySelector("#PeriodoId");
    const unidad = formulario.querySelector("#UnidadAsignacionId");

    const contenedorPeriodo = formulario.querySelector(
        "[data-contenedor-periodo]"
    );
    const contenedorUnidad = formulario.querySelector(
        "[data-contenedor-unidad]"
    );

    const modalidad = formulario.querySelector(
        "[data-modalidad-contenido]"
    );
    const archivo = formulario.querySelector(
        "[data-archivo-planificacion]"
    );

    const camposManuales = Array.from(
        formulario.querySelectorAll(
            "[data-contenido-manual] textarea"
        )
    );

    if (!tipo || !periodo || !unidad ||
        !contenedorPeriodo || !contenedorUnidad ||
        !modalidad || !archivo) {
        return;
    }

    // LIMPIAR SELECTOR NORMAL O TOM SELECT
    const limpiarSelector = selector => {
        if (!selector.value) return;

        if (selector.tomselect) {
            selector.tomselect.clear(true);
        } else {
            selector.value = "";
        }
    };

    // IDENTIFICAR TIPO DE PLANIFICACIÓN
    const obtenerTipo = () => {
        const opcion = tipo.options[tipo.selectedIndex];

        return (opcion?.textContent ?? "")
            .trim()
            .toLocaleLowerCase("es");
    };

    // ACTUALIZAR PERÍODO Y UNIDAD
    const actualizarTipo = () => {
        const seleccionado = obtenerTipo();

        const esAnual = seleccionado === "anual";
        const esBimestral = seleccionado === "bimestral";
        const esPorUnidad = seleccionado === "por unidad";

        const mostrarPeriodo = !esAnual && !esPorUnidad;
        const mostrarUnidad = !esAnual && !esBimestral;

        contenedorPeriodo.classList.toggle(
            "d-none",
            !mostrarPeriodo
        );

        contenedorUnidad.classList.toggle(
            "d-none",
            !mostrarUnidad
        );

        if (!mostrarPeriodo) {
            limpiarSelector(periodo);
        }

        if (!mostrarUnidad) {
            limpiarSelector(unidad);
        }
    };

    // CONSERVAR CONTENIDO MIENTRAS SE CAMBIA DE MODALIDAD
    const contenidoOriginal = new Map();

    camposManuales.forEach(campo => {
        contenidoOriginal.set(campo, campo.value);
    });

    const tienePdfActual =
        modalidad.dataset.pdfActual === "true";

    const esEdicion =
        modalidad.dataset.esEdicion === "true";

    // RECONOCER PLANIFICACIÓN EXISTENTE EN FORMATO PDF
    if (esEdicion &&
        tienePdfActual &&
        camposManuales.length > 0 &&
        camposManuales.every(campo => campo.value.trim() === "-")) {
        modalidad.value = "Pdf";
    }

    // ACTUALIZAR MODALIDAD DE CONTENIDO
    const actualizarModalidad = () => {
        const esPdf = modalidad.value === "Pdf";

        formulario.querySelectorAll(
            "[data-contenido-manual]"
        ).forEach(contenedor => {
            contenedor.classList.toggle("d-none", esPdf);
        });

        camposManuales.forEach(campo => {
            if (esPdf) {
                campo.value = "-";
                campo.required = false;
            } else {
                const anterior = contenidoOriginal.get(campo);

                if (campo.value === "-" && anterior !== "-") {
                    campo.value = anterior ?? "";
                }
            }
        });

        // EXIGIR PDF SOLO SI NO EXISTE UNO GUARDADO
        archivo.required = esPdf && !tienePdfActual;

        const ayuda = formulario.querySelector(
            "[data-ayuda-archivo]"
        );

        if (ayuda) {
            ayuda.textContent = esPdf
                ? tienePdfActual
                    ? "Puede conservar el PDF actual o seleccionar uno nuevo."
                    : "Seleccione un documento PDF para guardar la planificación."
                : "Opcional: puede adjuntar un PDF de apoyo para generación con IA.";
        }
    };

    // CAMBIOS DE TIPO
    tipo.addEventListener("change", actualizarTipo);

    // CAMBIOS DE MODALIDAD
    modalidad.addEventListener("change", actualizarModalidad);

    // VALIDAR EL ARCHIVO SELECCIONADO
    archivo.addEventListener("change", () => {
        const documento = archivo.files?.[0];

        if (!documento) {
            archivo.setCustomValidity("");
            return;
        }

        const extensionValida = documento.name
            .toLowerCase()
            .endsWith(".pdf");

        archivo.setCustomValidity(
            extensionValida
                ? ""
                : "Seleccione un archivo con extensión PDF."
        );
    });

    // INICIALIZACIÓN
    actualizarTipo();
    actualizarModalidad();
});
