document.addEventListener("DOMContentLoaded", () => {
    const formulario = document.querySelector("form[data-configuracion-calificaciones]");
    if (!formulario) return;

    const categorias = formulario.querySelector("[data-categorias]");
    const plantillaCategoria = document.querySelector("#plantilla-categoria");
    const plantillaActividad = document.querySelector("#plantilla-actividad");

    const renumerar = () => {
        categorias.querySelectorAll(":scope > [data-categoria]").forEach((categoria, indiceCategoria) => {
            categoria.querySelectorAll(":scope > .card-body > .row [data-campo]").forEach(campo => {
                campo.name = `Categorias[${indiceCategoria}].${campo.dataset.campo}`;
            });
            categoria.querySelectorAll("[data-actividades] > [data-actividad]").forEach((actividad, indiceActividad) => {
                actividad.querySelectorAll("[data-campo]").forEach(campo => {
                    campo.name = `Categorias[${indiceCategoria}].Actividades[${indiceActividad}].${campo.dataset.campo}`;
                });
            });
        });
    };

    const agregarActividad = categoria => {
        categoria.querySelector("[data-actividades]")
            .append(plantillaActividad.content.cloneNode(true));
        renumerar();
        window.inicializarSelectsBusqueda?.();
    };

    formulario.addEventListener("click", evento => {
        const agregarCategoria = evento.target.closest("[data-agregar-categoria]");
        if (agregarCategoria) {
            const fragmento = plantillaCategoria.content.cloneNode(true);
            categorias.append(fragmento);
            const nueva = categorias.lastElementChild;
            agregarActividad(nueva);
            nueva.querySelector("input")?.focus();
            return;
        }

        const agregar = evento.target.closest("[data-agregar-actividad]");
        if (agregar) {
            agregarActividad(agregar.closest("[data-categoria]"));
            return;
        }

        const quitarActividad = evento.target.closest("[data-eliminar-actividad]");
        if (quitarActividad) {
            quitarActividad.closest("[data-actividad]").remove();
            renumerar();
            return;
        }

        const quitarCategoria = evento.target.closest("[data-eliminar-categoria]");
        if (quitarCategoria) {
            quitarCategoria.closest("[data-categoria]").remove();
            renumerar();
        }
    });

    formulario.addEventListener("submit", renumerar);
    renumerar();
});
