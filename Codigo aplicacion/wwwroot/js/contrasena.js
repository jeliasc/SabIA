document.addEventListener("DOMContentLoaded", function () {
    // ICONO OJO
    const iconoMostrar = `
<svg xmlns="http://www.w3.org/2000/svg"
     width="18"
     height="18"
     fill="currentColor"
     viewBox="0 0 16 16">
    <path d="M16 8s-3-5.5-8-5.5S0 8 0 8s3 5.5 8 5.5S16 8 16 8M1.173 8a13 13 0 0 1 1.66-2.043C4.12 4.668 5.88 3.5 8 3.5s3.879 1.168 5.168 2.457A13 13 0 0 1 14.828 8c-.058.087-.122.183-.195.288-.335.48-.83 1.12-1.465 1.755C11.879 11.332 10.119 12.5 8 12.5s-3.879-1.168-5.168-2.457A13 13 0 0 1 1.172 8z"/>
    <path d="M8 5.5a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5M6.5 8a1.5 1.5 0 1 1 3 0 1.5 1.5 0 0 1-3 0"/>
</svg>
`;

    // ICONO OJO CON DIAGONAL
    const iconoOcultar = `
<svg xmlns="http://www.w3.org/2000/svg"
     width="18"
     height="18"
     fill="currentColor"
     viewBox="0 0 16 16">
    <path d="M13.359 11.238C14.27 10.296 15.08 9.172 16 8c-3-5.5-8-5.5-8-5.5a7.03 7.03 0 0 0-2.79.588l.77.771A5.94 5.94 0 0 1 8 3.5c2.12 0 3.879 1.168 5.168 2.457A13 13 0 0 1 14.828 8a13 13 0 0 1-1.469 1.957l-.734-.719z"/>
    <path d="M11.297 9.176a3.5 3.5 0 0 0-4.473-4.473l.823.823a2.5 2.5 0 0 1 2.827 2.827z"/>
    <path d="M3.35 5.47A13 13 0 0 0 1.172 8a13 13 0 0 0 1.66 2.043C4.121 11.332 5.881 12.5 8 12.5a5.94 5.94 0 0 0 2.021-.359l.77.771A7.03 7.03 0 0 1 8 13.5S3 13.5 0 8c.917-1.683 2.053-2.979 3.35-3.97z"/>
    <path d="M5.354 7.475a2.5 2.5 0 0 0 3.171 3.171z"/>
    <path d="M15.854 15.146a.5.5 0 0 1-.708 0l-14-14a.5.5 0 1 1 .708-.708l14 14a.5.5 0 0 1 0 .708"/>
</svg>
`;

    // MOSTRAR U OCULTAR CONTRASEÑAS
    const botonesMostrar =
        document.querySelectorAll("[data-mostrar-contrasena]");

    botonesMostrar.forEach(function (boton) {
        boton.addEventListener("click", function () {
            const grupo = boton.closest(".input-group");

            const campo =
                grupo.querySelector("[data-contrasena]");

            if (!campo) {
                return;
            }

            const estaOculta =
                campo.type === "password";

            campo.type =
                estaOculta ? "text" : "password";

            // ACTUALIZAR ICONO DEL BOTÓN
            boton.innerHTML =
                estaOculta ? iconoOcultar : iconoMostrar;

            // ACTUALIZAR DESCRIPCIÓN DEL BOTÓN
            boton.setAttribute(
                "aria-label",
                estaOculta
                    ? "Ocultar contraseña"
                    : "Mostrar contraseña"
            );

            boton.setAttribute(
                "title",
                estaOculta
                    ? "Ocultar contraseña"
                    : "Mostrar contraseña"
            );
        });
    });

    // VALIDAR REQUISITOS DE LA NUEVA CONTRASEÑA
    const nuevaContrasena =
        document.querySelector("[data-nueva-contrasena]");

    if (!nuevaContrasena) {
        return;
    }

    nuevaContrasena.addEventListener(
        "input",
        function () {
            validarRequisitosContrasena(
                nuevaContrasena.value
            );
        }
    );
});

// VALIDAR REQUISITOS VISUALES DE CONTRASEÑA
function validarRequisitosContrasena(contrasena) {
    actualizarRequisito(
        "longitud",
        contrasena.length >= 10
    );

    actualizarRequisito(
        "mayuscula",
        /[A-Z]/.test(contrasena)
    );

    actualizarRequisito(
        "minuscula",
        /[a-z]/.test(contrasena)
    );

    actualizarRequisito(
        "numero",
        /[0-9]/.test(contrasena)
    );

    actualizarRequisito(
        "especial",
        /[^A-Za-z0-9]/.test(contrasena)
    );
}

// ACTUALIZAR ESTADO VISUAL DE UN REQUISITO
function actualizarRequisito(requisito, cumple) {
    const elemento =
        document.querySelector(
            `[data-requisito="${requisito}"]`
        );

    if (!elemento) {
        return;
    }

    const texto =
        elemento.textContent
            .replace("✓", "")
            .replace("○", "")
            .trim();

    elemento.textContent =
        `${cumple ? "✓" : "○"} ${texto}`;

    elemento.classList.toggle(
        "text-success",
        cumple
    );

    elemento.classList.toggle(
        "text-muted",
        !cumple
    );
}