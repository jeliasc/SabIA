document.addEventListener("DOMContentLoaded", () => {
    const formulario = document.querySelector("[data-reabrir-entrega]");

    if (!formulario) {
        return;
    }

    formulario.addEventListener("submit", async evento => {
        if (!formulario.checkValidity()) {
            return;
        }

        evento.preventDefault();

        const confirmado = await confirmarAccion(
            "¿Reabrir esta entrega?",
            "Se habilitará un plazo individual y se conservará el historial de intentos.",
            "Sí, reabrir",
            "Cancelar",
            "#dc3545"
        );

        if (confirmado) {
            formulario.submit();
        }
    });
});
