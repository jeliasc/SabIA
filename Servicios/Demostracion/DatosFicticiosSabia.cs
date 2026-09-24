using Proyecto_Final.ViewModels.Prototipo;

namespace Proyecto_Final.Servicios.Demostracion;

public static class DatosFicticiosSabia
{
    private static readonly Dictionary<string, ModuloPrototipo> Modulos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["roles"] = Modulo(
                "roles", "Roles", "Agrupa permisos para controlar el acceso al sistema.", "bi bi-person-badge",
                ["Rol", "Usuarios", "Descripción", "Estado"],
                [
                    Fila(1, ("Rol", "Superusuario", null), ("Usuarios", "1", null), ("Descripción", "Acceso total de administración.", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Rol", "Administrador", null), ("Usuarios", "2", null), ("Descripción", "Gestión administrativa y académica.", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Rol", "Docente", null), ("Usuarios", "24", null), ("Descripción", "Gestión de cursos, contenido y evaluación.", null), ("Estado", "Propuesto", "bg-info text-dark")),
                    Fila(4, ("Rol", "Alumno", null), ("Usuarios", "386", null), ("Descripción", "Consulta de contenido y entrega de actividades.", null), ("Estado", "Propuesto", "bg-info text-dark"))
                ],
                [Campo("Nombre", "Nombre del rol", requerido: true), Campo("Descripcion", "Descripción", "textarea", requerido: true)],
                [Indicador("Roles definidos", "4", "bi bi-person-badge", "text-bg-primary"), Indicador("Usuarios asignados", "413", "bi bi-people", "text-bg-success")]
            ),
            ["permisos"] = Modulo(
                "permisos", "Permisos", "Catálogo de acciones autorizables por módulo.", "bi bi-shield-lock",
                ["Permiso", "Módulo", "Descripción", "Roles", "Estado"],
                [
                    Fila(1, ("Permiso", "Usuarios.Ver", null), ("Módulo", "Usuarios", null), ("Descripción", "Consultar usuarios.", null), ("Roles", "2", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Permiso", "Usuarios.Editar", null), ("Módulo", "Usuarios", null), ("Descripción", "Modificar datos de usuario.", null), ("Roles", "2", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Permiso", "Cursos.Ver", null), ("Módulo", "Cursos", null), ("Descripción", "Consultar cursos según alcance.", null), ("Roles", "3", null), ("Estado", "Propuesto", "bg-info text-dark")),
                    Fila(4, ("Permiso", "Entregas.Calificar", null), ("Módulo", "Entregas", null), ("Descripción", "Calificar y retroalimentar entregas.", null), ("Roles", "2", null), ("Estado", "Propuesto", "bg-info text-dark"))
                ],
                [Campo("Nombre", "Permiso", requerido: true), Campo("Modulo", "Módulo", requerido: true), Campo("Descripcion", "Descripción", "textarea", requerido: true)],
                [Indicador("Permisos actuales", "14", "bi bi-key", "text-bg-primary"), Indicador("Permisos propuestos", "18", "bi bi-plus-circle", "text-bg-warning")]
            ),
            ["docentes"] = Modulo(
                "docentes", "Docentes", "Docentes que imparten cursos y administran contenido académico.", "bi bi-person-video3",
                ["Código", "Docente", "Correo", "Cursos", "Estado"],
                [
                    Fila(1, ("Código", "DOC-001", null), ("Docente", "María Fernanda López", null), ("Correo", "mlopez@demo.edu.gt", null), ("Cursos", "4", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Código", "DOC-002", null), ("Docente", "Carlos Méndez", null), ("Correo", "cmendez@demo.edu.gt", null), ("Cursos", "3", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Código", "DOC-003", null), ("Docente", "Ana Lucía García", null), ("Correo", "agarcia@demo.edu.gt", null), ("Cursos", "2", null), ("Estado", "Activo", "bg-success")),
                    Fila(4, ("Código", "DOC-004", null), ("Docente", "José Ramírez", null), ("Correo", "jramirez@demo.edu.gt", null), ("Cursos", "0", null), ("Estado", "Sin asignación", "bg-warning text-dark"))
                ],
                [Campo("Codigo", "Código", requerido: true), Campo("Nombre", "Nombre completo", requerido: true), Campo("Correo", "Correo", "email", requerido: true), Campo("Telefono", "Teléfono"), Campo("Estado", "Estado", "select", opciones: ["Activo", "Inactivo"])],
                [Indicador("Docentes activos", "24", "bi bi-person-check", "text-bg-success"), Indicador("Sin asignación", "3", "bi bi-person-exclamation", "text-bg-warning")]
            ),
            ["alumnos"] = Modulo(
                "alumnos", "Alumnos", "Registro de estudiantes e información académica asociada.", "bi bi-mortarboard",
                ["Carné", "Alumno", "Grado / Sección", "Encargado", "Estado"],
                [
                    Fila(1, ("Carné", "A-2026-001", null), ("Alumno", "Sofía Morales", null), ("Grado / Sección", "1.º Básico A", null), ("Encargado", "Laura Morales", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Carné", "A-2026-002", null), ("Alumno", "Daniel Pérez", null), ("Grado / Sección", "1.º Básico A", null), ("Encargado", "Mónica Pérez", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Carné", "A-2026-003", null), ("Alumno", "Valeria Gómez", null), ("Grado / Sección", "2.º Básico B", null), ("Encargado", "Ricardo Gómez", null), ("Estado", "Activo", "bg-success")),
                    Fila(4, ("Carné", "A-2026-004", null), ("Alumno", "Mateo Castillo", null), ("Grado / Sección", "3.º Básico A", null), ("Encargado", "Paola Castillo", null), ("Estado", "Inactivo", "bg-secondary"))
                ],
                [Campo("Carne", "Carné", requerido: true), Campo("Nombre", "Nombre completo", requerido: true), Campo("FechaNacimiento", "Fecha de nacimiento", "date"), Campo("GradoSeccion", "Grado / sección", "select", opciones: ["1.º Básico A", "1.º Básico B", "2.º Básico A", "2.º Básico B", "3.º Básico A"]), Campo("Encargado", "Encargado")],
                [Indicador("Alumnos activos", "386", "bi bi-mortarboard-fill", "text-bg-primary"), Indicador("Sin inscripción", "7", "bi bi-exclamation-triangle", "text-bg-warning")]
            ),
            ["encargados"] = Modulo(
                "encargados", "Encargados", "Personas responsables vinculadas a uno o más alumnos.", "bi bi-people",
                ["Encargado", "Teléfono", "Correo", "Alumnos vinculados", "Estado"],
                [
                    Fila(1, ("Encargado", "Laura Morales", null), ("Teléfono", "5555-0101", null), ("Correo", "laura.morales@correo.gt", null), ("Alumnos vinculados", "1", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Encargado", "Mónica Pérez", null), ("Teléfono", "5555-0102", null), ("Correo", "monica.perez@correo.gt", null), ("Alumnos vinculados", "2", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Encargado", "Ricardo Gómez", null), ("Teléfono", "5555-0103", null), ("Correo", "ricardo.gomez@correo.gt", null), ("Alumnos vinculados", "1", null), ("Estado", "Activo", "bg-success"))
                ],
                [Campo("Nombre", "Nombre completo", requerido: true), Campo("Telefono", "Teléfono", requerido: true), Campo("Correo", "Correo", "email"), Campo("Relacion", "Relación con el alumno", "select", opciones: ["Madre", "Padre", "Tutor legal", "Otro"])],
                [Indicador("Encargados", "341", "bi bi-people", "text-bg-primary"), Indicador("Con varios alumnos", "38", "bi bi-people-fill", "text-bg-info")]
            ),
            ["ciclos"] = Modulo(
                "ciclos", "Ciclos escolares", "Períodos anuales que organizan la operación académica.", "bi bi-calendar3",
                ["Ciclo", "Inicio", "Fin", "Inscripciones", "Estado"],
                [
                    Fila(1, ("Ciclo", "2026", null), ("Inicio", "12/01/2026", null), ("Fin", "30/10/2026", null), ("Inscripciones", "386", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Ciclo", "2025", null), ("Inicio", "13/01/2025", null), ("Fin", "31/10/2025", null), ("Inscripciones", "371", null), ("Estado", "Cerrado", "bg-secondary"))
                ],
                [Campo("Nombre", "Ciclo", requerido: true), Campo("FechaInicio", "Fecha de inicio", "date", requerido: true), Campo("FechaFin", "Fecha de finalización", "date", requerido: true)],
                [Indicador("Ciclo actual", "2026", "bi bi-calendar-check", "text-bg-success"), Indicador("Alumnos inscritos", "386", "bi bi-person-check", "text-bg-primary")]
            ),
            ["periodos"] = Modulo(
                "periodos", "Períodos académicos", "Divisiones del ciclo escolar utilizadas para planificación y seguimiento.", "bi bi-calendar-range",
                ["Período", "Ciclo", "Inicio", "Fin", "Estado"],
                [
                    Fila(1, ("Período", "Primer bimestre", null), ("Ciclo", "2026", null), ("Inicio", "12/01/2026", null), ("Fin", "13/03/2026", null), ("Estado", "Cerrado", "bg-secondary")),
                    Fila(2, ("Período", "Segundo bimestre", null), ("Ciclo", "2026", null), ("Inicio", "16/03/2026", null), ("Fin", "22/05/2026", null), ("Estado", "Cerrado", "bg-secondary")),
                    Fila(3, ("Período", "Tercer bimestre", null), ("Ciclo", "2026", null), ("Inicio", "25/05/2026", null), ("Fin", "31/07/2026", null), ("Estado", "Activo", "bg-success")),
                    Fila(4, ("Período", "Cuarto bimestre", null), ("Ciclo", "2026", null), ("Inicio", "03/08/2026", null), ("Fin", "30/10/2026", null), ("Estado", "Próximo", "bg-info text-dark"))
                ],
                [Campo("Nombre", "Nombre", requerido: true), Campo("Ciclo", "Ciclo", "select", opciones: ["2026"]), Campo("FechaInicio", "Fecha de inicio", "date", requerido: true), Campo("FechaFin", "Fecha de finalización", "date", requerido: true)],
                [Indicador("Períodos", "4", "bi bi-calendar-range", "text-bg-primary"), Indicador("Período actual", "Tercer bimestre", "bi bi-calendar-event", "text-bg-success")]
            ),
            ["grados"] = Modulo(
                "grados", "Grados", "Grados académicos disponibles dentro del ciclo escolar.", "bi bi-layers",
                ["Nivel", "Grado", "Secciones", "Alumnos", "Estado"],
                [
                    Fila(1, ("Nivel", "Básico", null), ("Grado", "Primero", null), ("Secciones", "2", null), ("Alumnos", "132", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Nivel", "Básico", null), ("Grado", "Segundo", null), ("Secciones", "2", null), ("Alumnos", "128", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Nivel", "Básico", null), ("Grado", "Tercero", null), ("Secciones", "2", null), ("Alumnos", "126", null), ("Estado", "Activo", "bg-success"))
                ],
                [Campo("Nivel", "Nivel", "select", requerido: true, opciones: ["Básico", "Diversificado"]), Campo("Nombre", "Grado", requerido: true), Campo("Orden", "Orden", "number")],
                [Indicador("Grados activos", "3", "bi bi-layers", "text-bg-primary"), Indicador("Secciones", "6", "bi bi-diagram-3", "text-bg-info")]
            ),
            ["secciones"] = Modulo(
                "secciones", "Secciones", "Organización de alumnos por grado y sección.", "bi bi-diagram-3",
                ["Grado", "Sección", "Docente guía", "Alumnos", "Estado"],
                [
                    Fila(1, ("Grado", "Primero Básico", null), ("Sección", "A", null), ("Docente guía", "María F. López", null), ("Alumnos", "66", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Grado", "Primero Básico", null), ("Sección", "B", null), ("Docente guía", "Carlos Méndez", null), ("Alumnos", "66", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Grado", "Segundo Básico", null), ("Sección", "A", null), ("Docente guía", "Ana L. García", null), ("Alumnos", "64", null), ("Estado", "Activo", "bg-success")),
                    Fila(4, ("Grado", "Segundo Básico", null), ("Sección", "B", null), ("Docente guía", "José Ramírez", null), ("Alumnos", "64", null), ("Estado", "Activo", "bg-success"))
                ],
                [Campo("Grado", "Grado", "select", requerido: true, opciones: ["Primero Básico", "Segundo Básico", "Tercero Básico"]), Campo("Seccion", "Sección", requerido: true), Campo("DocenteGuia", "Docente guía", "select", opciones: ["María F. López", "Carlos Méndez", "Ana L. García", "José Ramírez"])],
                [Indicador("Secciones", "6", "bi bi-diagram-3", "text-bg-primary"), Indicador("Promedio por sección", "64", "bi bi-people", "text-bg-info")]
            ),
            ["cursos"] = Modulo(
                "cursos", "Cursos", "Catálogo de cursos utilizados en las asignaciones académicas.", "bi bi-book",
                ["Código", "Curso", "Grado", "Asignaciones", "Estado"],
                [
                    Fila(1, ("Código", "MAT-1", null), ("Curso", "Matemática", null), ("Grado", "Primero Básico", null), ("Asignaciones", "2", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Código", "COM-1", null), ("Curso", "Comunicación y Lenguaje", null), ("Grado", "Primero Básico", null), ("Asignaciones", "2", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Código", "CCNN-2", null), ("Curso", "Ciencias Naturales", null), ("Grado", "Segundo Básico", null), ("Asignaciones", "2", null), ("Estado", "Activo", "bg-success")),
                    Fila(4, ("Código", "SOC-3", null), ("Curso", "Ciencias Sociales", null), ("Grado", "Tercero Básico", null), ("Asignaciones", "2", null), ("Estado", "Activo", "bg-success"))
                ],
                [Campo("Codigo", "Código", requerido: true), Campo("Nombre", "Nombre del curso", requerido: true), Campo("Grado", "Grado", "select", requerido: true, opciones: ["Primero Básico", "Segundo Básico", "Tercero Básico"]), Campo("Descripcion", "Descripción", "textarea")],
                [Indicador("Cursos activos", "18", "bi bi-book", "text-bg-primary"), Indicador("Con docente asignado", "17", "bi bi-person-check", "text-bg-success")]
            ),
            ["asignaciones"] = Modulo(
                "asignaciones", "Asignaciones docentes", "Relaciona docente, curso, grado, sección y período académico.", "bi bi-person-workspace",
                ["Curso", "Docente", "Grado / Sección", "Período", "Estado"],
                [
                    Fila(1, ("Curso", "Matemática", null), ("Docente", "María F. López", null), ("Grado / Sección", "1.º Básico A", null), ("Período", "2026", null), ("Estado", "Activo", "bg-success")),
                    Fila(2, ("Curso", "Matemática", null), ("Docente", "María F. López", null), ("Grado / Sección", "1.º Básico B", null), ("Período", "2026", null), ("Estado", "Activo", "bg-success")),
                    Fila(3, ("Curso", "Ciencias Naturales", null), ("Docente", "Carlos Méndez", null), ("Grado / Sección", "2.º Básico A", null), ("Período", "2026", null), ("Estado", "Activo", "bg-success"))
                ],
                [Campo("Curso", "Curso", "select", requerido: true, opciones: ["Matemática", "Comunicación y Lenguaje", "Ciencias Naturales", "Ciencias Sociales"]), Campo("Docente", "Docente", "select", requerido: true, opciones: ["María F. López", "Carlos Méndez", "Ana L. García"]), Campo("GradoSeccion", "Grado / sección", "select", requerido: true, opciones: ["1.º Básico A", "1.º Básico B", "2.º Básico A", "2.º Básico B"]), Campo("Ciclo", "Ciclo", "select", requerido: true, opciones: ["2026"])],
                [Indicador("Asignaciones activas", "36", "bi bi-person-workspace", "text-bg-primary"), Indicador("Sin docente", "1", "bi bi-exclamation-triangle", "text-bg-warning")]
            ),
            ["inscripciones"] = Modulo(
                "inscripciones", "Inscripciones", "Vincula cada alumno con su grado y sección durante el ciclo escolar.", "bi bi-person-vcard",
                ["Alumno", "Grado / Sección", "Ciclo", "Fecha", "Estado"],
                [
                    Fila(1, ("Alumno", "Sofía Morales", null), ("Grado / Sección", "1.º Básico A", null), ("Ciclo", "2026", null), ("Fecha", "08/01/2026", null), ("Estado", "Activa", "bg-success")),
                    Fila(2, ("Alumno", "Daniel Pérez", null), ("Grado / Sección", "1.º Básico A", null), ("Ciclo", "2026", null), ("Fecha", "08/01/2026", null), ("Estado", "Activa", "bg-success")),
                    Fila(3, ("Alumno", "Valeria Gómez", null), ("Grado / Sección", "2.º Básico B", null), ("Ciclo", "2026", null), ("Fecha", "09/01/2026", null), ("Estado", "Activa", "bg-success"))
                ],
                [Campo("Alumno", "Alumno", "select", requerido: true, opciones: ["Sofía Morales", "Daniel Pérez", "Valeria Gómez"]), Campo("GradoSeccion", "Grado / sección", "select", requerido: true, opciones: ["1.º Básico A", "2.º Básico B", "3.º Básico A"]), Campo("Ciclo", "Ciclo", "select", requerido: true, opciones: ["2026"]), Campo("Fecha", "Fecha de inscripción", "date", requerido: true)],
                [Indicador("Inscripciones activas", "386", "bi bi-person-vcard", "text-bg-success"), Indicador("Pendientes", "7", "bi bi-hourglass-split", "text-bg-warning")]
            ),
            ["unidades"] = Modulo(
                "unidades", "Unidades", "Organiza el contenido que el docente libera progresivamente para sus alumnos.", "bi bi-journal-bookmark",
                ["Curso", "Unidad", "Período", "Materiales", "Estado"],
                [
                    Fila(1, ("Curso", "Matemática", null), ("Unidad", "Unidad 3: Álgebra básica", null), ("Período", "Tercer bimestre", null), ("Materiales", "5", null), ("Estado", "Liberada", "bg-success")),
                    Fila(2, ("Curso", "Comunicación y Lenguaje", null), ("Unidad", "Unidad 4: Texto argumentativo", null), ("Período", "Tercer bimestre", null), ("Materiales", "4", null), ("Estado", "Liberada", "bg-success")),
                    Fila(3, ("Curso", "Ciencias Naturales", null), ("Unidad", "Unidad 3: Ecosistemas", null), ("Período", "Tercer bimestre", null), ("Materiales", "6", null), ("Estado", "Borrador", "bg-warning text-dark"))
                ],
                [Campo("Curso", "Curso", "select", requerido: true, opciones: ["Matemática", "Comunicación y Lenguaje", "Ciencias Naturales"]), Campo("Nombre", "Nombre de unidad", requerido: true), Campo("Periodo", "Período", "select", requerido: true, opciones: ["Tercer bimestre", "Cuarto bimestre"]), Campo("Descripcion", "Objetivo / descripción", "textarea"), Campo("Estado", "Estado", "select", opciones: ["Borrador", "Liberada"])],
                [Indicador("Unidades liberadas", "41", "bi bi-unlock", "text-bg-success"), Indicador("En borrador", "9", "bi bi-pencil-square", "text-bg-warning")]
            ),
            ["materiales"] = Modulo(
                "materiales", "Materiales", "Recursos y archivos publicados dentro de las unidades.", "bi bi-folder2-open",
                ["Título", "Tipo", "Curso", "Unidad", "Estado"],
                [
                    Fila(1, ("Título", "Guía de ecuaciones lineales", null), ("Tipo", "PDF", null), ("Curso", "Matemática", null), ("Unidad", "Álgebra básica", null), ("Estado", "Publicado", "bg-success")),
                    Fila(2, ("Título", "Video: resolución de ecuaciones", null), ("Tipo", "Enlace", null), ("Curso", "Matemática", null), ("Unidad", "Álgebra básica", null), ("Estado", "Publicado", "bg-success")),
                    Fila(3, ("Título", "Lectura: ecosistemas de Guatemala", null), ("Tipo", "PDF", null), ("Curso", "Ciencias Naturales", null), ("Unidad", "Ecosistemas", null), ("Estado", "Borrador", "bg-warning text-dark"))
                ],
                [Campo("Titulo", "Título", requerido: true), Campo("Tipo", "Tipo", "select", requerido: true, opciones: ["PDF", "Documento", "Enlace", "Video", "Imagen"]), Campo("Curso", "Curso", "select", requerido: true, opciones: ["Matemática", "Comunicación y Lenguaje", "Ciencias Naturales"]), Campo("Unidad", "Unidad", requerido: true), Campo("Descripcion", "Descripción", "textarea")],
                [Indicador("Materiales publicados", "126", "bi bi-folder-check", "text-bg-success"), Indicador("Pendientes", "14", "bi bi-file-earmark", "text-bg-warning")]
            ),
            ["planificaciones"] = Modulo(
                "planificaciones", "Planificación docente", "Prototipo de planificación por curso y período.", "bi bi-clipboard2-check",
                ["Docente", "Curso", "Período", "Última actualización", "Estado"],
                [
                    Fila(1, ("Docente", "María F. López", null), ("Curso", "Matemática", null), ("Período", "Tercer bimestre", null), ("Última actualización", "18/09/2026", null), ("Estado", "Aprobada", "bg-success")),
                    Fila(2, ("Docente", "Carlos Méndez", null), ("Curso", "Ciencias Naturales", null), ("Período", "Tercer bimestre", null), ("Última actualización", "17/09/2026", null), ("Estado", "En revisión", "bg-warning text-dark"))
                ],
                [Campo("Curso", "Curso", "select", requerido: true, opciones: ["Matemática", "Ciencias Naturales"]), Campo("Periodo", "Período", "select", requerido: true, opciones: ["Tercer bimestre", "Cuarto bimestre"]), Campo("Objetivos", "Objetivos", "textarea", requerido: true), Campo("Actividades", "Actividades", "textarea")],
                [Indicador("Planificaciones", "24", "bi bi-clipboard2-check", "text-bg-primary"), Indicador("En revisión", "5", "bi bi-hourglass", "text-bg-warning")],
                "La documentación identifica planificación como módulo a afinar en el modelo de datos. Esta pantalla es únicamente de prototipo."
            ),
            ["documentos"] = Modulo(
                "documentos", "Documentos de coordinación", "Espacio prototipo para documentos internos entre coordinación y docentes.", "bi bi-file-earmark-text",
                ["Documento", "Tipo", "Responsable", "Fecha", "Estado"],
                [
                    Fila(1, ("Documento", "Lineamientos de evaluación - septiembre", null), ("Tipo", "Circular", null), ("Responsable", "Coordinación", null), ("Fecha", "02/09/2026", null), ("Estado", "Publicado", "bg-success")),
                    Fila(2, ("Documento", "Formato de planificación bimestral", null), ("Tipo", "Plantilla", null), ("Responsable", "Coordinación", null), ("Fecha", "28/08/2026", null), ("Estado", "Publicado", "bg-success"))
                ],
                [Campo("Titulo", "Título", requerido: true), Campo("Tipo", "Tipo", "select", opciones: ["Circular", "Plantilla", "Lineamiento", "Otro"]), Campo("Descripcion", "Descripción", "textarea"), Campo("Fecha", "Fecha", "date")],
                [Indicador("Documentos activos", "12", "bi bi-file-earmark-text", "text-bg-primary"), Indicador("Nuevos este mes", "3", "bi bi-file-earmark-plus", "text-bg-success")],
                "Los documentos de coordinación aparecen como módulo pendiente de afinar. No se han creado entidades de base de datos para esta vista."
            ),
            ["tareas"] = Modulo(
                "tareas", "Tareas", "Actividades publicadas por los docentes y asignadas a sus alumnos.", "bi bi-list-check",
                ["Tarea", "Curso", "Fecha límite", "Entregas", "Estado"],
                [
                    Fila(1, ("Tarea", "Ejercicios de ecuaciones", null), ("Curso", "Matemática", null), ("Fecha límite", "26/09/2026", null), ("Entregas", "42 / 66", null), ("Estado", "Activa", "bg-success")),
                    Fila(2, ("Tarea", "Ensayo argumentativo", null), ("Curso", "Comunicación y Lenguaje", null), ("Fecha límite", "28/09/2026", null), ("Entregas", "37 / 66", null), ("Estado", "Activa", "bg-success")),
                    Fila(3, ("Tarea", "Mapa conceptual de ecosistemas", null), ("Curso", "Ciencias Naturales", null), ("Fecha límite", "22/09/2026", null), ("Entregas", "61 / 64", null), ("Estado", "Cerrada", "bg-secondary"))
                ],
                [Campo("Titulo", "Título", requerido: true), Campo("Curso", "Curso", "select", requerido: true, opciones: ["Matemática", "Comunicación y Lenguaje", "Ciencias Naturales"]), Campo("Descripcion", "Instrucciones", "textarea", requerido: true), Campo("FechaLimite", "Fecha límite", "date", requerido: true), Campo("Estado", "Estado", "select", opciones: ["Borrador", "Activa", "Cerrada"])],
                [Indicador("Tareas activas", "37", "bi bi-list-check", "text-bg-primary"), Indicador("Por vencer", "8", "bi bi-clock", "text-bg-warning")]
            ),
            ["entregas"] = Modulo(
                "entregas", "Entregas y calificación", "Bandeja docente para revisar, calificar y retroalimentar actividades.", "bi bi-inbox",
                ["Alumno", "Tarea", "Curso", "Entregada", "Estado"],
                [
                    Fila(1, ("Alumno", "Sofía Morales", null), ("Tarea", "Ejercicios de ecuaciones", null), ("Curso", "Matemática", null), ("Entregada", "23/09/2026 18:42", null), ("Estado", "Pendiente de revisión", "bg-warning text-dark")),
                    Fila(2, ("Alumno", "Daniel Pérez", null), ("Tarea", "Ejercicios de ecuaciones", null), ("Curso", "Matemática", null), ("Entregada", "22/09/2026 20:15", null), ("Estado", "Calificada", "bg-success")),
                    Fila(3, ("Alumno", "Valeria Gómez", null), ("Tarea", "Mapa conceptual de ecosistemas", null), ("Curso", "Ciencias Naturales", null), ("Entregada", "21/09/2026 16:02", null), ("Estado", "Requiere corrección", "bg-danger"))
                ],
                [Campo("Calificacion", "Calificación de apoyo", "number"), Campo("Retroalimentacion", "Retroalimentación", "textarea", requerido: true), Campo("Estado", "Estado", "select", opciones: ["Pendiente de revisión", "Calificada", "Requiere corrección"])],
                [Indicador("Pendientes de revisión", "62", "bi bi-inbox", "text-bg-warning"), Indicador("Calificadas esta semana", "94", "bi bi-check2-circle", "text-bg-success")],
                "Las calificaciones mostradas en SabIA son de apoyo y no sustituyen el sistema oficial de calificaciones."
            ) with { PermitirCrear = false, EtiquetaEditar = "Calificar / revisar" },
            ["cuestionarios"] = Modulo(
                "cuestionarios", "Cuestionarios", "Prototipo de cuestionarios asociados a unidades y contenido generado/revisado por docentes.", "bi bi-ui-checks",
                ["Cuestionario", "Curso", "Preguntas", "Intentos", "Estado"],
                [
                    Fila(1, ("Cuestionario", "Repaso de álgebra", null), ("Curso", "Matemática", null), ("Preguntas", "10", null), ("Intentos", "58", null), ("Estado", "Publicado", "bg-success")),
                    Fila(2, ("Cuestionario", "Comprensión lectora", null), ("Curso", "Comunicación y Lenguaje", null), ("Preguntas", "8", null), ("Intentos", "51", null), ("Estado", "Borrador", "bg-warning text-dark"))
                ],
                [Campo("Titulo", "Título", requerido: true), Campo("Curso", "Curso", "select", requerido: true, opciones: ["Matemática", "Comunicación y Lenguaje"]), Campo("Preguntas", "Cantidad de preguntas", "number"), Campo("Estado", "Estado", "select", opciones: ["Borrador", "Publicado"])],
                [Indicador("Publicados", "11", "bi bi-ui-checks", "text-bg-success"), Indicador("En borrador", "4", "bi bi-pencil", "text-bg-warning")],
                "La documentación incluye cuestionarios como función prevista; el detalle del modelo todavía debe afinarse antes de crear tablas definitivas."
            ),
            ["contenidoia"] = Modulo(
                "contenidoia", "Contenido con IA", "Prototipo para generar borradores de guías, resúmenes y cuestionarios que el docente revisa antes de publicar.", "bi bi-stars",
                ["Tipo", "Curso", "Solicitud", "Generado", "Estado"],
                [
                    Fila(1, ("Tipo", "Guía", null), ("Curso", "Matemática", null), ("Solicitud", "Ejercicios de ecuaciones lineales", null), ("Generado", "23/09/2026", null), ("Estado", "Pendiente de revisión", "bg-warning text-dark")),
                    Fila(2, ("Tipo", "Resumen", null), ("Curso", "Ciencias Naturales", null), ("Solicitud", "Ecosistemas de Guatemala", null), ("Generado", "22/09/2026", null), ("Estado", "Aprobado", "bg-success")),
                    Fila(3, ("Tipo", "Cuestionario", null), ("Curso", "Comunicación y Lenguaje", null), ("Solicitud", "Texto argumentativo", null), ("Generado", "21/09/2026", null), ("Estado", "Borrador", "bg-secondary"))
                ],
                [Campo("Tipo", "Tipo de contenido", "select", requerido: true, opciones: ["Guía", "Resumen", "Cuestionario"]), Campo("Curso", "Curso", "select", requerido: true, opciones: ["Matemática", "Comunicación y Lenguaje", "Ciencias Naturales"]), Campo("Solicitud", "Instrucción para generar", "textarea", requerido: true)],
                [Indicador("Generaciones este mes", "38", "bi bi-stars", "text-bg-primary"), Indicador("Pendientes de revisión", "7", "bi bi-eye", "text-bg-warning")],
                "El contenido generado por IA debe ser revisado por el docente antes de su publicación. El modelo de persistencia definitivo queda pendiente."
            ) with { EtiquetaCrear = "Generar contenido", EtiquetaEditar = "Revisar" },
            ["notificaciones"] = Modulo(
                "notificaciones", "Alertas y notificaciones", "Prototipo de avisos sobre tareas, entregas, actividad y eventos relevantes.", "bi bi-bell",
                ["Tipo", "Destinatario", "Mensaje", "Fecha", "Estado"],
                [
                    Fila(1, ("Tipo", "Entrega", null), ("Destinatario", "Docente", null), ("Mensaje", "Nueva entrega en Ejercicios de ecuaciones.", null), ("Fecha", "23/09/2026 18:42", null), ("Estado", "No leída", "bg-warning text-dark")),
                    Fila(2, ("Tipo", "Tarea", null), ("Destinatario", "Alumno", null), ("Mensaje", "La tarea Ensayo argumentativo vence en 5 días.", null), ("Fecha", "23/09/2026 08:00", null), ("Estado", "Leída", "bg-success")),
                    Fila(3, ("Tipo", "Seguimiento", null), ("Destinatario", "Coordinación", null), ("Mensaje", "Curso con baja frecuencia de acceso durante la semana.", null), ("Fecha", "22/09/2026 17:30", null), ("Estado", "Pendiente", "bg-danger"))
                ],
                [Campo("Tipo", "Tipo", "select", opciones: ["Tarea", "Entrega", "Seguimiento", "Sistema"]), Campo("Destinatario", "Destinatario", requerido: true), Campo("Mensaje", "Mensaje", "textarea", requerido: true)],
                [Indicador("No leídas", "8", "bi bi-bell-fill", "text-bg-warning"), Indicador("Alertas de seguimiento", "5", "bi bi-exclamation-diamond", "text-bg-danger")],
                "Alertas y notificaciones están contempladas en los requerimientos, pero sus reglas definitivas todavía deben validarse."
            ) with { PermitirCrear = false, PermitirEditar = false },
        };

    public static DashboardPrototipo ObtenerDashboard()
    {
        return new DashboardPrototipo
        {
            Indicadores =
            [
                Indicador("Alumnos activos", "386", "bi bi-mortarboard-fill", "text-bg-primary"),
                Indicador("Docentes activos", "24", "bi bi-person-video3", "text-bg-success"),
                Indicador("Cursos activos", "18", "bi bi-book", "text-bg-info"),
                Indicador("Entregas pendientes", "62", "bi bi-inbox", "text-bg-warning")
            ],
            ActividadReciente =
            [
                new() { Icono = "bi bi-file-earmark-arrow-up", Titulo = "Material publicado", Detalle = "Matemática · Unidad 3", Hace = "Hace 18 min" },
                new() { Icono = "bi bi-inbox", Titulo = "Nueva entrega", Detalle = "Sofía Morales · Ejercicios de ecuaciones", Hace = "Hace 32 min" },
                new() { Icono = "bi bi-person-plus", Titulo = "Alumno inscrito", Detalle = "Nuevo registro en 1.º Básico A", Hace = "Hace 2 h" },
                new() { Icono = "bi bi-list-check", Titulo = "Tarea publicada", Detalle = "Comunicación y Lenguaje · Ensayo argumentativo", Hace = "Ayer" }
            ],
            Alertas =
            [
                new() { Tipo = "Seguimiento", Mensaje = "3 cursos presentan baja frecuencia de acceso estudiantil esta semana.", Clase = "warning" },
                new() { Tipo = "Entregas", Mensaje = "62 entregas están pendientes de revisión docente.", Clase = "danger" },
                new() { Tipo = "Asignaciones", Mensaje = "1 curso activo todavía no tiene docente asignado.", Clase = "info" }
            ],
            Cursos =
            [
                new() { Curso = "Matemática", GradoSeccion = "1.º Básico A", Docente = "María F. López", Progreso = 76, EntregasPendientes = 18 },
                new() { Curso = "Comunicación y Lenguaje", GradoSeccion = "1.º Básico A", Docente = "Ana L. García", Progreso = 69, EntregasPendientes = 14 },
                new() { Curso = "Ciencias Naturales", GradoSeccion = "2.º Básico B", Docente = "Carlos Méndez", Progreso = 72, EntregasPendientes = 11 },
                new() { Curso = "Ciencias Sociales", GradoSeccion = "3.º Básico A", Docente = "José Ramírez", Progreso = 81, EntregasPendientes = 7 }
            ]
        };
    }

    public static ExperienciaDocentePrototipo ObtenerExperienciaDocente()
    {
        return new ExperienciaDocentePrototipo
        {
            Docente = "María Fernanda López",
            Indicadores =
            [
                Indicador("Mis cursos", "4", "bi bi-book", "text-bg-primary"),
                Indicador("Alumnos", "126", "bi bi-people", "text-bg-info"),
                Indicador("Tareas activas", "7", "bi bi-list-check", "text-bg-success"),
                Indicador("Por revisar", "18", "bi bi-inbox", "text-bg-warning")
            ],
            Cursos =
            [
                new() { Curso = "Matemática", GradoSeccion = "1.º Básico A", Docente = "María F. López", Progreso = 76, EntregasPendientes = 10 },
                new() { Curso = "Matemática", GradoSeccion = "1.º Básico B", Docente = "María F. López", Progreso = 73, EntregasPendientes = 8 }
            ],
            Entregas =
            [
                new() { Alumno = "Sofía Morales", Tarea = "Ejercicios de ecuaciones", Curso = "Matemática", Fecha = "23/09/2026 18:42" },
                new() { Alumno = "Daniel Pérez", Tarea = "Ejercicios de ecuaciones", Curso = "Matemática", Fecha = "23/09/2026 17:10" },
                new() { Alumno = "Gabriela Ruiz", Tarea = "Problemas de aplicación", Curso = "Matemática", Fecha = "22/09/2026 20:04" }
            ]
        };
    }

    public static ExperienciaAlumnoPrototipo ObtenerExperienciaAlumno()
    {
        return new ExperienciaAlumnoPrototipo
        {
            Alumno = "Sofía Morales",
            GradoSeccion = "1.º Básico A",
            ProgresoGeneral = 74,
            Indicadores =
            [
                Indicador("Cursos", "9", "bi bi-book", "text-bg-primary"),
                Indicador("Tareas pendientes", "3", "bi bi-list-check", "text-bg-warning"),
                Indicador("Entregadas", "21", "bi bi-check2-circle", "text-bg-success"),
                Indicador("Recursos nuevos", "5", "bi bi-folder2-open", "text-bg-info")
            ],
            Unidades =
            [
                new() { Curso = "Matemática", Unidad = "Álgebra básica", Estado = "Disponible", Progreso = 80 },
                new() { Curso = "Comunicación y Lenguaje", Unidad = "Texto argumentativo", Estado = "Disponible", Progreso = 65 },
                new() { Curso = "Ciencias Naturales", Unidad = "Ecosistemas", Estado = "Próximamente", Progreso = 35 }
            ],
            Tareas =
            [
                new() { Tarea = "Ejercicios de ecuaciones", Curso = "Matemática", FechaLimite = "26/09/2026", Estado = "Pendiente" },
                new() { Tarea = "Ensayo argumentativo", Curso = "Comunicación y Lenguaje", FechaLimite = "28/09/2026", Estado = "Pendiente" },
                new() { Tarea = "Mapa conceptual", Curso = "Ciencias Naturales", FechaLimite = "22/09/2026", Estado = "Entregada" }
            ]
        };
    }

    public static ModuloPrototipo ObtenerModulo(string clave)
    {
        if (!Modulos.TryGetValue(clave, out var modulo))
        {
            throw new InvalidOperationException($"No existe configuración de prototipo para el módulo '{clave}'.");
        }

        return modulo;
    }

    public static DetalleModuloPrototipo ObtenerDetalle(string clave, int id)
    {
        var modulo = ObtenerModulo(clave);
        var fila = modulo.Filas.FirstOrDefault(x => x.Id == id) ?? modulo.Filas.First();

        return new DetalleModuloPrototipo
        {
            Modulo = modulo,
            Fila = fila
        };
    }

    public static FormularioModuloPrototipo ObtenerFormulario(string clave, bool esEdicion, int? id = null)
    {
        return new FormularioModuloPrototipo
        {
            Modulo = ObtenerModulo(clave),
            EsEdicion = esEdicion,
            Id = id
        };
    }

    private static ModuloPrototipo Modulo(
        string clave,
        string titulo,
        string descripcion,
        string icono,
        List<string> columnas,
        List<FilaPrototipo> filas,
        List<CampoFormularioPrototipo> campos,
        List<IndicadorPrototipo> indicadores,
        string? notaTecnica = null)
    {
        return new ModuloPrototipo
        {
            Clave = clave,
            Titulo = titulo,
            Descripcion = descripcion,
            Icono = icono,
            Columnas = columnas,
            Filas = filas,
            CamposFormulario = campos,
            Indicadores = indicadores,
            NotaTecnica = notaTecnica
        };
    }

    private static FilaPrototipo Fila(
        int id,
        params (string Columna, string Texto, string? Badge)[] celdas)
    {
        var fila = new FilaPrototipo { Id = id };

        foreach (var celda in celdas)
        {
            fila.Valores[celda.Columna] = new CeldaPrototipo
            {
                Texto = celda.Texto,
                ClaseBadge = celda.Badge
            };
        }

        return fila;
    }

    private static CampoFormularioPrototipo Campo(
        string nombre,
        string etiqueta,
        string tipo = "text",
        string? valor = null,
        string? placeholder = null,
        bool requerido = false,
        List<string>? opciones = null)
    {
        return new CampoFormularioPrototipo
        {
            Nombre = nombre,
            Etiqueta = etiqueta,
            Tipo = tipo,
            ValorEjemplo = valor,
            Placeholder = placeholder,
            Requerido = requerido,
            Opciones = opciones ?? []
        };
    }

    private static IndicadorPrototipo Indicador(string titulo, string valor, string icono, string clase)
    {
        return new IndicadorPrototipo
        {
            Titulo = titulo,
            Valor = valor,
            Icono = icono,
            Clase = clase
        };
    }
}
