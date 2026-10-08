(() => {
    const inicializarSelects = () => {
        if (typeof window.TomSelect !== "function") {
            return;
        }

        document.querySelectorAll("select[data-select-busqueda]").forEach(select => {
            if (select.tomselect) {
                return;
            }

            const etiqueta = select.labels?.[0]?.textContent?.trim();
            const placeholder = select.dataset.searchPlaceholder ?? "Buscar...";

            new window.TomSelect(select, {
                allowEmptyOption: true,
                closeAfterSelect: true,
                create: false,
                diacritics: true,
                maxOptions: null,
                placeholder,
                searchField: ["text"],
                selectOnTab: true,
                onInitialize() {
                    if (etiqueta) {
                        this.control_input.setAttribute("aria-label", etiqueta);
                    }
                }
            });

            select.addEventListener("change", () => {
                if (window.jQuery?.validator) {
                    window.jQuery(select).valid();
                }
            });
        });

        window.setTimeout(() => {
            if (!window.jQuery?.validator) {
                return;
            }

            window.jQuery("form:has(select[data-select-busqueda])").each((_, form) => {
                const validator = window.jQuery(form).data("validator");

                if (validator) {
                    validator.settings.ignore = ":hidden:not(select[data-select-busqueda])";
                }
            });
        }, 0);
    };

    window.inicializarSelectsBusqueda = inicializarSelects;

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", inicializarSelects, { once: true });
    } else {
        inicializarSelects();
    }
})();
