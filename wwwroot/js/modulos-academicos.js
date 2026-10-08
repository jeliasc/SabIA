document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-filtro-tarjetas]').forEach(input => {
        const sel = input.dataset.filtroTarjetas;
        const cards = [
            ...document.querySelectorAll(sel)
        ];

        const empty = document.querySelector(
            input.dataset.sinResultados || ''
        );

        const run = () => {
            const q = input.value
                .trim()
                .toLocaleLowerCase();

            let n = 0;

            cards.forEach(c => {
                const ok = (c.dataset.textoBusqueda || '')
                    .toLocaleLowerCase()
                    .includes(q);

                c.classList.toggle('d-none', !ok);

                if (ok) {
                    n++;
                }
            });

            if (empty) {
                empty.classList.toggle(
                    'd-none',
                    n !== 0
                );
            }
        };

        input.addEventListener('input', run);
    });
});