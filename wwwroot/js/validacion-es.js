/* global jQuery */
(function ($) {
    "use strict";

    if (!$.validator) {
        return;
    }

    $.extend($.validator.messages, {
        required: "Este campo es obligatorio.",
        remote: "Corrija este campo.",
        email: "Ingrese una dirección de correo electrónico válida.",
        url: "Ingrese una dirección URL válida.",
        date: "Ingrese una fecha válida.",
        dateISO: "Ingrese una fecha válida.",
        number: "Ingrese un número válido.",
        digits: "Ingrese solo dígitos.",
        equalTo: "Los valores no coinciden.",
        maxlength: $.validator.format("No ingrese más de {0} caracteres."),
        minlength: $.validator.format("Ingrese al menos {0} caracteres."),
        rangelength: $.validator.format("Ingrese un valor entre {0} y {1} caracteres."),
        range: $.validator.format("Ingrese un valor entre {0} y {1}."),
        max: $.validator.format("Ingrese un valor menor o igual que {0}."),
        min: $.validator.format("Ingrese un valor mayor o igual que {0}.")
    });
}(jQuery));
