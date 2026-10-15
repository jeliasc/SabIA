# Intentos de entregas

Las tareas de tipo **Entrega de archivo** permiten 1, 2, 3 o intentos
ilimitados. `NULL` en `Tareas.MaximoIntentosEntrega` significa ilimitados; no
se usa un valor centinela numérico.

La migración `AgregarMaximoIntentosEntrega` establece `1` en todas las tareas
de archivo existentes. No modifica entregas, archivos, calificaciones ni
reaperturas históricas. Las tareas de cuestionario conservan `NULL`, pues usan
su propia configuración.

Cada registro `Entrega` representa un intento. Al enviarlo se conserva
inmutable y, cuando queda cupo general, se crea solamente el siguiente
pendiente. Una reapertura reutiliza el pendiente existente o crea uno adicional
y queda auditada en `ReaperturasEntregas`.

La nota efectiva de una actividad es la del intento calificado más reciente.
Un intento posterior pendiente o sin calificar no elimina esa nota; el
historial señala que existe evaluación pendiente.

Los registros heredados con varios archivos en un mismo intento no se dividen
ni renumeran. Se muestran juntos, cada uno con acciones independientes de
visualización segura (PDF/imágenes permitidas) y descarga.

## Validación

La compilación de la aplicación y del proyecto de pruebas se realizó sin
advertencias ni errores. La suite se ejecuta con el runner nativo de xUnit v3,
porque `dotnet test`/Microsoft Testing Platform falla durante el handshake
antes de descubrir pruebas en este entorno.

La validación PostgreSQL requiere que exista la instancia descartable indicada
por `SABIA_PRUEBAS_PG`. Si el servidor temporal no está disponible, las pruebas
no deben redirigirse a desarrollo, Render ni producción. La migración es no
destructiva: añade la columna, asigna un intento a las tareas de archivo ya
existentes y no toca entregas, archivos ni calificaciones.
