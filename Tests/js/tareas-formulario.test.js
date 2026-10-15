"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");
const {
    obtenerEstadoFormulario
} = require("../../wwwroot/js/tareas-formulario.js");

test("Cuestionario oculta el adjunto y cambia la acción", () => {
    const estado = obtenerEstadoFormulario("2", "2");

    assert.equal(estado.esCuestionario, true);
    assert.equal(estado.mostrarAdjunto, false);
    assert.equal(estado.textoAccion, "Configurar cuestionario");
});

test("Entrega de archivo restaura el adjunto y el borrador", () => {
    const estado = obtenerEstadoFormulario("1", "2");

    assert.equal(estado.esCuestionario, false);
    assert.equal(estado.mostrarAdjunto, true);
    assert.equal(estado.textoAccion, "Guardar borrador");
});
