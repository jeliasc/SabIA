import http from 'node:http';

const port = Number(process.env.E2E_IA_PORT ?? 5107);
let consultas = 0;
const cuestionario = JSON.stringify({
  preguntas: [
    {
      tipo: 'seleccion_unica',
      enunciado: '¿Cuál es la capital de Guatemala?',
      opciones: ['Ciudad de Guatemala', 'Quetzaltenango'],
      correctas: ['Ciudad de Guatemala'],
      explicacion: 'La capital del país es Ciudad de Guatemala.'
    },
    {
      tipo: 'verdadero_falso',
      enunciado: 'Guatemala pertenece a Centroamérica.',
      opciones: ['Verdadero', 'Falso'],
      correctas: ['Verdadero']
    }
  ]
});

const responder = (response, status, body) => {
  response.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8' });
  response.end(JSON.stringify(body));
};

const server = http.createServer((request, response) => {
  if (request.method === 'POST' && request.url === '/api/generar-desde-documento/') {
    consultas = 0;
    request.resume();
    request.on('end', () => responder(response, 200, {
      trabajos: [{ job_id: 'e2e-cuestionario-simulado', tipo: 'cuestionario' }]
    }));
    return;
  }

  if (request.method === 'GET' && request.url === '/api/estado/e2e-cuestionario-simulado/') {
    consultas++;
    responder(response, 200, consultas === 1
      ? { estado: 'pendiente', resultado: null }
      : { estado: 'completado', resultado: cuestionario });
    return;
  }

  responder(response, 404, { mensaje: 'Ruta simulada no encontrada.' });
});

server.listen(port, '127.0.0.1', () =>
  process.stdout.write(`Proveedor IA simulado disponible en http://127.0.0.1:${port}/api\n`));

const cerrar = () => server.close(() => process.exit(0));
process.on('SIGINT', cerrar);
process.on('SIGTERM', cerrar);
