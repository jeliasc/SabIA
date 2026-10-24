
document.addEventListener("DOMContentLoaded", function () {
    const contenedor = document.getElementById("unidadesMovil");
    if (!contenedor) return;

    // Mostrar y ocultar el buscador móvil.
    const botonBusqueda = contenedor.querySelector(
        "[data-mostrar-busqueda-unidades]"
    );

    const contenedorBusqueda = contenedor.querySelector(
        "[data-contenedor-busqueda-unidades]"
    );

    const buscador = document.getElementById("buscarUnidadMovil");

    if (botonBusqueda && contenedorBusqueda) {
        botonBusqueda.addEventListener("click", function () {
            const estabaOculto = contenedorBusqueda.classList.contains("d-none");

            contenedorBusqueda.classList.toggle("d-none", !estabaOculto);
            botonBusqueda.setAttribute("aria-expanded", String(estabaOculto));

            if (estabaOculto) {
                buscador?.focus();
            }
        });
    }

    // Obtener las tarjetas y los controles de paginación.
    const tarjetas = Array.from(
        contenedor.querySelectorAll(".unidad-movil-item")
    );

    const anterior = document.getElementById("anteriorUnidadesMovil");
    const siguiente = document.getElementById("siguienteUnidadesMovil");
    const contador = document.getElementById("contadorUnidadesMovil");
    const vacio = document.getElementById("sinUnidadesMovil");

    if (!buscador || !anterior || !siguiente || !contador || !vacio) {
        return;
    }

    const porPagina = 10;
    let pagina = 1;

    function normalizar(texto) {
        return (texto || "")
            .toLocaleLowerCase("es")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "");
    }

    function actualizar() {
        const termino = normalizar(buscador.value.trim());

        const filtradas = tarjetas.filter(tarjeta =>
            normalizar(tarjeta.dataset.busqueda).includes(termino)
        );

        const totalPaginas = Math.max(
            1,
            Math.ceil(filtradas.length / porPagina)
        );

        pagina = Math.min(pagina, totalPaginas);

        // Ocultar todas las tarjetas.
        tarjetas.forEach(tarjeta => {
            tarjeta.classList.add("d-none");
        });

        // Mostrar únicamente las tarjetas de la página actual.
        filtradas
            .slice((pagina - 1) * porPagina, pagina * porPagina)
            .forEach(tarjeta => {
                tarjeta.classList.remove("d-none");
            });

        // Mensaje cuando no hay coincidencias.
        vacio.classList.toggle("d-none", filtradas.length !== 0);

        // Actualizar contador.
        contador.textContent = filtradas.length === 0
            ? "0 registros"
            : `Página ${pagina} de ${totalPaginas} · ${filtradas.length} registros`;

        // Habilitar o deshabilitar los botones.
        anterior.disabled = pagina <= 1;
        siguiente.disabled = pagina >= totalPaginas;
    }

    // Buscar mientras se escribe.
    buscador.addEventListener("input", function () {
        pagina = 1;
        actualizar();
    });

    // Página anterior.
    anterior.addEventListener("click", function () {
        if (pagina > 1) {
            pagina--;
            actualizar();
        }
    });

    // Página siguiente.
    siguiente.addEventListener("click", function () {
        if (!siguiente.disabled) {
            pagina++;
            actualizar();
        }
    });

    // Inicializar la vista móvil.
    actualizar();
});
