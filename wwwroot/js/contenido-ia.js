document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('[data-trabajo-ia]').forEach(card => {
    const id = card.dataset.jobId;
    const estado = card.querySelector('[data-estado]');
    const resultado = card.querySelector('[data-resultado]');
    const error = card.querySelector('[data-error]');
    let intentos = 0;
    const consultar = async () => {
      intentos++;
      try {
        const r = await fetch(`/ContenidoIA/Estado?id=${encodeURIComponent(id)}`, { credentials: 'same-origin' });
        const data = await r.json();
        if (!data.exitoso) throw new Error(data.mensaje || 'No fue posible consultar el trabajo.');
        const valor = (data.estado || '').toLowerCase();
        estado.textContent = data.estado || 'Desconocido';
        if (['completado', 'completada', 'completed', 'listo', 'finalizado', 'finalizada', 'success'].includes(valor)) {
          estado.className = 'app-badge app-badge-success';
          if (data.resultado) { resultado.classList.remove('d-none'); resultado.querySelector('pre').textContent = typeof data.resultado === 'string' ? data.resultado : JSON.stringify(data.resultado, null, 2); }
          return;
        }
        if (['error', 'failed', 'fallido', 'fallida'].includes(valor)) { estado.className = 'app-badge app-badge-danger'; return; }
        estado.className = 'app-badge app-badge-secondary';
        if (intentos < 120) window.setTimeout(consultar, 3000);
      } catch (e) {
        error.textContent = e.message;
        error.classList.remove('d-none');
        if (intentos < 10) window.setTimeout(consultar, 5000);
      }
    };
    consultar();
  });
});
