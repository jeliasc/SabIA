# SabIA - Integración final

Base utilizada: `Proyecto_Final(4).zip`.

## Módulos protegidos

No se modificaron los archivos propios de los módulos ya terminados:

- Usuarios
- Roles
- Permisos
- Autenticación
- Correos

Sus controladores, servicios, ViewModels, vistas y JavaScript específicos se conservaron byte por byte respecto de la base recibida. Los cambios globales de integración (DI, navegación, permisos para módulos nuevos y configuración de IA) se realizaron fuera de la lógica propia de esos módulos.

## Módulos integrados

- Docentes
- Alumnos
- Encargados
- Ciclos escolares
- Períodos
- Grados
- Secciones
- Cursos
- Asignaciones docentes
- Inscripciones y traslados
- Unidades
- Materiales
- Planificaciones
- Documentos internos
- Tareas
- Entregas y retroalimentación
- Cuestionarios
- Generación de contenido con IA
- Notificaciones
- Dashboard según perfil

## Reglas relevantes implementadas

- Restricción del docente a sus asignaciones académicas.
- Inscripción académica por sección/ciclo.
- Validación de grado entre curso y sección al asignar.
- Publicación de tareas con creación transaccional de entregas pendientes para los alumnos inscritos.
- Material visible al alumno únicamente si está publicado y la unidad está liberada.
- Entregas tardías según configuración de la tarea.
- Calificación y retroalimentación como apoyo, sin sustituir el sistema oficial de notas.
- Archivos descargados a través de acciones autorizadas, no mediante exposición directa de la ruta física.
- Generación IA mediante PDF de planificación, `X-API-Key`, envío multipart y consulta del estado por `job_id`.

## Configuración de IA

Configure la clave fuera del repositorio, preferiblemente con User Secrets o variable de entorno. La clave se dejó vacía en `appsettings.json` intencionalmente.

Sección utilizada:

```json
"InteligenciaArtificial": {
  "UrlBase": "http://100.118.205.1:8001/api",
  "ApiKey": ""
}
```

Con User Secrets:

```powershell
dotnet user-secrets set "InteligenciaArtificial:ApiKey" "SU_CLAVE"
```

## Migraciones nuevas de integración

- `20261007113000_PermitirEntregasPendientes`
- `20261007124500_EstadoPublicacionMaterial`

Ejecutar en el equipo de desarrollo:

```powershell
dotnet restore
dotnet build
dotnet ef database update
```

## Validación realizada en el entorno de integración

- Verificación byte por byte de los módulos protegidos contra `Proyecto_Final(4).zip`.
- Verificación de presencia de controlador, servicio y vistas de los módulos integrados.
- Revisión de balance estructural de llaves de todos los archivos C# del proyecto.
- Revisión de archivos vacíos y de marcadores `NotImplementedException` en código integrado.
- Revisión de coherencia de las dos migraciones nuevas con el snapshot.
- Revisión de flujo transaccional de publicación de tareas y creación de entregas pendientes.
- Revisión de filtros de acceso docente/alumno para contenido académico.

Este contenedor no dispone del SDK de .NET, por lo que la compilación real debe confirmarse con `dotnet build` en un equipo que tenga el SDK .NET 10 configurado.
