document.addEventListener("DOMContentLoaded", () => {

    // FORMULARIO
    const formulario =
        document.querySelector("[data-modo]");

    if (formulario) {

        // CAMPOS
        const campos = {
            codigo:
                document.getElementById("Codigo"),

            modulo:
                document.getElementById("Modulo"),

            descripcion:
                document.getElementById("Descripcion")
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

        // VALIDAR CÓDIGO
        function validarCodigo() {
            const campo =
                campos.codigo;

            if (!campo) {
                return true;
            }

            const valor =
                campo.value.trim();

            limpiarErrorCampo(campo);

            if (valor === "") {
                mostrarErrorCampo(
                    campo,
                    "El código del permiso es obligatorio."
                );

                return false;
            }

            if (valor.length > 150) {
                mostrarErrorCampo(
                    campo,
                    "El código del permiso no puede superar los 150 caracteres."
                );

                return false;
            }

            // Formato esperado: Modulo.Accion
            const expresionCodigo =
                /^[A-Za-zÁÉÍÓÚáéíóúÑñ0-9]+(?:[A-Za-zÁÉÍÓÚáéíóúÑñ0-9_-]*[A-Za-zÁÉÍÓÚáéíóúÑñ0-9])?\.[A-Za-zÁÉÍÓÚáéíóúÑñ0-9]+(?:[A-Za-zÁÉÍÓÚáéíóúÑñ0-9_-]*[A-Za-zÁÉÍÓÚáéíóúÑñ0-9])?$/;

            if (!expresionCodigo.test(valor)) {
                mostrarErrorCampo(
                    campo,
                    "Utilice el formato Modulo.Accion. Ejemplo: Docentes.Crear."
                );

                return false;
            }

            return true;
        }

        // VALIDAR MÓDULO
        function validarModulo() {
            const campo =
                campos.modulo;

            if (!campo) {
                return true;
            }

            const valor =
                campo.value.trim();

            limpiarErrorCampo(campo);

            if (valor === "") {
                mostrarErrorCampo(
                    campo,
                    "El módulo es obligatorio."
                );

                return false;
            }

            if (valor.length > 100) {
                mostrarErrorCampo(
                    campo,
                    "El módulo no puede superar los 100 caracteres."
                );

                return false;
            }

            return true;
        }

        // VALIDAR DESCRIPCIÓN
        function validarDescripcion() {
            const campo =
                campos.descripcion;

            if (!campo) {
                return true;
            }

            const valor =
                campo.value.trim();

            limpiarErrorCampo(campo);

            if (valor === "") {
                mostrarErrorCampo(
                    campo,
                    "La descripción es obligatoria."
                );

                return false;
            }

            if (valor.length > 250) {
                mostrarErrorCampo(
                    campo,
                    "La descripción no puede superar los 250 caracteres."
                );

                return false;
            }

            return true;
        }

        // VALIDAR FORMULARIO
        function validarFormulario() {

            const codigoValido =
                validarCodigo();

            const moduloValido =
                validarModulo();

            const descripcionValida =
                validarDescripcion();

            return codigoValido &&
                moduloValido &&
                descripcionValida;
        }

        // VALIDACIÓN Y CONFIRMACIÓN DEL FORMULARIO
        formulario.addEventListener(
            "submit",
            async (evento) => {

                evento.preventDefault();

                if (!validarFormulario()) {
                    return;
                }

                const modo =
                    formulario.dataset.modo;

                let confirmado;

                if (modo === "crear") {

                    confirmado =
                        await confirmarAccion(
                            "Crear permiso",
                            "¿Desea registrar este permiso en el sistema?",
                            "Crear",
                            "Cancelar"
                        );

                } else {

                    confirmado =
                        await confirmarAccion(
                            "Guardar cambios",
                            "¿Desea guardar los cambios realizados al permiso?",
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
        if (campos.codigo) {

            campos.codigo.addEventListener(
                "input",
                validarCodigo
            );

            campos.codigo.addEventListener(
                "blur",
                validarCodigo
            );
        }

        if (campos.modulo) {

            campos.modulo.addEventListener(
                "input",
                validarModulo
            );

            campos.modulo.addEventListener(
                "blur",
                validarModulo
            );
        }

        if (campos.descripcion) {

            campos.descripcion.addEventListener(
                "input",
                validarDescripcion
            );

            campos.descripcion.addEventListener(
                "blur",
                validarDescripcion
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
                                "Desactivar permiso",
                                `¿Está seguro de que desea desactivar el permiso "${nombre}"?`,
                                "Desactivar",
                                "Cancelar",
                                "#dc3545"
                            );

                    } else {

                        confirmado =
                            await confirmarAccion(
                                "Activar permiso",
                                `¿Está seguro de que desea activar el permiso "${nombre}"?`,
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
});