# PostgreSQL descartable para relevos de asignaciones

Estas pruebas nunca deben apuntar a desarrollo compartido, Render ni producción. El ejecutor solo las habilita cuando existen las dos variables siguientes:

```powershell
$env:SABIA_PRUEBAS_PG = 'Host=localhost;Port=55432;Database=postgres;Username=postgres;Password=<clave>;Pooling=false'
$env:SABIA_PRUEBAS_PG_CONFIRMAR = 'BASE_DESCARTABLE'
```

La conexión es administrativa y las pruebas crean bases con el prefijo `sabia_prueba_`, aplican las migraciones y eliminan cada base al terminar. No debe indicarse como `SABIA_PRUEBAS_PG` una cadena de la aplicación.

Una forma aislada de prepararla es iniciar un contenedor PostgreSQL dedicado, con volumen efímero y un puerto que no use la instalación local:

```powershell
docker run --rm --name sabia-pg-pruebas -e POSTGRES_PASSWORD=<clave> -p 55432:5432 postgres:17
```

En otra terminal se configuran las variables anteriores y se ejecuta:

```powershell
dotnet test Tests/Proyecto_Final.Tests/Proyecto_Final.Tests.csproj --filter "Postgres"
```

Al finalizar, se detiene el contenedor. La ausencia de `SABIA_PRUEBAS_PG_CONFIRMAR=BASE_DESCARTABLE` hace que todas las pruebas PostgreSQL se omitan, aun si existe una conexión heredada en el entorno.

## Validaciones obligatorias

- [x] Aplicar todas las migraciones sobre una copia estructural representativa y comprobar filas/FK históricas antes y después. Resultado: `VALIDACION_VERSIONAR_ASIGNACIONES.md`.
- [x] Migrar hacia atrás sin historial incompatible, volver a migrar hacia adelante y comprobar el bloqueo con historial. Resultado: `VALIDACION_VERSIONAR_ASIGNACIONES.md`.
- Probar dos relevos concurrentes y confirmar que solo uno crea la asignación activa.
- Confirmar en PostgreSQL el índice parcial único de `(SeccionId, CursoId)` para `Estado = 1`.
- Verificar delegaciones, cierres de ciclo y acceso de alumnos/docentes sobre datos históricos representativos.

## Respaldo y recuperación previa al despliegue

Antes de una eventual migración autorizada, crear un respaldo lógico verificable:

```powershell
pg_dump --format=custom --no-owner --no-acl --file=sabia_pre_versionado.dump '<CONEXION_AUTORIZADA>'
pg_restore --list sabia_pre_versionado.dump
```

La recuperación segura consiste en detener escrituras, crear una base nueva, restaurar allí el respaldo y validar conteos y relaciones antes de cambiar la conexión de la aplicación:

```powershell
createdb sabia_recuperacion
pg_restore --exit-on-error --clean --if-exists --no-owner --dbname=sabia_recuperacion sabia_pre_versionado.dump
```

No se debe confiar en `database update <migración anterior>` como recuperación de datos: al existir varias versiones históricas, volver a crear la antigua unicidad puede fallar y eliminar las tablas de relevos perdería auditoría. La restauración del respaldo es el plan de recuperación de datos.
