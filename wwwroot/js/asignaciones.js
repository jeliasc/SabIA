(() => {
    const inicializarFormulario = () => {
        const seccion = document.querySelector("select[data-asignacion-seccion]");
        const curso = document.querySelector("select[data-asignacion-curso]");

        if (!seccion || !curso) {
            return;
        }

        const cursos = Array.from(curso.options)
            .filter(opcion => opcion.value)
            .map(opcion => ({
                value: opcion.value,
                text: opcion.text,
                gradoId: opcion.dataset.gradoId
            }));

        const actualizarCursos = () => {
            const opcionSeccion = seccion.options[seccion.selectedIndex];
            const gradoId = opcionSeccion?.dataset.gradoId ?? "";
            const cursoSeleccionado = curso.value;
            const opciones = cursos.filter(opcion => opcion.gradoId === gradoId);
            const conservarCurso = opciones.some(
                opcion => opcion.value === cursoSeleccionado);

            if (curso.tomselect) {
                curso.tomselect.clear(true);
                curso.tomselect.clearOptions();

                if (gradoId) {
                    curso.tomselect.addOption(opciones);
                    curso.tomselect.enable();

                    if (conservarCurso) {
                        curso.tomselect.setValue(cursoSeleccionado, true);
                    }
                } else {
                    curso.tomselect.disable();
                }

                curso.tomselect.refreshOptions(false);
                return;
            }

            curso.replaceChildren(new Option("Seleccione...", ""));

            opciones.forEach(opcion => {
                curso.add(new Option(opcion.text, opcion.value));
            });

            curso.disabled = !gradoId;

            if (conservarCurso) {
                curso.value = cursoSeleccionado;
            }
        };

        seccion.addEventListener("change", actualizarCursos);
        actualizarCursos();
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", inicializarFormulario, { once: true });
    } else {
        inicializarFormulario();
    }
})();
