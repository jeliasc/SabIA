from flask import Flask, request, jsonify, render_template_string
import requests

app = Flask(__name__)

# Datos de la API de SabIA (vive en el servidor, accesible por Tailscale)
SABIA_API_URL = "http://100.118.205.1:8001/api"
SABIA_API_KEY = "Umz3rNvNZPEt9MJrg29xmYD3DW4XpF6AzEHKSKWUwBM3SFyXZ5EtbK5aRSja4Dqe6mjfPxJpgvUHBaBy"

TIPOS_LABELS = {
    "cuestionario": "Cuestionario",
    "resumen": "Resumen",
    "hoja_trabajo": "Hoja de trabajo",
    "glosario": "Glosario",
}

PAGINA = """
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8">
<title>SabIA - Demo local</title>
<style>
  body { font-family: sans-serif; max-width: 680px; margin: 40px auto; padding: 0 20px; color: #222; }
  h1 { font-size: 22px; }
  h2 { font-size: 16px; margin-top: 28px; }
  textarea { width: 100%; height: 70px; font-size: 14px; padding: 10px; box-sizing: border-box; margin-top: 6px; }
  button { padding: 10px 22px; font-size: 14px; margin-top: 16px; cursor: pointer; }
  .tipo-item { border: 1px solid #ddd; border-radius: 8px; padding: 12px 14px; margin-bottom: 10px; }
  .tipo-item label { font-weight: bold; cursor: pointer; }
  .resultado-card { margin-top: 16px; padding: 16px; background: #f5f5f5; border-radius: 8px; }
  .resultado-card h3 { margin: 0 0 6px; font-size: 15px; }
  .estado-tipo { font-weight: bold; color: #555; margin: 0 0 8px; }
  .contenido-tipo { white-space: pre-wrap; line-height: 1.5; }
</style>
</head>
<body>
  <h1>SabIA · demo local</h1>
  <p>Subir un PDF, marca qué quieres generar, y da instrucciones a cada tipo si quieres.</p>

  <h2>1. Documento base</h2>
  <input type="file" id="pdf" accept="application/pdf">

  <h2>2. ¿Qué quieres generar?</h2>
  <div class="tipo-item">
    <label><input type="checkbox" class="tipo-check" value="cuestionario"> Cuestionario</label>
    <textarea class="tipo-instrucciones" data-tipo="cuestionario" placeholder="Instrucciones para el cuestionario (opcional)" style="display:none;"></textarea>
  </div>
  <div class="tipo-item">
    <label><input type="checkbox" class="tipo-check" value="resumen"> Resumen</label>
    <textarea class="tipo-instrucciones" data-tipo="resumen" placeholder="Instrucciones para el resumen (opcional)" style="display:none;"></textarea>
  </div>
  <div class="tipo-item">
    <label><input type="checkbox" class="tipo-check" value="hoja_trabajo"> Hoja de trabajo</label>
    <textarea class="tipo-instrucciones" data-tipo="hoja_trabajo" placeholder="Instrucciones para la hoja de trabajo (opcional)" style="display:none;"></textarea>
  </div>
  <div class="tipo-item">
    <label><input type="checkbox" class="tipo-check" value="glosario"> Glosario</label>
    <textarea class="tipo-instrucciones" data-tipo="glosario" placeholder="Instrucciones para el glosario (opcional)" style="display:none;"></textarea>
  </div>

  <button onclick="generarDesdeDocumento()">Generar contenido</button>

  <div id="resultados"></div>

<script>
document.querySelectorAll('.tipo-check').forEach(function(cb) {
  cb.addEventListener('change', function() {
    var textarea = document.querySelector('.tipo-instrucciones[data-tipo="' + cb.value + '"]');
    textarea.style.display = cb.checked ? 'block' : 'none';
  });
});

var ETIQUETAS = {
  cuestionario: 'Cuestionario',
  resumen: 'Resumen',
  hoja_trabajo: 'Hoja de trabajo',
  glosario: 'Glosario'
};

async function generarDesdeDocumento() {
  var archivo = document.getElementById('pdf').files[0];
  if (!archivo) { alert('Subí un PDF primero'); return; }

  var solicitudes = [];
  document.querySelectorAll('.tipo-check:checked').forEach(function(cb) {
    var textarea = document.querySelector('.tipo-instrucciones[data-tipo="' + cb.value + '"]');
    solicitudes.push({tipo: cb.value, instrucciones: textarea.value});
  });

  if (solicitudes.length === 0) { alert('Marcá al menos un tipo de contenido'); return; }

  var formData = new FormData();
  formData.append('documento', archivo);
  formData.append('solicitudes', JSON.stringify(solicitudes));

  document.getElementById('resultados').innerHTML = '<p><b>Subiendo documento y creando trabajos...</b></p>';

  const resp = await fetch('/generar-documento', { method: 'POST', body: formData });
  const data = await resp.json();

  if (!data.trabajos) {
    document.getElementById('resultados').innerHTML = '<p>Error: ' + JSON.stringify(data) + '</p>';
    return;
  }

  document.getElementById('resultados').innerHTML = '';
  data.trabajos.forEach(function(t) {
    var div = document.createElement('div');
    div.className = 'resultado-card';
    div.id = 'job-' + t.job_id;
    div.innerHTML = '<h3>' + (ETIQUETAS[t.tipo] || t.tipo) + '</h3>' +
      '<p class="estado-tipo">Estado: pendiente</p><div class="contenido-tipo"></div>';
    document.getElementById('resultados').appendChild(div);
    consultarEstadoTipo(t.job_id);
  });
}

async function consultarEstadoTipo(jobId) {
  const resp = await fetch('/estado/' + jobId);
  const data = await resp.json();
  var card = document.getElementById('job-' + jobId);
  card.querySelector('.estado-tipo').textContent = 'Estado: ' + data.estado;

  if (data.estado === 'listo') {
    card.querySelector('.contenido-tipo').textContent = data.resultado;
  } else if (data.estado === 'error') {
    card.querySelector('.contenido-tipo').textContent = 'Error: ' + data.error;
  } else {
    setTimeout(function() { consultarEstadoTipo(jobId); }, 2000);
  }
}
</script>
</body>
</html>
"""


