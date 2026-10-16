"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");
const path = require("node:path");

const scriptPath = path.resolve(__dirname, "../../wwwroot/js/datatables-general.js");

function inicializarTabla(actionsColumn) {
    const llamadas = [];
    const tabla = {
        dataset: {
            actionsColumn,
            searchPlaceholder: "Buscar registros",
            emptyMessage: "Sin registros",
            zeroMessage: "Sin coincidencias"
        }
    };

    global.document = {
        addEventListener(evento, controlador) {
            assert.equal(evento, "DOMContentLoaded");
            controlador();
        },
        querySelectorAll(selector) {
            assert.equal(selector, "[data-tabla-index]");
            return [tabla];
        }
    };
    global.DataTable = function (elemento, configuracion) {
        llamadas.push({ elemento, configuracion });
    };

    delete require.cache[scriptPath];
    require(scriptPath);

    delete global.document;
    delete global.DataTable;
    return llamadas[0];
}

test("DataTables mantiene la configuración estándar sin columna de acciones", () => {
    const llamada = inicializarTabla("false");

    assert.equal(llamada.configuracion.pageLength, 10);
    assert.deepEqual(llamada.configuracion.lengthMenu, [5, 10, 25, 50]);
    assert.equal(llamada.configuracion.columnDefs, undefined);
});

test("DataTables deshabilita ordenamiento y búsqueda solo cuando la columna existe", () => {
    const llamada = inicializarTabla("true");

    assert.deepEqual(llamada.configuracion.columnDefs, [{
        targets: -1,
        orderable: false,
        searchable: false
    }]);
});
