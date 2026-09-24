namespace Proyecto_Final.Seguridad;

public static class Permisos
{
    public static class Usuarios
    {
        public const string Ver = "Usuarios.Ver";
        public const string Crear = "Usuarios.Crear";
        public const string Editar = "Usuarios.Editar";
        public const string Desactivar = "Usuarios.Desactivar";
        public const string CambiarEstado = "Usuarios.CambiarEstado";
        public const string RestablecerContrasena =
            "Usuarios.RestablecerContrasena";
    }

    public static class Roles
    {
        public const string Ver = "Roles.Ver";
        public const string Crear = "Roles.Crear";
        public const string Editar = "Roles.Editar";
        public const string Desactivar = "Roles.Desactivar";
        public const string AsignarPermisos =
            "Roles.AsignarPermisos";
    }

    public static class PermisosSistema
    {
        public const string Ver = "Permisos.Ver";
        public const string Crear = "Permisos.Crear";
        public const string Editar = "Permisos.Editar";
        public const string Desactivar = "Permisos.Desactivar";
    }

    public static IEnumerable<string> ObtenerTodos()
    {
        return
        [
            Usuarios.Ver,
            Usuarios.Crear,
            Usuarios.Editar,
            Usuarios.CambiarEstado,
            Usuarios.RestablecerContrasena,

            Roles.Ver,
            Roles.Crear,
            Roles.Editar,
            Roles.Desactivar,
            Roles.AsignarPermisos,

            PermisosSistema.Ver,
            PermisosSistema.Crear,
            PermisosSistema.Editar,
            PermisosSistema.Desactivar
        ];
    }
}