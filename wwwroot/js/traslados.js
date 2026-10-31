        document.addEventListener('DOMContentLoaded', () => {
            const t = document.getElementById('Tipo'), c = document.getElementById('consolidacion');
            const f = () => c.hidden = t.value !== 'Consolidacion' && t.value !== '3';
            t.addEventListener('change', f);
            f();
        });