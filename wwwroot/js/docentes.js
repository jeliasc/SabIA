document.addEventListener("DOMContentLoaded", function () {
    const botonBusqueda = document.querySelector("[data-mostrar-busqueda-docentes]");
    const contenedorBusqueda = document.querySelector("[data-contenedor-busqueda-docentes]");
    const entradaBusqueda = document.querySelector("[data-busqueda-docentes]");
    const tarjetas = Array.from(document.querySelectorAll("[data-docente]"));
    const sinResultados = document.querySelector("[data-docentes-sin-resultados]");

    botonBusqueda?.addEventListener("click", function () {
        contenedorBusqueda?.classList.toggle("d-none");
        const busquedaVisible = !contenedorBusqueda?.classList.contains("d-none");

        botonBusqueda.setAttribute("aria-expanded", String(busquedaVisible));

        if (busquedaVisible) {
            entradaBusqueda?.focus();
        }
    });

    entradaBusqueda?.addEventListener("input", function () {
        const normalizarTexto = function (valor) {
            return valor
                .normalize("NFD")
                .replace(/[\u0300-\u036f]/g, "")
                .toLowerCase();
        };

        const texto = normalizarTexto(entradaBusqueda.value.trim());
        let visibles = 0;

        tarjetas.forEach(function (tarjeta) {
            const coincide = normalizarTexto(
                tarjeta.dataset.textoBusqueda ?? ""
            )
                .includes(texto);

            tarjeta.classList.toggle("d-none", !coincide);
            if (coincide) visibles++;
        });

        sinResultados?.classList.toggle("d-none", visibles !== 0);
    });

    document.querySelectorAll("[data-cambiar-estado-docente]").forEach(function (formulario) {
        formulario.addEventListener("submit", async function (evento) {
            evento.preventDefault();

            const accion = formulario.dataset.accion ?? "actualizar";
            const confirmado = await confirmarAccion(
                `${accion === "desactivar" ? "Desactivar" : "Activar"} docente`,
                `¿Confirma que desea ${accion} este docente?`,
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

            if (!$(formulario).valid()) {
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
        "[data-crear-docente]",
        "Crear docente",
        "Se creará la cuenta de acceso del docente y se generará una contraseña temporal.",
        "Crear"
    );

    configurarConfirmacionFormulario(
        "[data-editar-docente]",
        "Guardar cambios",
        "¿Desea guardar los cambios realizados al docente?",
        "Guardar"
    );
});
