document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-retirar-inscripcion]").forEach(formulario => {
        formulario.addEventListener("submit", async evento => {
            evento.preventDefault();

            const confirmado = await confirmarAccion(
                "¿Retirar esta inscripción?",
                "La inscripción quedará en el historial y el alumno podrá reinscribirse conforme a las reglas académicas.",
                "Sí, retirar",
                "Cancelar",
                "#dc3545"
            );

            if (confirmado) {
                formulario.submit();
            }
        });
    });
});
