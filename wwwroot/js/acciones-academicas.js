document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("form[data-confirmar-accion-academica]").forEach((formulario) => {
        formulario.addEventListener("submit", async (evento) => {
            if (formulario.dataset.confirmado === "true") {
                return;
            }

            evento.preventDefault();

            const confirmado = await confirmarAccion(
                formulario.dataset.confirmarTitulo || "Confirmar operación",
                formulario.dataset.confirmarMensaje || "¿Desea continuar con esta operación?",
                formulario.dataset.confirmarTexto || "Confirmar",
                "Cancelar",
                formulario.dataset.confirmarColor || "#dc3545"
            );

            if (confirmado) {
                formulario.dataset.confirmado = "true";
                formulario.requestSubmit();
            }
        });
    });
});
