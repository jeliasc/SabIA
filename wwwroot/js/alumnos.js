document.addEventListener("DOMContentLoaded", function () {
    const botonBusqueda = document.querySelector("[data-mostrar-busqueda-alumnos]");
    const contenedorBusqueda = document.querySelector("[data-contenedor-busqueda-alumnos]");
    const entradaBusqueda = document.querySelector("[data-busqueda-alumnos]");
    const tarjetas = Array.from(document.querySelectorAll("[data-alumno]"));
    const sinResultados = document.querySelector("[data-alumnos-sin-resultados]");

    const normalizarTexto = function (valor) {
        return valor
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLowerCase();
    };

    botonBusqueda?.addEventListener("click", function () {
        contenedorBusqueda?.classList.toggle("d-none");
        const busquedaVisible = !contenedorBusqueda?.classList.contains("d-none");

        botonBusqueda.setAttribute("aria-expanded", String(busquedaVisible));

        if (busquedaVisible) {
            entradaBusqueda?.focus();
        }
    });

    entradaBusqueda?.addEventListener("input", function () {
        const texto = normalizarTexto(entradaBusqueda.value.trim());
        let visibles = 0;

        tarjetas.forEach(function (tarjeta) {
            const coincide = normalizarTexto(
                tarjeta.dataset.textoBusqueda ?? ""
            ).includes(texto);

            tarjeta.classList.toggle("d-none", !coincide);
            if (coincide) visibles++;
        });

        sinResultados?.classList.toggle("d-none", visibles !== 0);
    });

    const registrarEncargado = document.querySelector("[data-registrar-encargado]");
    const datosEncargado = document.querySelector("[data-datos-encargado]");
    const actualizarDatosEncargado = function () {
        if (!registrarEncargado || !datosEncargado) return;
        datosEncargado.classList.toggle("d-none", !registrarEncargado.checked);
        datosEncargado.querySelectorAll("input, select, textarea").forEach(function (campo) {
            campo.disabled = !registrarEncargado.checked;
        });
    };
    actualizarDatosEncargado();
    registrarEncargado?.addEventListener("change", actualizarDatosEncargado);

    document.querySelectorAll("[data-quitar-encargado]").forEach(function (boton) {
        boton.addEventListener("click", async function () {
            const aceptar = await confirmarAccion("Desvincular encargado", "El encargado se desvinculará del alumno, pero permanecerá en el catálogo general.", "Desvincular", "Cancelar", "#dc3545");
            if (aceptar) boton.closest("[data-relacion-encargado]")?.remove();
        });
    });

    document.querySelectorAll("[data-cambiar-estado-alumno]").forEach(function (formulario) {
        formulario.addEventListener("submit", async function (evento) {
            evento.preventDefault();

            const accion = formulario.dataset.accion ?? "actualizar";
            const confirmado = await confirmarAccion(
                `${accion === "desactivar" ? "Desactivar" : "Activar"} alumno`,
                `¿Confirma que desea ${accion} este alumno?`,
                accion === "desactivar" ? "Desactivar" : "Activar"
            );

            if (confirmado) formulario.submit();
        });
    });

    const configurarConfirmacionFormulario = function (
        selector,
        titulo,
        mensaje,
        textoConfirmar
    ) {
        const formulario = document.querySelector(selector);

        if (!formulario) {
            return;
        }

        let confirmado = false;

        formulario.addEventListener("submit", async function (evento) {
            if (confirmado) {
                return;
            }

            evento.preventDefault();

            const esValido = typeof window.jQuery === "function"
                ? window.jQuery(formulario).valid()
                : formulario.checkValidity();

            if (!esValido) {
                formulario.reportValidity();
                return;
            }

            const aceptar = await confirmarAccion(
                titulo,
                typeof mensaje === "function" ? mensaje() : mensaje,
                textoConfirmar,
                "Cancelar",
                "#198754"
            );

            if (!aceptar) {
                return;
            }

            confirmado = true;
            formulario.requestSubmit();
        });
    };

    configurarConfirmacionFormulario(
        "[data-crear-alumno]",
        "Crear alumno",
        () => document.querySelector("[data-inscribir-ahora]")?.checked
            ? "Se crearán la cuenta de acceso y la inscripción inicial del alumno. Se generará una contraseña temporal."
            : "Se crearán la cuenta de acceso y el perfil del alumno sin inscripción académica. Se generará una contraseña temporal.",
        "Crear"
    );

    configurarConfirmacionFormulario(
        "[data-editar-alumno]",
        "Guardar cambios",
        "¿Desea guardar los cambios realizados al alumno?",
        "Guardar"
    );
});

// INSCRIPCIÓN ACADÉMICA OPCIONAL
(() => {
    const iniciar = () => {
        const formulario = document.querySelector("[data-crear-alumno]");
        if (!formulario) return;

        const casilla = formulario.querySelector("[data-inscribir-ahora]");
        const campos = formulario.querySelector("[data-campos-inscripcion]");
        if (!casilla || !campos) return;

        const mensajes = {
            CicloEscolarId: "Seleccione un ciclo escolar.",
            Carrera: "Seleccione una carrera.",
            GradoId: "Seleccione un grado.",
            SeccionId: "Seleccione una sección."
        };

        const controles = Object.entries(mensajes).map(([nombre, mensaje]) => ({
            campo: formulario.elements.namedItem(nombre),
            nombre,
            mensaje
        })).filter(item => item.campo);

        // EVITAR VALIDACIONES AUTOMÁTICAS INCONDICIONALES
        controles.forEach(({ campo }) => {
            campo.removeAttribute("required");
            campo.removeAttribute("data-val-required");
        });

        const $ = window.jQuery;

        if ($?.validator?.unobtrusive) {
            $(formulario).removeData("validator");
            $(formulario).removeData("unobtrusiveValidation");
            $.validator.unobtrusive.parse(formulario);
        }

        const mostrarError = ({ campo, nombre, mensaje }, error) => {
            const elemento = formulario.querySelector(
                `[data-valmsg-for="${nombre}"]`
            );

            campo.classList.toggle("input-validation-error", error);

            if (elemento) {
                elemento.textContent = error ? mensaje : "";
                elemento.classList.toggle("field-validation-error", error);
                elemento.classList.toggle("field-validation-valid", !error);
            }
        };

        const limpiarErrores = () => {
            controles.forEach(item => mostrarError(item, false));
        };

        const validarInscripcion = () => {
            if (!casilla.checked) return true;

            let valido = true;

            controles.forEach(item => {
                const vacio = !item.campo.value ||
                    item.campo.value === "0";

                mostrarError(item, vacio);

                if (vacio) valido = false;
            });

            return valido;
        };

        const actualizar = () => {
            campos.classList.toggle("d-none", !casilla.checked);
            limpiarErrores();
        };

        // VALIDACIÓN ANTES DE LA CONFIRMACIÓN
        formulario.addEventListener("submit", evento => {
            if (!validarInscripcion()) {
                evento.preventDefault();
                evento.stopImmediatePropagation();
            }
        }, true);

        controles.forEach(({ campo }) => {
            campo.addEventListener("change", () => {
                if (casilla.checked) {
                    const item = controles.find(x => x.campo === campo);
                    mostrarError(item, false);
                }
            });
        });

        casilla.addEventListener("change", actualizar);
        actualizar();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciar, { once: true });
    } else {
        iniciar();
    }
})();