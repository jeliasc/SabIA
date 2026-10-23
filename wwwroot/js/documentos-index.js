
document.addEventListener("DOMContentLoaded", function () {
    const contenedor = document.getElementById("documentosMovil");
    if (!contenedor) return;

    const botonBusqueda = contenedor.querySelector(
        "[data-mostrar-busqueda-documentos]"
    );

    const contenedorBusqueda = contenedor.querySelector(
        "[data-contenedor-busqueda-documentos]"
    );

    const buscador = document.getElementById("buscarDocumentoMovil");

    if (botonBusqueda && contenedorBusqueda) {
        botonBusqueda.addEventListener("click", function () {
            const estabaOculto = contenedorBusqueda.classList.contains("d-none");

            contenedorBusqueda.classList.toggle("d-none", !estabaOculto);
            botonBusqueda.setAttribute("aria-expanded", String(estabaOculto));

            if (estabaOculto) buscador?.focus();
        });
    }

    const tarjetas = Array.from(
        contenedor.querySelectorAll(".documento-movil-item")
    );

    const anterior = document.getElementById("anteriorDocumentosMovil");
    const siguiente = document.getElementById("siguienteDocumentosMovil");
    const contador = document.getElementById("contadorDocumentosMovil");
    const vacio = document.getElementById("sinDocumentosMovil");

    if (!buscador || !anterior || !siguiente || !contador || !vacio) return;

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

        tarjetas.forEach(tarjeta => tarjeta.classList.add("d-none"));

        filtradas
            .slice((pagina - 1) * porPagina, pagina * porPagina)
            .forEach(tarjeta => tarjeta.classList.remove("d-none"));

        vacio.classList.toggle("d-none", filtradas.length !== 0);

        contador.textContent = filtradas.length === 0
            ? "0 registros"
            : `Página ${pagina} de ${totalPaginas} · ${filtradas.length} registros`;

        anterior.disabled = pagina <= 1;
        siguiente.disabled = pagina >= totalPaginas;
    }

    buscador.addEventListener("input", function () {
        pagina = 1;
        actualizar();
    });

    anterior.addEventListener("click", function () {
        if (pagina > 1) {
            pagina--;
            actualizar();
        }
    });

    siguiente.addEventListener("click", function () {
        if (!siguiente.disabled) {
            pagina++;
            actualizar();
        }
    });

    actualizar();
});
