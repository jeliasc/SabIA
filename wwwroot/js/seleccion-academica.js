(() => {
    const opciones = select => Array.from(select.options).map(option => ({
        value: option.value,
        text: option.text,
        dataset: { ...option.dataset }
    }));

    const actualizar = (select, originales, permite, conservar = false) => {
        const visibles = originales.filter(
            option => !option.value || permite(option)
        );

        const valor = conservar &&
            visibles.some(option => option.value === select.value)
            ? select.value
            : "";

        const habilitado = visibles.some(option => option.value);

        if (select.tomselect) {
            const control = select.tomselect;

            control.clear(true);
            control.clearOptions();

            control.addOption(
                visibles.map(option => ({
                    value: option.value,
                    text: option.text
                }))
            );

            if (habilitado) {
                control.enable();
            } else {
                control.disable();
            }

            if (valor) {
                control.setValue(valor, true);
            }

            control.refreshOptions(false);
        } else {
            select.replaceChildren(
                ...visibles.map(option => {
                    const elemento = document.createElement("option");
                    elemento.value = option.value;
                    elemento.text = option.text;
                    Object.assign(elemento.dataset, option.dataset);
                    return elemento;
                })
            );

            select.disabled = !habilitado;
            select.value = valor;
        }
    };

    const valor = select => select.value || "";

    const iniciar = contenedor => {
        const ciclo = contenedor.querySelector("[data-academico-ciclo]");
        const carrera = contenedor.querySelector("[data-academico-carrera]");
        const grado = contenedor.querySelector("[data-academico-grado]");
        const seccion = contenedor.querySelector("[data-academico-seccion]");
        if (!ciclo || !carrera || !grado || !seccion) return;

        const originales = new Map([[carrera, opciones(carrera)], [grado, opciones(grado)], [seccion, opciones(seccion)]]);
        const actualizarSeccion = conservar => actualizar(seccion, originales.get(seccion), option =>
            option.dataset.cicloEscolarId === valor(ciclo) &&
            option.dataset.carrera === valor(carrera) &&
            option.dataset.gradoId === valor(grado), conservar);
        const actualizarGrado = conservar => {
            actualizar(grado, originales.get(grado), option =>
                option.dataset.cicloEscolarId === valor(ciclo) && option.dataset.carrera === valor(carrera), conservar);
            actualizarSeccion(conservar);
        };
        const actualizarCarrera = conservar => {
            actualizar(carrera, originales.get(carrera), option => option.dataset.cicloEscolarId === valor(ciclo), conservar);
            actualizarGrado(conservar);
        };

        ciclo.addEventListener("change", () => actualizarCarrera(false));
        carrera.addEventListener("change", () => actualizarGrado(false));
        grado.addEventListener("change", () => actualizarSeccion(false));
        actualizarCarrera(true);
    };

    const iniciarTodo = () => document.querySelectorAll("[data-seleccion-academica]").forEach(iniciar);
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", iniciarTodo, { once: true });
    else iniciarTodo();
})();
