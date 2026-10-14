document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-form-material]').forEach(formulario => {
        const tipo = formulario.querySelector('[data-material-tipo]');
        const grupoArchivo = formulario.querySelector('[data-material-archivo]');
        const grupoUrl = formulario.querySelector('[data-material-url]');
        const grupoDescarga = formulario.querySelector('[data-material-descarga]');
        const archivo = formulario.querySelector('[data-material-archivo-input]');
        const url = formulario.querySelector('[data-material-url-input]');
        const descargable = grupoDescarga?.querySelector('input[type="checkbox"]');

        if (!tipo || !grupoArchivo || !grupoUrl || !grupoDescarga || !archivo || !url) {
            return;
        }

        const tieneArchivoActual = grupoArchivo.dataset.tieneArchivoActual === 'true';

        const actualizarOrigen = (limpiarIncompatible) => {
            const esEnlace = tipo.value === '7';

            grupoArchivo.hidden = esEnlace;
            grupoUrl.hidden = !esEnlace;
            grupoDescarga.hidden = esEnlace;

            archivo.disabled = esEnlace;
            archivo.required = !esEnlace && !tieneArchivoActual;
            url.disabled = !esEnlace;
            url.required = esEnlace;

            if (limpiarIncompatible) {
                if (esEnlace) {
                    archivo.value = '';
                    if (descargable) {
                        descargable.checked = false;
                    }
                } else {
                    url.value = '';
                }
            }
        };

        tipo.addEventListener('change', () => actualizarOrigen(true));
        actualizarOrigen(false);
    });
});
