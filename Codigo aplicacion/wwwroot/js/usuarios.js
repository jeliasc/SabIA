document.addEventListener("DOMContentLoaded", function () {

    // CONFIGURAR TABLA DE USUARIOS
    const tablaUsuarios =
        document.getElementById("tablaUsuarios");

    if (tablaUsuarios &&
        typeof DataTable !== "undefined") {

        new DataTable(
            tablaUsuarios,
            {
                pageLength: 10,

                lengthMenu: [
                    5,
                    10,
                    25,
                    50
                ],

                order: [
                    [0, "asc"]
                ],

                columnDefs: [
                    {
                        targets: -1,
                        orderable: false,
                        searchable: false
                    }
                ],

                language: {
                    search: "Buscar:",
                    searchPlaceholder:
                        "Usuario, nombre o correo...",

                    lengthMenu:
                        "Mostrar _MENU_ registros",

                    info:
                        "Mostrando _START_ a _END_ de _TOTAL_ registros",

                    infoEmpty:
                        "No existen registros",

                    infoFiltered:
                        "(filtrado de _MAX_ registros)",

                    zeroRecords:
                        "No se encontraron usuarios",

                    emptyTable:
                        "No existen usuarios registrados",

                    paginate: {
                        first: "<<",
                        previous: "<",
                        next: ">",
                        last: ">>"
                    }
                }
            }
        );
    }


    // COPIAR CONTRASEÑA TEMPORAL
    const botonCopiar =
        document.getElementById(
            "botonCopiarContrasena"
        );

    const campoContrasena =
        document.getElementById(
            "contrasenaTemporal"
        );

    if (botonCopiar && campoContrasena) {
        botonCopiar.addEventListener(
            "click",
            async function () {
                try {
                    await navigator.clipboard.writeText(
                        campoContrasena.value
                    );

                    botonCopiar.textContent =
                        "Copiada";

                    setTimeout(
                        function () {
                            botonCopiar.textContent =
                                "Copiar";
                        },
                        1500
                    );
                }
                catch {
                    mostrarError(
                        "No fue posible copiar",
                        "Copie la contraseña temporal manualmente."
                    );
                }
            }
        );
    }


    // MOSTRAR U OCULTAR BÚSQUEDA EN MÓVIL
    const botonMostrarBusqueda =
        document.querySelector(
            "[data-mostrar-busqueda-usuarios]"
        );

    const contenedorBusqueda =
        document.querySelector(
            "[data-contenedor-busqueda-usuarios]"
        );

    const campoBusqueda =
        document.querySelector(
            "[data-busqueda-usuarios]"
        );

    if (botonMostrarBusqueda &&
        contenedorBusqueda &&
        campoBusqueda) {

        botonMostrarBusqueda.addEventListener(
            "click",
            function () {
                const estaOculta =
                    contenedorBusqueda.classList.contains(
                        "d-none"
                    );

                contenedorBusqueda.classList.toggle(
                    "d-none"
                );

                botonMostrarBusqueda.textContent =
                    estaOculta
                        ? "Ocultar búsqueda"
                        : "Buscar usuario";

                if (estaOculta) {
                    campoBusqueda.focus();
                }
            }
        );
    }


    // FILTRAR USUARIOS EN VISTA MÓVIL
    const tarjetasUsuarios =
        document.querySelectorAll(
            "[data-usuario]"
        );

    const mensajeSinResultados =
        document.querySelector(
            "[data-sin-resultados-usuarios]"
        );

    if (campoBusqueda) {
        campoBusqueda.addEventListener(
            "input",
            function () {
                const textoBuscado =
                    normalizarTexto(
                        campoBusqueda.value
                    );

                let encontrados = 0;

                tarjetasUsuarios.forEach(
                    function (tarjeta) {
                        const textoUsuario =
                            normalizarTexto(
                                tarjeta.dataset
                                    .textoBusqueda || ""
                            );

                        const coincide =
                            textoUsuario.includes(
                                textoBuscado
                            );

                        tarjeta.classList.toggle(
                            "d-none",
                            !coincide
                        );

                        if (coincide) {
                            encontrados++;
                        }
                    }
                );

                if (mensajeSinResultados) {
                    mensajeSinResultados.classList.toggle(
                        "d-none",
                        encontrados > 0
                    );
                }
            }
        );
    }


    // CONFIRMAR RESTABLECIMIENTO DE CONTRASEÑA
    const formulariosRestablecer =
        document.querySelectorAll(
            "[data-restablecer-contrasena]"
        );

    formulariosRestablecer.forEach(
        function (formulario) {
            formulario.addEventListener(
                "submit",
                async function (evento) {
                    evento.preventDefault();

                    const confirmado =
                        await confirmarAccion(
                            "Restablecer contraseña",
                            "La contraseña actual dejará de funcionar y se generará una nueva contraseña temporal.",
                            "Restablecer",
                            "Cancelar",
                            "#dc3545"
                        );

                    if (confirmado) {
                        formulario.submit();
                    }
                }
            );
        }
    );

    // CONFIRMAR CREACIÓN DE USUARIO
    const formularioCrear =
        document.querySelector(
            "[data-crear-usuario]"
        );

    if (formularioCrear) {
        let confirmado = false;

        formularioCrear.addEventListener(
            "submit",
            async function (evento) {
                if (confirmado) {
                    return;
                }

                evento.preventDefault();

                // VALIDAR CAMPOS DEL FORMULARIO
                if (!$(formularioCrear).valid()) {
                    return;
                }

                // VALIDAR DATOS EN EL SERVIDOR
                const datosValidos =
                    await validarDatosUsuario(
                        formularioCrear
                    );

                if (!datosValidos) {
                    return;
                }

                // CONFIRMAR CREACIÓN
                const aceptar =
                    await confirmarAccion(
                        "Crear usuario",
                        "¿Desea crear el nuevo usuario?",
                        "Crear",
                        "Cancelar",
                        "#198754"
                    );

                if (!aceptar) {
                    return;
                }

                confirmado = true;

                formularioCrear.requestSubmit();
            }
        );
    }


    // CONFIRMAR EDICIÓN DE USUARIO
    const formularioEditar =
        document.querySelector(
            "[data-editar-usuario]"
        );

    if (formularioEditar) {
        let confirmado = false;

        formularioEditar.addEventListener(
            "submit",
            async function (evento) {
                if (confirmado) {
                    return;
                }

                evento.preventDefault();

                // VALIDAR CAMPOS DEL FORMULARIO
                if (!$(formularioEditar).valid()) {
                    return;
                }

                // VALIDAR DATOS EN EL SERVIDOR
                const datosValidos =
                    await validarDatosUsuario(
                        formularioEditar
                    );

                if (!datosValidos) {
                    return;
                }

                // CONFIRMAR SOLAMENTE SI TODO ES VÁLIDO
                const aceptar =
                    await confirmarAccion(
                        "Guardar cambios",
                        "¿Desea guardar los cambios realizados al usuario?",
                        "Aceptar",
                        "Cancelar",
                        "#198754"
                    );

                if (!aceptar) {
                    return;
                }

                confirmado = true;

                formularioEditar.requestSubmit();
            }
        );
    }


    // CONFIRMAR CAMBIO DE ESTADO
    const formulariosCambiarEstado =
        document.querySelectorAll(
            "[data-cambiar-estado-usuario]"
        );

    formulariosCambiarEstado.forEach(
        function (formulario) {
            let confirmado = false;

            formulario.addEventListener(
                "submit",
                async function (evento) {
                    if (confirmado) {
                        return;
                    }

                    evento.preventDefault();

                    const accion =
                        formulario.dataset.accion;

                    const esActivar =
                        accion === "activar";

                    const aceptar =
                        await confirmarAccion(
                            esActivar
                                ? "Activar usuario"
                                : "Desactivar usuario",

                            esActivar
                                ? "¿Desea permitir nuevamente el acceso de este usuario al sistema?"
                                : "El usuario ya no podrá iniciar sesión en el sistema.",

                            esActivar
                                ? "Activar"
                                : "Desactivar",

                            "Cancelar",

                            esActivar
                                ? "#198754"
                                : "#dc3545"
                        );

                    if (!aceptar) {
                        return;
                    }

                    confirmado = true;

                    formulario.requestSubmit();
                }
            );
        }
    );
});


