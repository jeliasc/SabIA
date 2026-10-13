// MOSTRAR ERROR
function mostrarError(
    titulo,
    mensaje
) {
    Swal.fire({
        icon: "error",
        title: titulo,
        text: mensaje,
        confirmButtonText: "Aceptar"
    });
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