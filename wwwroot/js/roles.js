document.addEventListener("DOMContentLoaded", () => {

    // FORMULARIO
    const formulario =
        document.querySelector("[data-modo]");

    if (formulario) {

        // CAMPOS
        const campos = {
            nombre:
                document.getElementById("Nombre")
        };

        // MOSTRAR ERROR DE CAMPO
        function mostrarErrorCampo(
            campo,
            mensaje
        ) {
            const elementoError =
                document.getElementById(
                    `${campo.id}Error`
                );

            campo.classList.add("is-invalid");

            if (elementoError) {
                elementoError.textContent =
                    mensaje;
            }
        }

        // LIMPIAR ERROR DE CAMPO
        function limpiarErrorCampo(campo) {
            const elementoError =
                document.getElementById(
                    `${campo.id}Error`
                );

            campo.classList.remove(
                "is-invalid"
            );

            if (elementoError) {
                elementoError.textContent = "";
            }
        }

        // VALIDAR NOMBRE
        function validarNombre() {
            const campo =
                campos.nombre;

            if (!campo) {
                return true;
            }

            const valor =
                campo.value.trim();

            limpiarErrorCampo(campo);

            if (valor === "") {
                mostrarErrorCampo(
                    campo,
                    "El nombre del rol es obligatorio."
                );

                return false;
            }

            if (valor.length > 100) {
                mostrarErrorCampo(
                    campo,
                    "El nombre del rol no puede superar los 100 caracteres."
                );

                return false;
            }

            return true;
        }

        // VALIDACIÓN Y CONFIRMACIÓN DEL FORMULARIO
        formulario.addEventListener(
            "submit",
            async (evento) => {

                evento.preventDefault();

                if (!validarNombre()) {
                    return;
                }

                const modo =
                    formulario.dataset.modo;

                let confirmado;

                if (modo === "crear") {

                    confirmado =
                        await confirmarAccion(
                            "Crear rol",
                            "¿Desea registrar este rol en el sistema?",
                            "Crear",
                            "Cancelar"
                        );

                } else {

                    confirmado =
                        await confirmarAccion(
                            "Guardar cambios",
                            "¿Desea guardar los cambios realizados al rol?",
                            "Guardar",
                            "Cancelar"
                        );
                }

                if (confirmado) {
                    formulario.submit();
                }
            }
        );

        // VALIDACIÓN EN TIEMPO REAL
        if (campos.nombre) {

            campos.nombre.addEventListener(
                "input",
                validarNombre
            );

            campos.nombre.addEventListener(
                "blur",
                validarNombre
            );
        }
    }

    // CONFIRMAR CAMBIO DE ESTADO
    const formulariosEstado =
        document.querySelectorAll(
            ".formulario-cambiar-estado"
        );

    formulariosEstado.forEach(
        (formularioEstado) => {

            formularioEstado.addEventListener(
                "submit",
                async (evento) => {

                    evento.preventDefault();

                    const accion =
                        formularioEstado.dataset.accion;

                    const nombre =
                        formularioEstado.dataset.nombre;

                    let confirmado;

                    if (accion === "desactivar") {

                        confirmado =
                            await confirmarAccion(
                                "Desactivar rol",
                                `¿Está seguro de que desea desactivar el rol "${nombre}"?`,
                                "Desactivar",
                                "Cancelar",
                                "#dc3545"
                            );

                    } else {

                        confirmado =
                            await confirmarAccion(
                                "Activar rol",
                                `¿Está seguro de que desea activar el rol "${nombre}"?`,
                                "Activar",
                                "Cancelar"
                            );
                    }

                    if (confirmado) {
                        formularioEstado.submit();
                    }
                }
            );
        }
    );

    // SELECCIONAR TODOS LOS PERMISOS
    const botonSeleccionarTodos =
        document.getElementById("seleccionarTodosPermisos");

    if (botonSeleccionarTodos) {
        botonSeleccionarTodos.addEventListener(
            "click",
            function () {
                const permisos =
                    document.querySelectorAll(
                        ".permiso-rol"
                    );

                permisos.forEach(function (permiso) {
                    permiso.checked = true;
                });
            }
        );
    }

    // QUITAR TODOS LOS PERMISOS
    const botonQuitarTodos =
        document.getElementById("quitarTodosPermisos");

    if (botonQuitarTodos) {
        botonQuitarTodos.addEventListener(
            "click",
            function () {
                const permisos =
                    document.querySelectorAll(
                        ".permiso-rol"
                    );

                permisos.forEach(function (permiso) {
                    permiso.checked = false;
                });
            }
        );
    }
});