@app.route('/')
def index():
    return render_template_string(PAGINA)


@app.route('/generar', methods=['POST'])
def generar():
    data = request.get_json(silent=True) or {}
    prompt = data.get('prompt', '')
    max_tokens = data.get('max_tokens', 300)

    if not prompt:
        return jsonify({"error": "Falta el prompt"}), 400

    try:
        resp = requests.post(
            f"{SABIA_API_URL}/generar/",
            headers={"X-API-Key": SABIA_API_KEY},
            json={"prompt": prompt, "max_tokens": max_tokens},
            timeout=10
        )
        return jsonify(resp.json()), resp.status_code
    except requests.RequestException as e:
        return jsonify({"error": f"No se pudo conectar con el servidor (¿Tailscale conectado?): {e}"}), 502


@app.route('/generar-documento', methods=['POST'])
def generar_documento():
    archivo = request.files.get('documento')
    solicitudes = request.form.get('solicitudes')

    if not archivo:
        return jsonify({"error": "Falta el archivo 'documento'"}), 400
    if not solicitudes:
        return jsonify({"error": "Falta el campo 'solicitudes'"}), 400

    try:
        resp = requests.post(
            f"{SABIA_API_URL}/generar-desde-documento/",
            headers={"X-API-Key": SABIA_API_KEY},
            files={"documento": (archivo.filename, archivo.stream, archivo.mimetype)},
            data={"solicitudes": solicitudes},
            timeout=30
        )
        return jsonify(resp.json()), resp.status_code
    except requests.RequestException as e:
        return jsonify({"error": f"No se pudo conectar con el servidor (¿Tailscale conectado?): {e}"}), 502


@app.route('/estado/<job_id>')
def estado(job_id):
    try:
        resp = requests.get(
            f"{SABIA_API_URL}/estado/{job_id}/",
            headers={"X-API-Key": SABIA_API_KEY},
            timeout=10
        )
        return jsonify(resp.json()), resp.status_code
    except requests.RequestException as e:
        return jsonify({"error": f"No se pudo conectar con el servidor (¿Tailscale conectado?): {e}"}), 502


if __name__ == '__main__':
    app.run(debug=True, port=5000)