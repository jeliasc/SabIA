// CUESTIONARIOS
// - Configuración: muestra solo los campos de tiempo que aplican.
// - Intento: relojes, envío automático al vencer el tiempo,
//   navegación entre preguntas y guardado automático.
// El servidor vuelve a validar tiempos y orden; este script solo
// mejora la experiencia del alumno.

(function () {
    "use strict";

    // =================================================================
    // CONFIGURACIÓN (DOCENTE)
    // =================================================================

    function iniciarConfiguracion(formulario) {
        const modalidad = formulario.querySelector("[data-modalidad-tiempo]");
        const general = formulario.querySelector("[data-tiempo-general]");
        const porPregunta = formulario.querySelector("[data-tiempo-pregunta]");
        const navegacion = formulario.querySelector("[data-permitir-navegacion]");

        if (!modalidad) return;

        const actualizar = () => {
            const valor = modalidad.value;
            general.hidden = valor !== "2";
            porPregunta.hidden = valor !== "3";

            // Con tiempo por pregunta no se permite regresar.
            if (navegacion) {
                navegacion.disabled = valor === "3";
                if (valor === "3") navegacion.checked = false;
            }
        };

        modalidad.addEventListener("change", actualizar);
        actualizar();
    }

    // =================================================================
    // INTENTO (ALUMNO)
    // =================================================================

    function formatear(segundos) {
        const s = Math.max(0, segundos);
        const minutos = Math.floor(s / 60);
        const resto = s % 60;
        return `${String(minutos).padStart(2, "0")}:${String(resto).padStart(2, "0")}`;
    }

    // Cuenta regresiva basada en la hora de inicio de la página, para que
    // una pestaña en segundo plano no se atrase.
    function reloj(elemento, segundos, alTerminar) {
        if (!Number.isFinite(segundos)) return;

        const fin = Date.now() + segundos * 1000;
        let terminado = false;

        const tick = () => {
            const restantes = Math.ceil((fin - Date.now()) / 1000);
            if (elemento) {
                elemento.textContent = formatear(restantes);
                elemento.closest(".badge")?.classList.toggle("text-bg-danger", restantes <= 30);
            }

            if (restantes <= 0 && !terminado) {
                terminado = true;
                alTerminar();
                return;
            }

            setTimeout(tick, 250);
        };

        tick();
    }

    function leerNumero(valor) {
        if (valor === undefined || valor === null || valor === "") return NaN;
        return Number(valor);
    }

    function iniciarIntento(contenedor) {
        const navegable = contenedor.dataset.navegable === "true";
        const formFinal = contenedor.querySelector("[data-form-final]");
        const formPregunta = contenedor.querySelector("[data-form-pregunta]");
        let enviando = false;

        const enviar = (formulario) => {
            if (enviando || !formulario) return;
            enviando = true;
            formulario.querySelectorAll("button").forEach(b => (b.disabled = true));
            HTMLFormElement.prototype.submit.call(formulario);
        };

        // Evita doble envío con el botón. Se revisa después de los demás
        // manejadores, por si alguno canceló el envío para pedir confirmación.
        [formFinal, formPregunta].forEach(f => {
            f?.addEventListener("submit", (evento) => {
                setTimeout(() => {
                    if (evento.defaultPrevented) return;
                    enviando = true;
                    f.querySelectorAll("button").forEach(b => (b.disabled = true));
                }, 0);
            });
        });

        // Tiempo total: al vencer se finaliza el intento.
        reloj(
            contenedor.querySelector("[data-reloj-total]"),
            leerNumero(contenedor.dataset.segundos),
            () => enviar(formFinal)
        );

        // Tiempo por pregunta: al vencer se envía la respuesta actual.
        reloj(
            contenedor.querySelector("[data-reloj-pregunta]"),
            leerNumero(contenedor.dataset.segundosPregunta),
            () => enviar(formPregunta)
        );

        if (navegable) {
            iniciarNavegacion(contenedor, formFinal, () => enviando, v => (enviando = v));
        }
    }

    function iniciarNavegacion(contenedor, formulario, estaEnviando, marcarEnvio) {
        const tarjetas = Array.from(contenedor.querySelectorAll("[data-pregunta]"));
        const botonesNav = Array.from(contenedor.querySelectorAll("[data-ir-a]"));
        const anterior = contenedor.querySelector("[data-anterior]");
        const siguiente = contenedor.querySelector("[data-siguiente]");
        const numeroActual = contenedor.querySelector("[data-numero-actual]");
        const estadoGuardado = contenedor.querySelector("[data-estado-guardado]");
        const urlGuardar = contenedor.dataset.urlGuardar;
        const token = formulario.querySelector("input[name='__RequestVerificationToken']")?.value;
        const intentoId = formulario.querySelector("input[name='IntentoId']")?.value;
        const pendientes = new Set();
        let actual = 0;

        const respondida = (tarjeta) =>
            Array.from(tarjeta.querySelectorAll("input")).some(i =>
                ((i.type === "radio" || i.type === "checkbox") && i.checked) ||
                (i.type === "text" && i.value.trim() !== ""));

        const actualizarNavegador = () => {
            botonesNav.forEach((boton, indice) => {
                const lista = respondida(tarjetas[indice]);
                boton.classList.toggle("app-btn-soft-success", lista);
                boton.classList.toggle("app-btn-soft-secondary", !lista);
                boton.classList.toggle("fw-bold", indice === actual);
                boton.setAttribute("aria-current", indice === actual ? "true" : "false");
            });
        };

        const mostrar = (indice) => {
            if (indice < 0 || indice >= tarjetas.length) return;
            guardar(actual);
            actual = indice;
            tarjetas.forEach((t, i) => (t.hidden = i !== actual));
            if (numeroActual) numeroActual.textContent = String(actual + 1);
            anterior.disabled = actual === 0;
            siguiente.disabled = actual === tarjetas.length - 1;
            actualizarNavegador();
            tarjetas[actual].querySelector("input:not([type=hidden])")?.focus({ preventScroll: true });
        };

        // Envía la respuesta de una pregunta al servidor. Los campos se
        // renombran de "Respuestas[i].X" a "Respuesta.X".
        async function guardar(indice) {
            if (!pendientes.has(indice) || !urlGuardar) return;
            pendientes.delete(indice);

            const datos = new FormData();
            datos.append("__RequestVerificationToken", token);
            datos.append("IntentoId", intentoId);

            tarjetas[indice].querySelectorAll("input").forEach(entrada => {
                if ((entrada.type === "radio" || entrada.type === "checkbox") && !entrada.checked) return;
                const nombre = entrada.name.replace(/^Respuestas\[\d+\]\./, "Respuesta.");
                datos.append(nombre, entrada.value);
            });

            try {
                const respuesta = await fetch(urlGuardar, { method: "POST", body: datos });
                const resultado = await respuesta.json();

                if (resultado.finalizado && resultado.urlResultado) {
                    marcarEnvio(true);
                    window.location.href = resultado.urlResultado;
                    return;
                }

                if (estadoGuardado) {
                    estadoGuardado.textContent = resultado.exitoso
                        ? "Respuestas guardadas."
                        : (resultado.mensaje || "No se pudo guardar la respuesta.");
                }
            } catch {
                // Sin conexión: la respuesta queda en el formulario y se envía al finalizar.
                pendientes.add(indice);
                if (estadoGuardado) {
                    estadoGuardado.textContent = "Sin conexión. Sus respuestas se enviarán al finalizar.";
                }
            }
        }

        tarjetas.forEach((tarjeta, indice) => {
            tarjeta.addEventListener("change", () => {
                pendientes.add(indice);
                actualizarNavegador();
            });
            tarjeta.addEventListener("input", () => pendientes.add(indice));
        });

        anterior.addEventListener("click", () => mostrar(actual - 1));
        siguiente.addEventListener("click", () => mostrar(actual + 1));
        botonesNav.forEach(boton =>
            boton.addEventListener("click", () => mostrar(Number(boton.dataset.irA))));

        // Confirmación si hay preguntas sin responder.
        formulario.addEventListener("submit", async (evento) => {
            if (estaEnviando()) {
                evento.preventDefault();
                return;
            }

            const sinResponder = tarjetas.filter(t => !respondida(t)).length;
            if (sinResponder === 0 || evento.defaultPrevented) return;

            evento.preventDefault();

            const mensaje = `Tiene ${sinResponder} pregunta(s) sin responder. ¿Desea finalizar de todas formas?`;
            const confirmado = typeof confirmarAccion === "function"
                ? await confirmarAccion("Finalizar cuestionario", mensaje, "Finalizar", "Seguir respondiendo")
                : window.confirm(mensaje);

            if (confirmado) {
                marcarEnvio(true);
                formulario.querySelectorAll("button").forEach(b => (b.disabled = true));
                HTMLFormElement.prototype.submit.call(formulario);
            }
        });

        mostrar(0);
    }

    document.querySelectorAll("[data-config-cuestionario]").forEach(iniciarConfiguracion);
    document.querySelectorAll("[data-intento-cuestionario]").forEach(iniciarIntento);
})();
