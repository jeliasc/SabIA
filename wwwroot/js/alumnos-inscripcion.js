(() => {
    const iniciar = () => {
        const bloque = document.querySelector("[data-alumnos-inscripcion]");
        const alumno = bloque?.querySelector("select");
        const ciclo = document.querySelector("[data-academico-ciclo]");
        const aviso = document.querySelector("[data-no-alumnos-inscripcion]");
        if (!alumno || !ciclo || !aviso) return;

        const opciones = Array.from(alumno.options).map(option => ({
            value: option.value,
            text: option.text,
            ciclosNoDisponibles: (option.dataset.ciclosNoDisponibles || "").split(",").filter(Boolean)
        }));

        const actualizar = () => {
            const cicloId = ciclo.value;
            const visibles = opciones.filter(option => !option.value ||
                cicloId && !option.ciclosNoDisponibles.includes(cicloId));
            const seleccionado = visibles.some(option => option.value === alumno.value) ? alumno.value : "";

            alumno.disabled = !cicloId || visibles.length === 1;
            aviso.classList.toggle("d-none", !cicloId || visibles.length > 1);

            if (alumno.tomselect) {
                const control = alumno.tomselect;
                control.clear(true);
                control.clearOptions();
                control.addOption(visibles.map(option => ({ value: option.value, text: option.text })));
                control.refreshOptions(false);
                if (seleccionado) control.setValue(seleccionado, true);
            } else {
                alumno.replaceChildren(...visibles.map(option => {
                    const elemento = document.createElement("option");
                    elemento.value = option.value;
                    elemento.text = option.text;
                    return elemento;
                }));
                alumno.value = seleccionado;
            }
        };

        ciclo.addEventListener("change", actualizar);
        actualizar();
    };

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", iniciar, { once: true });
    else iniciar();
})();
