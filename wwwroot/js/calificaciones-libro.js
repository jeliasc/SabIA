document.addEventListener("DOMContentLoaded", () => {
    const libro = document.querySelector("[data-libro-calificaciones]");
    if (!libro) return;

    const metodo = Number(libro.dataset.metodo);
    const mostrar = valor => new Intl.NumberFormat("es-GT", {
        maximumFractionDigits: 4
    }).format(valor);

    const recalcular = fila => {
        const categorias = new Map();
        let pendientes = 0;

        fila.querySelectorAll("[data-calificacion]").forEach(celda => {
            const entrada = celda.querySelector("[data-entrada-nota]");
            const texto = entrada ? entrada.value : celda.dataset.notaValor;
            const nota = texto === "" ? null : Number(texto);
            if (nota === null || Number.isNaN(nota)) pendientes++;

            const id = celda.dataset.categoriaId;
            if (!categorias.has(id)) {
                categorias.set(id, {
                    tipo: Number(celda.dataset.tipo),
                    porcentaje: Number(celda.dataset.porcentaje),
                    maximo: Number(celda.dataset.maximoCategoria),
                    obtenido: 0
                });
            }
            if (nota !== null && !Number.isNaN(nota)) categorias.get(id).obtenido += nota;
            celda.dataset.notaValor = texto;
        });

        const lista = [...categorias.values()];
        const desempeno = lista.filter(x => x.tipo === 1).reduce((s, x) => s + x.obtenido, 0);
        const actitudinal = lista.filter(x => x.tipo === 2).reduce((s, x) => s + x.obtenido, 0);
        const total = metodo === 1
            ? lista.reduce((s, x) => s + x.obtenido, 0)
            : lista.reduce((s, x) => s + (x.maximo ? x.obtenido * x.porcentaje / x.maximo : 0), 0);

        const asignar = (nombre, valor) => {
            const destino = fila.querySelector(`[data-resumen="${nombre}"]`);
            if (destino) destino.textContent = valor;
        };
        asignar("desempeno", mostrar(desempeno));
        asignar("actitudinal", mostrar(actitudinal));
        asignar("total", mostrar(total));
        const etiquetaPendientes = fila.querySelector('[data-resumen="pendientes"]');
        if (etiquetaPendientes) {
            etiquetaPendientes.textContent = etiquetaPendientes.classList.contains("app-badge")
                ? `${pendientes} pendientes`
                : pendientes;
        }
    };

    libro.querySelectorAll("[data-entrada-nota]").forEach(entrada => {
        entrada.addEventListener("input", () => recalcular(entrada.closest("[data-fila-calificacion]")));
    });
});
