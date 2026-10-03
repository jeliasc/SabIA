document.addEventListener("DOMContentLoaded", function () {
    // MOSTRAR ALERTAS GENERADAS POR LAS VISTAS
    const alertas =
        document.querySelectorAll("[data-alerta]");

    alertas.forEach(function (alerta) {
        const tipo =
            alerta.dataset.tipo || "info";

        const titulo =
            alerta.dataset.titulo || "";

        const mensaje =
            alerta.dataset.mensaje || "";

        mostrarAlerta(
            tipo,
            titulo,
            mensaje
        );
    });
});

// MOSTRAR ALERTA GENERAL
function mostrarAlerta(
    tipo,
    titulo,
    mensaje
) {
    Swal.fire({
        icon: tipo,
        title: titulo,
        text: mensaje,
        confirmButtonText: "Aceptar"
    });
}

// MOSTRAR ERROR
function mostrarError(
    titulo,
    mensaje
) {
    mostrarAlerta(
        "error",
        titulo,
        mensaje
    );
}

// MOSTRAR MENSAJE DE ÉXITO
function mostrarExito(
    titulo,
    mensaje
) {
    mostrarAlerta(
        "success",
        titulo,
        mensaje
    );
}

// MOSTRAR ADVERTENCIA
function mostrarAdvertencia(
    titulo,
    mensaje
) {
    mostrarAlerta(
        "warning",
        titulo,
        mensaje
    );
}

// MOSTRAR INFORMACIÓN
function mostrarInformacion(
    titulo,
    mensaje
) {
    mostrarAlerta(
        "info",
        titulo,
        mensaje
    );
}

// SOLICITAR CONFIRMACIÓN
async function confirmarAccion(
    titulo,
    mensaje,
    textoConfirmar = "Aceptar",
    textoCancelar = "Cancelar",
    colorConfirmar = "#198754"
) {
    const resultado =
        await Swal.fire({
            icon: "warning",
            title: titulo,
            text: mensaje,

            showCancelButton: true,

            confirmButtonText:
                textoConfirmar,

            cancelButtonText:
                textoCancelar,

            confirmButtonColor:
                colorConfirmar,

            cancelButtonColor:
                "#6c757d",

            reverseButtons: true
        });

    return resultado.isConfirmed;
}