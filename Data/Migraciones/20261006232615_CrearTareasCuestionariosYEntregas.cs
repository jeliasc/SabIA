using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class CrearTareasCuestionariosYEntregas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tareas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AsignacionId = table.Column<int>(type: "integer", nullable: false),
                    UnidadAsignacionId = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Instrucciones = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    PunteoMaximo = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    FechaDisponibilidad = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaLimite = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PermitirEntregaTardia = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaPublicacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreadoPorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    GrupoVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroVersion = table.Column<int>(type: "integer", nullable: false),
                    TareaAnteriorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tareas", x => x.Id);
                    table.CheckConstraint("CK_Tareas_Fechas", "\"FechaDisponibilidad\" IS NULL OR \"FechaLimite\" IS NULL OR \"FechaDisponibilidad\" <= \"FechaLimite\"");
                    table.CheckConstraint("CK_Tareas_NumeroVersion", "\"NumeroVersion\" >= 1");
                    table.CheckConstraint("CK_Tareas_Punteo", "\"PunteoMaximo\" >= 0");
                    table.ForeignKey(
                        name: "FK_Tareas_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tareas_AspNetUsers_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tareas_Tareas_TareaAnteriorId",
                        column: x => x.TareaAnteriorId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tareas_UnidadesAsignaciones_UnidadAsignacionId",
                        column: x => x.UnidadAsignacionId,
                        principalTable: "UnidadesAsignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cuestionarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TareaId = table.Column<int>(type: "integer", nullable: false),
                    Origen = table.Column<int>(type: "integer", nullable: false),
                    ModalidadTiempo = table.Column<int>(type: "integer", nullable: false),
                    TiempoGeneralSegundos = table.Column<int>(type: "integer", nullable: true),
                    MaximoIntentos = table.Column<int>(type: "integer", nullable: false),
                    CriterioCalificacion = table.Column<int>(type: "integer", nullable: false),
                    MostrarResultadosAlFinalizar = table.Column<bool>(type: "boolean", nullable: false),
                    MostrarRespuestasCorrectas = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuestionarios", x => x.Id);
                    table.CheckConstraint("CK_Cuestionarios_Intentos", "\"MaximoIntentos\" >= 0");
                    table.CheckConstraint("CK_Cuestionarios_Tiempo", "\"TiempoGeneralSegundos\" IS NULL OR \"TiempoGeneralSegundos\" > 0");
                    table.ForeignKey(
                        name: "FK_Cuestionarios_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Entregas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TareaId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroEnvio = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    ComentarioAlumno = table.Column<string>(type: "text", nullable: true),
                    FechaEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Calificacion = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    Retroalimentacion = table.Column<string>(type: "text", nullable: true),
                    FechaCalificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CalificadoPorUsuarioId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entregas", x => x.Id);
                    table.CheckConstraint("CK_Entregas_Calificacion", "\"Calificacion\" IS NULL OR \"Calificacion\" >= 0");
                    table.CheckConstraint("CK_Entregas_NumeroEnvio", "\"NumeroEnvio\" >= 1");
                    table.ForeignKey(
                        name: "FK_Entregas_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregas_AspNetUsers_CalificadoPorUsuarioId",
                        column: x => x.CalificadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entregas_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TareasArchivos",
                columns: table => new
                {
                    TareaId = table.Column<int>(type: "integer", nullable: false),
                    ArchivoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareasArchivos", x => new { x.TareaId, x.ArchivoId });
                    table.ForeignKey(
                        name: "FK_TareasArchivos_Archivos_ArchivoId",
                        column: x => x.ArchivoId,
                        principalTable: "Archivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TareasArchivos_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntentosCuestionarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CuestionarioId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroIntento = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinalizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Calificacion = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    FechaCalificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntentosCuestionarios", x => x.Id);
                    table.CheckConstraint("CK_IntentosCuestionarios_Calificacion", "\"Calificacion\" IS NULL OR \"Calificacion\" >= 0");
                    table.CheckConstraint("CK_IntentosCuestionarios_Fechas", "\"FechaFinalizacion\" IS NULL OR \"FechaFinalizacion\" >= \"FechaInicio\"");
                    table.CheckConstraint("CK_IntentosCuestionarios_NumeroIntento", "\"NumeroIntento\" >= 1");
                    table.ForeignKey(
                        name: "FK_IntentosCuestionarios_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntentosCuestionarios_Cuestionarios_CuestionarioId",
                        column: x => x.CuestionarioId,
                        principalTable: "Cuestionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Preguntas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CuestionarioId = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Enunciado = table.Column<string>(type: "text", nullable: false),
                    Punteo = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    TiempoLimiteSegundos = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preguntas", x => x.Id);
                    table.CheckConstraint("CK_Preguntas_Orden", "\"Orden\" >= 1");
                    table.CheckConstraint("CK_Preguntas_Punteo", "\"Punteo\" > 0");
                    table.CheckConstraint("CK_Preguntas_Tiempo", "\"TiempoLimiteSegundos\" IS NULL OR \"TiempoLimiteSegundos\" > 0");
                    table.ForeignKey(
                        name: "FK_Preguntas_Cuestionarios_CuestionarioId",
                        column: x => x.CuestionarioId,
                        principalTable: "Cuestionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntregasArchivos",
                columns: table => new
                {
                    EntregaId = table.Column<int>(type: "integer", nullable: false),
                    ArchivoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntregasArchivos", x => new { x.EntregaId, x.ArchivoId });
                    table.ForeignKey(
                        name: "FK_EntregasArchivos_Archivos_ArchivoId",
                        column: x => x.ArchivoId,
                        principalTable: "Archivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntregasArchivos_Entregas_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entregas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpcionesPreguntas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PreguntaId = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    EsCorrecta = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcionesPreguntas", x => x.Id);
                    table.CheckConstraint("CK_OpcionesPreguntas_Orden", "\"Orden\" >= 1");
                    table.ForeignKey(
                        name: "FK_OpcionesPreguntas_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RespuestasAceptadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PreguntaId = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestasAceptadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespuestasAceptadas_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RespuestasAlumnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IntentoCuestionarioId = table.Column<int>(type: "integer", nullable: false),
                    PreguntaId = table.Column<int>(type: "integer", nullable: false),
                    TextoRespuesta = table.Column<string>(type: "text", nullable: true),
                    FechaPresentacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaRespuesta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PunteoObtenido = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    RequiereRevisionManual = table.Column<bool>(type: "boolean", nullable: false),
                    CalificadoPorUsuarioId = table.Column<string>(type: "text", nullable: true),
                    FechaCalificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestasAlumnos", x => x.Id);
                    table.CheckConstraint("CK_RespuestasAlumnos_Fechas", "\"FechaRespuesta\" IS NULL OR \"FechaRespuesta\" >= \"FechaPresentacion\"");
                    table.CheckConstraint("CK_RespuestasAlumnos_Punteo", "\"PunteoObtenido\" IS NULL OR \"PunteoObtenido\" >= 0");
                    table.ForeignKey(
                        name: "FK_RespuestasAlumnos_AspNetUsers_CalificadoPorUsuarioId",
                        column: x => x.CalificadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespuestasAlumnos_IntentosCuestionarios_IntentoCuestionario~",
                        column: x => x.IntentoCuestionarioId,
                        principalTable: "IntentosCuestionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespuestasAlumnos_Preguntas_PreguntaId",
                        column: x => x.PreguntaId,
                        principalTable: "Preguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RespuestasAlumnosOpciones",
                columns: table => new
                {
                    RespuestaAlumnoId = table.Column<int>(type: "integer", nullable: false),
                    OpcionPreguntaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestasAlumnosOpciones", x => new { x.RespuestaAlumnoId, x.OpcionPreguntaId });
                    table.ForeignKey(
                        name: "FK_RespuestasAlumnosOpciones_OpcionesPreguntas_OpcionPreguntaId",
                        column: x => x.OpcionPreguntaId,
                        principalTable: "OpcionesPreguntas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespuestasAlumnosOpciones_RespuestasAlumnos_RespuestaAlumno~",
                        column: x => x.RespuestaAlumnoId,
                        principalTable: "RespuestasAlumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuestionarios_TareaId",
                table: "Cuestionarios",
                column: "TareaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_AlumnoId",
                table: "Entregas",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_CalificadoPorUsuarioId",
                table: "Entregas",
                column: "CalificadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TareaId_AlumnoId_NumeroEnvio",
                table: "Entregas",
                columns: new[] { "TareaId", "AlumnoId", "NumeroEnvio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntregasArchivos_ArchivoId",
                table: "EntregasArchivos",
                column: "ArchivoId");

            migrationBuilder.CreateIndex(
                name: "IX_IntentosCuestionarios_AlumnoId",
                table: "IntentosCuestionarios",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_IntentosCuestionarios_CuestionarioId_AlumnoId_NumeroIntento",
                table: "IntentosCuestionarios",
                columns: new[] { "CuestionarioId", "AlumnoId", "NumeroIntento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpcionesPreguntas_PreguntaId_Orden",
                table: "OpcionesPreguntas",
                columns: new[] { "PreguntaId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Preguntas_CuestionarioId_Orden",
                table: "Preguntas",
                columns: new[] { "CuestionarioId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasAceptadas_PreguntaId",
                table: "RespuestasAceptadas",
                column: "PreguntaId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasAlumnos_CalificadoPorUsuarioId",
                table: "RespuestasAlumnos",
                column: "CalificadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasAlumnos_IntentoCuestionarioId_PreguntaId",
                table: "RespuestasAlumnos",
                columns: new[] { "IntentoCuestionarioId", "PreguntaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasAlumnos_PreguntaId",
                table: "RespuestasAlumnos",
                column: "PreguntaId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasAlumnosOpciones_OpcionPreguntaId",
                table: "RespuestasAlumnosOpciones",
                column: "OpcionPreguntaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_AsignacionId_Estado_FechaLimite",
                table: "Tareas",
                columns: new[] { "AsignacionId", "Estado", "FechaLimite" });

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_CreadoPorUsuarioId",
                table: "Tareas",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_GrupoVersionId_NumeroVersion",
                table: "Tareas",
                columns: new[] { "GrupoVersionId", "NumeroVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_TareaAnteriorId",
                table: "Tareas",
                column: "TareaAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_UnidadAsignacionId",
                table: "Tareas",
                column: "UnidadAsignacionId");

            migrationBuilder.CreateIndex(
                name: "IX_TareasArchivos_ArchivoId",
                table: "TareasArchivos",
                column: "ArchivoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntregasArchivos");

            migrationBuilder.DropTable(
                name: "RespuestasAceptadas");

            migrationBuilder.DropTable(
                name: "RespuestasAlumnosOpciones");

            migrationBuilder.DropTable(
                name: "TareasArchivos");

            migrationBuilder.DropTable(
                name: "Entregas");

            migrationBuilder.DropTable(
                name: "OpcionesPreguntas");

            migrationBuilder.DropTable(
                name: "RespuestasAlumnos");

            migrationBuilder.DropTable(
                name: "IntentosCuestionarios");

            migrationBuilder.DropTable(
                name: "Preguntas");

            migrationBuilder.DropTable(
                name: "Cuestionarios");

            migrationBuilder.DropTable(
                name: "Tareas");
        }
    }
}
