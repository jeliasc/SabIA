namespace Proyecto_Final.Seguridad;

public static class Permisos
{
    public static class Dashboard
    {
        public const string CoordinacionVer = "Dashboard.Coordinacion.Ver";
    }

    public static class Usuarios
    {
        public const string Ver = "Usuarios.Ver";
        public const string Crear = "Usuarios.Crear";
        public const string Editar = "Usuarios.Editar";
        public const string CambiarEstado = "Usuarios.CambiarEstado";
        public const string RestablecerContrasena = "Usuarios.RestablecerContrasena";
    }

    public static class Roles
    {
        public const string Ver = "Roles.Ver";
        public const string Crear = "Roles.Crear";
        public const string Editar = "Roles.Editar";
        public const string CambiarEstado = "Roles.CambiarEstado";
        public const string AsignarPermisos = "Roles.AsignarPermisos";
    }

    public static class PermisosSistema
    {
        public const string Ver = "Permisos.Ver";
        public const string Crear = "Permisos.Crear";
        public const string Editar = "Permisos.Editar";
        public const string CambiarEstado = "Permisos.CambiarEstado";
    }

    public static class Docentes
    {
        public const string Ver = "Docentes.Ver";
        public const string Crear = "Docentes.Crear";
        public const string Editar = "Docentes.Editar";
        public const string CambiarEstado = "Docentes.CambiarEstado";
    }

    public static class Alumnos
    {
        public const string Ver = "Alumnos.Ver";
        public const string Crear = "Alumnos.Crear";
        public const string Editar = "Alumnos.Editar";
        public const string CambiarEstado = "Alumnos.CambiarEstado";
    }

    public static class Encargados
    {
        public const string Ver = "Encargados.Ver";
        public const string Crear = "Encargados.Crear";
        public const string Editar = "Encargados.Editar";
    }

    public static class Ciclos
    {
        public const string Ver = "Ciclos.Ver";
        public const string Crear = "Ciclos.Crear";
        public const string Editar = "Ciclos.Editar";
        public const string Activar = "Ciclos.Activar";
    }

    public static class Periodos
    {
        public const string Ver = "Periodos.Ver";
        public const string Crear = "Periodos.Crear";
        public const string Editar = "Periodos.Editar";
    }

    public static class Grados
    {
        public const string Ver = "Grados.Ver";
        public const string Crear = "Grados.Crear";
        public const string Editar = "Grados.Editar";
    }

    public static class Secciones
    {
        public const string Ver = "Secciones.Ver";
        public const string Crear = "Secciones.Crear";
        public const string Editar = "Secciones.Editar";
        public const string CambiarEstado = "Secciones.CambiarEstado";
    }

    public static class Cursos
    {
        public const string Ver = "Cursos.Ver";
        public const string Crear = "Cursos.Crear";
        public const string Editar = "Cursos.Editar";
        public const string CambiarEstado = "Cursos.CambiarEstado";
    }

    public static class Asignaciones
    {
        public const string Ver = "Asignaciones.Ver";
        public const string Crear = "Asignaciones.Crear";
        public const string Editar = "Asignaciones.Editar";
        public const string CambiarEstado = "Asignaciones.CambiarEstado";
    }

    public static class Inscripciones
    {
        public const string Ver = "Inscripciones.Ver";
        public const string Crear = "Inscripciones.Crear";
        public const string Editar = "Inscripciones.Editar";
        public const string Trasladar = "Inscripciones.Trasladar";
        public const string CambiarEstado = "Inscripciones.CambiarEstado";
    }

    public static class Unidades
    {
        public const string Ver = "Unidades.Ver";
        public const string Crear = "Unidades.Crear";
        public const string Editar = "Unidades.Editar";
        public const string Publicar = "Unidades.Publicar";
    }

    public static class Materiales
    {
        public const string Ver = "Materiales.Ver";
        public const string Crear = "Materiales.Crear";
        public const string Editar = "Materiales.Editar";
        public const string Publicar = "Materiales.Publicar";
        public const string Descargar = "Materiales.Descargar";
    }

    public static class Planificaciones
    {
        public const string Ver = "Planificaciones.Ver";
        public const string Crear = "Planificaciones.Crear";
        public const string Editar = "Planificaciones.Editar";
        public const string EnviarRevision = "Planificaciones.EnviarRevision";
        public const string Revisar = "Planificaciones.Revisar";
    }

    public static class Documentos
    {
        public const string Ver = "Documentos.Ver";
        public const string Crear = "Documentos.Crear";
        public const string Descargar = "Documentos.Descargar";
    }

