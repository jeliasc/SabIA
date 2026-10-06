document.addEventListener("DOMContentLoaded", function () {

    const tablas = document.querySelectorAll("[data-tabla-index]");

    tablas.forEach(function (tabla) {

        if (typeof DataTable === "undefined") {
            return;
        }

        const placeholder =
            tabla.dataset.searchPlaceholder
            ?? "Buscar...";

        const mensajeVacio =
            tabla.dataset.emptyMessage
            ?? "No existen registros";

        const mensajeSinResultados =
            tabla.dataset.zeroMessage
            ?? "No se encontraron registros";

        const columnaAcciones =
            tabla.dataset.actionsColumn !== "false";

        const configuracion = {

            pageLength: 10,

            lengthMenu: [
                5,
                10,
                25,
                50
            ],

            autoWidth: false,

            order: [
                [0, "asc"]
            ],

            language: {

                search: "Buscar:",

                searchPlaceholder:
                    placeholder,

                lengthMenu:
                    "Mostrar _MENU_ registros",

                info:
                    "Mostrando _START_ a _END_ de _TOTAL_ registros",

                infoEmpty:
                    "No existen registros",

                infoFiltered:
                    "(filtrado de _MAX_ registros)",

                zeroRecords:
                    mensajeSinResultados,

                emptyTable:
                    mensajeVacio,

                paginate: {
                    first: "<<",
                    previous: "<",
                    next: ">",
                    last: ">>"
                }
            }
        };

        if (columnaAcciones) {

            configuracion.columnDefs = [
                {
                    targets: -1,
                    orderable: false,
                    searchable: false,
                    width: "280px"
                }
            ];

        }

        new DataTable(
            tabla,
            configuracion
        );

    });

});