// VALIDAR DATOS DEL USUARIO EN EL SERVIDOR
async function validarDatosUsuario(formulario) {
    const campoUsuario =
        formulario.querySelector(
            "[name='Usuario']"
        );

    const campoCorreo =
        formulario.querySelector(
            "[name='Correo']"
        );

    const campoId =
        formulario.querySelector(
            "[name='Id']"
        );

    const errorUsuario =
        formulario.querySelector(
            "[data-error-usuario]"
        );

    const errorCorreo =
        formulario.querySelector(
            "[data-error-correo]"
        );

    const urlValidar =
        formulario.dataset.urlValidarDatos;

    if (!campoUsuario ||
        !campoCorreo) {
        return false;
    }

    // SI LA VISTA NO TIENE VALIDACIÓN PREVIA,
    // SE CONSERVA EL ENVÍO NORMAL AL SERVIDOR
    if (!urlValidar) {
        return true;
    }

    if (errorUsuario) {
        errorUsuario.textContent = "";
    }

    if (errorCorreo) {
        errorCorreo.textContent = "";
    }

    const parametros =
        new URLSearchParams({
            usuario: campoUsuario.value,
            correo: campoCorreo.value
        });

    if (campoId?.value) {
        parametros.append(
            "id",
            campoId.value
        );
    }

    try {
        const respuesta =
            await fetch(
                `${urlValidar}?${parametros.toString()}`
            );

        if (!respuesta.ok) {
            mostrarError(
                "Validación",
                "No fue posible validar los datos del usuario."
            );

            return false;
        }

        const resultado =
            await respuesta.json();

        if (resultado.valido) {
            return true;
        }

        if (resultado.errores?.Usuario &&
            errorUsuario) {
            errorUsuario.textContent =
                resultado.errores.Usuario[0];
        }

        if (resultado.errores?.Correo &&
            errorCorreo) {
            errorCorreo.textContent =
                resultado.errores.Correo[0];
        }

        return false;
    }
    catch {
        mostrarError(
            "Validación",
            "No fue posible validar los datos del usuario."
        );

        return false;
    }
}


// NORMALIZAR TEXTO PARA BÚSQUEDAS
function normalizarTexto(texto) {
    return texto
        .toLowerCase()
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .trim();
}