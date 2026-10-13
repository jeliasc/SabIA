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
                mensaje,
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
        "Se crearán la cuenta de acceso y la inscripción inicial del alumno, y se generará una contraseña temporal.",
        "Crear"
    );

    configurarConfirmacionFormulario(
        "[data-editar-alumno]",
        "Guardar cambios",
        "¿Desea guardar los cambios realizados al alumno?",
        "Guardar"
    );
});