    public static class Tareas
    {
        public const string Ver = "Tareas.Ver";
        public const string Crear = "Tareas.Crear";
        public const string Editar = "Tareas.Editar";
        public const string Publicar = "Tareas.Publicar";
        public const string Cerrar = "Tareas.Cerrar";
    }

    public static class Entregas
    {
        public const string Ver = "Entregas.Ver";
        public const string Entregar = "Entregas.Entregar";
        public const string Calificar = "Entregas.Calificar";
        public const string Reabrir = "Entregas.Reabrir";
    }

    public static class Cuestionarios
    {
        public const string Ver = "Cuestionarios.Ver";
        public const string Crear = "Cuestionarios.Crear";
        public const string Editar = "Cuestionarios.Editar";
        public const string Resolver = "Cuestionarios.Resolver";
    }

    public static class ContenidoIA
    {
        public const string Generar = "ContenidoIA.Generar";
    }

    public static class Notificaciones
    {
        public const string Ver = "Notificaciones.Ver";
    }

    public static class Calificaciones
    {
        public const string Ver = "Calificaciones.Ver";
        public const string Configurar = "Calificaciones.Configurar";
        public const string Registrar = "Calificaciones.Registrar";
        public const string Cerrar = "Calificaciones.Cerrar";
        public const string SolicitarCorreccion = "Calificaciones.SolicitarCorreccion";
        public const string AprobarCorreccion = "Calificaciones.AprobarCorreccion";
    }

    public static IEnumerable<string> ObtenerTodos()
    {
        return
        [
            Dashboard.CoordinacionVer,

            Usuarios.Ver,
            Usuarios.Crear,
            Usuarios.Editar,
            Usuarios.CambiarEstado,
            Usuarios.RestablecerContrasena,

            Roles.Ver,
            Roles.Crear,
            Roles.Editar,
            Roles.CambiarEstado,
            Roles.AsignarPermisos,

            PermisosSistema.Ver,
            PermisosSistema.Crear,
            PermisosSistema.Editar,
            PermisosSistema.CambiarEstado,

            Docentes.Ver,
            Docentes.Crear,
            Docentes.Editar,
            Docentes.CambiarEstado,

            Alumnos.Ver,
            Alumnos.Crear,
            Alumnos.Editar,
            Alumnos.CambiarEstado,

            Encargados.Ver,
            Encargados.Crear,
            Encargados.Editar,

            Ciclos.Ver,
            Ciclos.Crear,
            Ciclos.Editar,
            Ciclos.Activar,

            Periodos.Ver,
            Periodos.Crear,
            Periodos.Editar,

            Grados.Ver,
            Grados.Crear,
            Grados.Editar,

            Secciones.Ver,
            Secciones.Crear,
            Secciones.Editar,
            Secciones.CambiarEstado,

            Cursos.Ver,
            Cursos.Crear,
            Cursos.Editar,
            Cursos.CambiarEstado,

            Asignaciones.Ver,
            Asignaciones.Crear,
            Asignaciones.Editar,
            Asignaciones.CambiarEstado,

            Inscripciones.Ver,
            Inscripciones.Crear,
            Inscripciones.Editar,
            Inscripciones.Trasladar,
            Inscripciones.CambiarEstado,

            Unidades.Ver,
            Unidades.Crear,
            Unidades.Editar,
            Unidades.Publicar,

            Materiales.Ver,
            Materiales.Crear,
            Materiales.Editar,
            Materiales.Publicar,
            Materiales.Descargar,

            Planificaciones.Ver,
            Planificaciones.Crear,
            Planificaciones.Editar,
            Planificaciones.EnviarRevision,
            Planificaciones.Revisar,

            Documentos.Ver,
            Documentos.Crear,
            Documentos.Descargar,

            Tareas.Ver,
            Tareas.Crear,
            Tareas.Editar,
            Tareas.Publicar,
            Tareas.Cerrar,

            Entregas.Ver,
            Entregas.Entregar,
            Entregas.Calificar,
            Entregas.Reabrir,

            Cuestionarios.Ver,
            Cuestionarios.Crear,
            Cuestionarios.Editar,
            Cuestionarios.Resolver,

            ContenidoIA.Generar,
            Notificaciones.Ver,

            Calificaciones.Ver,
            Calificaciones.Configurar,
            Calificaciones.Registrar,
            Calificaciones.Cerrar,
            Calificaciones.SolicitarCorreccion,
            Calificaciones.AprobarCorreccion
        ];
    }
}
