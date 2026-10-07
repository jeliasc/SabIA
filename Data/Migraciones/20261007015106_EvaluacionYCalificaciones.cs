using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class EvaluacionYCalificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RespuestasAlumnosOpciones_OpcionesPreguntas_OpcionPreguntaId",
                table: "RespuestasAlumnosOpciones");

            migrationBuilder.DropForeignKey(
                name: "FK_RespuestasAlumnosOpciones_RespuestasAlumnos_RespuestaAlumno~",
                table: "RespuestasAlumnosOpciones");

            migrationBuilder.DropIndex(
                name: "IX_RespuestasAlumnosOpciones_OpcionPreguntaId",
                table: "RespuestasAlumnosOpciones");

            migrationBuilder.AddColumn<int>(
                name: "PreguntaId",
                table: "RespuestasAlumnosOpciones",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Tareas_Id_AsignacionId",
                table: "Tareas",
                columns: new[] { "Id", "AsignacionId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RespuestasAlumnos_Id_PreguntaId",
                table: "RespuestasAlumnos",
                columns: new[] { "Id", "PreguntaId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Periodos_Id_CicloEscolarId",
                table: "Periodos",
                columns: new[] { "Id", "CicloEscolarId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_OpcionesPreguntas_Id_PreguntaId",
                table: "OpcionesPreguntas",
                columns: new[] { "Id", "PreguntaId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Asignaciones_Id_SeccionId",
                table: "Asignaciones",
                columns: new[] { "Id", "SeccionId" });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AsignacionId = table.Column<int>(type: "integer", nullable: false),
                    SeccionId = table.Column<int>(type: "integer", nullable: false),
                    CicloEscolarId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false),
                    MetodoCalculo = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActivacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfiguradoPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesEvaluacion", x => x.Id);
                    table.UniqueConstraint("AK_ConfiguracionesEvaluacion_Id_AsignacionId", x => new { x.Id, x.AsignacionId });
                    table.ForeignKey(
                        name: "FK_ConfiguracionesEvaluacion_Asignaciones_AsignacionId_Seccion~",
                        columns: x => new { x.AsignacionId, x.SeccionId },
                        principalTable: "Asignaciones",
                        principalColumns: new[] { "Id", "SeccionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesEvaluacion_AspNetUsers_ConfiguradoPorUsuario~",
                        column: x => x.ConfiguradoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesEvaluacion_Periodos_PeriodoId_CicloEscolarId",
                        columns: x => new { x.PeriodoId, x.CicloEscolarId },
                        principalTable: "Periodos",
                        principalColumns: new[] { "Id", "CicloEscolarId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesEvaluacion_Secciones_SeccionId_CicloEscolarId",
                        columns: x => new { x.SeccionId, x.CicloEscolarId },
                        principalTable: "Secciones",
                        principalColumns: new[] { "Id", "CicloEscolarId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesNotasAnuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CicloEscolarId = table.Column<int>(type: "integer", nullable: false),
                    MetodoCalculo = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActivacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfiguradoPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesNotasAnuales", x => x.Id);
                    table.UniqueConstraint("AK_ConfiguracionesNotasAnuales_Id_CicloEscolarId", x => new { x.Id, x.CicloEscolarId });
                    table.ForeignKey(
                        name: "FK_ConfiguracionesNotasAnuales_AspNetUsers_ConfiguradoPorUsuar~",
                        column: x => x.ConfiguradoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesNotasAnuales_CiclosEscolares_CicloEscolarId",
                        column: x => x.CicloEscolarId,
                        principalTable: "CiclosEscolares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfiguracionEvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasEvaluacion", x => x.Id);
                    table.UniqueConstraint("AK_CategoriasEvaluacion_Id_ConfiguracionEvaluacionId", x => new { x.Id, x.ConfiguracionEvaluacionId });
                    table.CheckConstraint("CK_CategoriasEvaluacion_Orden", "\"Orden\" >= 1");
                    table.CheckConstraint("CK_CategoriasEvaluacion_Porcentaje", "\"Porcentaje\" >= 0 AND \"Porcentaje\" <= 100");
                    table.ForeignKey(
                        name: "FK_CategoriasEvaluacion_ConfiguracionesEvaluacion_Configuracio~",
                        column: x => x.ConfiguracionEvaluacionId,
                        principalTable: "ConfiguracionesEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CierresCalificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfiguracionEvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CerradoPorUsuarioId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CierresCalificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CierresCalificaciones_AspNetUsers_CerradoPorUsuarioId",
                        column: x => x.CerradoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CierresCalificaciones_ConfiguracionesEvaluacion_Configuraci~",
                        column: x => x.ConfiguracionEvaluacionId,
                        principalTable: "ConfiguracionesEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PonderacionesPeriodosAnuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfiguracionNotaAnualId = table.Column<int>(type: "integer", nullable: false),
                    CicloEscolarId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PonderacionesPeriodosAnuales", x => x.Id);
                    table.CheckConstraint("CK_PonderacionesPeriodosAnuales_Porcentaje", "\"Porcentaje\" >= 0 AND \"Porcentaje\" <= 100");
                    table.ForeignKey(
                        name: "FK_PonderacionesPeriodosAnuales_ConfiguracionesNotasAnuales_Co~",
                        columns: x => new { x.ConfiguracionNotaAnualId, x.CicloEscolarId },
                        principalTable: "ConfiguracionesNotasAnuales",
                        principalColumns: new[] { "Id", "CicloEscolarId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PonderacionesPeriodosAnuales_Periodos_PeriodoId_CicloEscola~",
                        columns: x => new { x.PeriodoId, x.CicloEscolarId },
                        principalTable: "Periodos",
                        principalColumns: new[] { "Id", "CicloEscolarId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActividadesEvaluables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfiguracionEvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    AsignacionId = table.Column<int>(type: "integer", nullable: false),
                    CategoriaEvaluacionId = table.Column<int>(type: "integer", nullable: true),
                    Origen = table.Column<int>(type: "integer", nullable: false),
                    TareaId = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PunteoMaximo = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActividadesEvaluables", x => x.Id);
                    table.CheckConstraint("CK_ActividadesEvaluables_Orden", "\"Orden\" >= 1");
                    table.CheckConstraint("CK_ActividadesEvaluables_Origen", "(\"Origen\" = 1 AND \"TareaId\" IS NOT NULL) OR (\"Origen\" = 2 AND \"TareaId\" IS NULL)");
                    table.CheckConstraint("CK_ActividadesEvaluables_Punteo", "\"PunteoMaximo\" > 0");
                    table.ForeignKey(
                        name: "FK_ActividadesEvaluables_CategoriasEvaluacion_CategoriaEvaluac~",
                        columns: x => new { x.CategoriaEvaluacionId, x.ConfiguracionEvaluacionId },
                        principalTable: "CategoriasEvaluacion",
                        principalColumns: new[] { "Id", "ConfiguracionEvaluacionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActividadesEvaluables_ConfiguracionesEvaluacion_Configuraci~",
                        columns: x => new { x.ConfiguracionEvaluacionId, x.AsignacionId },
                        principalTable: "ConfiguracionesEvaluacion",
                        principalColumns: new[] { "Id", "AsignacionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActividadesEvaluables_Tareas_TareaId_AsignacionId",
                        columns: x => new { x.TareaId, x.AsignacionId },
                        principalTable: "Tareas",
                        principalColumns: new[] { "Id", "AsignacionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCierresCalificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CierreCalificacionesId = table.Column<int>(type: "integer", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "integer", nullable: false),
                    EstadoNuevo = table.Column<int>(type: "integer", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RealizadoPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCierresCalificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCierresCalificaciones_AspNetUsers_RealizadoPorUs~",
                        column: x => x.RealizadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosCierresCalificaciones_CierresCalificaciones_Cier~",
                        column: x => x.CierreCalificacionesId,
                        principalTable: "CierresCalificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalificacionesManuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActividadEvaluableId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Nota = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaCalificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CalificadoPorUsuarioId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalificacionesManuales", x => x.Id);
                    table.CheckConstraint("CK_CalificacionesManuales_Nota", "\"Nota\" IS NULL OR \"Nota\" >= 0");
                    table.ForeignKey(
                        name: "FK_CalificacionesManuales_ActividadesEvaluables_ActividadEvalu~",
                        column: x => x.ActividadEvaluableId,
                        principalTable: "ActividadesEvaluables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalificacionesManuales_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalificacionesManuales_AspNetUsers_CalificadoPorUsuarioId",
                        column: x => x.CalificadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialesCalificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TipoOrigen = table.Column<int>(type: "integer", nullable: false),
                    CalificacionManualId = table.Column<int>(type: "integer", nullable: true),
                    EntregaId = table.Column<int>(type: "integer", nullable: true),
                    IntentoCuestionarioId = table.Column<int>(type: "integer", nullable: true),
                    RespuestaAlumnoId = table.Column<int>(type: "integer", nullable: true),
                    NotaAnterior = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    NotaNueva = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModificadoPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialesCalificaciones", x => x.Id);
                    table.CheckConstraint("CK_HistorialesCalificaciones_Notas", "(\"NotaAnterior\" IS NULL OR \"NotaAnterior\" >= 0) AND (\"NotaNueva\" IS NULL OR \"NotaNueva\" >= 0)");
                    table.CheckConstraint("CK_HistorialesCalificaciones_Origen", "(\"TipoOrigen\" = 1 AND \"CalificacionManualId\" IS NOT NULL AND \"EntregaId\" IS NULL AND \"IntentoCuestionarioId\" IS NULL AND \"RespuestaAlumnoId\" IS NULL) OR (\"TipoOrigen\" = 2 AND \"EntregaId\" IS NOT NULL AND \"CalificacionManualId\" IS NULL AND \"IntentoCuestionarioId\" IS NULL AND \"RespuestaAlumnoId\" IS NULL) OR (\"TipoOrigen\" = 3 AND \"IntentoCuestionarioId\" IS NOT NULL AND \"CalificacionManualId\" IS NULL AND \"EntregaId\" IS NULL AND \"RespuestaAlumnoId\" IS NULL) OR (\"TipoOrigen\" = 4 AND \"RespuestaAlumnoId\" IS NOT NULL AND \"CalificacionManualId\" IS NULL AND \"EntregaId\" IS NULL AND \"IntentoCuestionarioId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_HistorialesCalificaciones_AspNetUsers_ModificadoPorUsuarioId",
                        column: x => x.ModificadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialesCalificaciones_CalificacionesManuales_Calificaci~",
                        column: x => x.CalificacionManualId,
                        principalTable: "CalificacionesManuales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialesCalificaciones_Entregas_EntregaId",
                        column: x => x.EntregaId,
                        principalTable: "Entregas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialesCalificaciones_IntentosCuestionarios_IntentoCues~",
                        column: x => x.IntentoCuestionarioId,
                        principalTable: "IntentosCuestionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialesCalificaciones_RespuestasAlumnos_RespuestaAlumno~",
                        column: x => x.RespuestaAlumnoId,
                        principalTable: "RespuestasAlumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasAlumnosOpciones_OpcionPreguntaId_PreguntaId",
                table: "RespuestasAlumnosOpciones",
                columns: new[] { "OpcionPreguntaId", "PreguntaId" });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasAlumnosOpciones_RespuestaAlumnoId_PreguntaId",
                table: "RespuestasAlumnosOpciones",
                columns: new[] { "RespuestaAlumnoId", "PreguntaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesEvaluables_CategoriaEvaluacionId_ConfiguracionEv~",
                table: "ActividadesEvaluables",
                columns: new[] { "CategoriaEvaluacionId", "ConfiguracionEvaluacionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesEvaluables_ConfiguracionEvaluacionId_AsignacionId",
                table: "ActividadesEvaluables",
                columns: new[] { "ConfiguracionEvaluacionId", "AsignacionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesEvaluables_ConfiguracionEvaluacionId_Orden",
                table: "ActividadesEvaluables",
                columns: new[] { "ConfiguracionEvaluacionId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesEvaluables_TareaId",
                table: "ActividadesEvaluables",
                column: "TareaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActividadesEvaluables_TareaId_AsignacionId",
                table: "ActividadesEvaluables",
                columns: new[] { "TareaId", "AsignacionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesManuales_ActividadEvaluableId_AlumnoId",
                table: "CalificacionesManuales",
                columns: new[] { "ActividadEvaluableId", "AlumnoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesManuales_AlumnoId",
                table: "CalificacionesManuales",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesManuales_CalificadoPorUsuarioId",
                table: "CalificacionesManuales",
                column: "CalificadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasEvaluacion_ConfiguracionEvaluacionId_Nombre",
                table: "CategoriasEvaluacion",
                columns: new[] { "ConfiguracionEvaluacionId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasEvaluacion_ConfiguracionEvaluacionId_Orden",
                table: "CategoriasEvaluacion",
                columns: new[] { "ConfiguracionEvaluacionId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CierresCalificaciones_CerradoPorUsuarioId",
                table: "CierresCalificaciones",
                column: "CerradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CierresCalificaciones_ConfiguracionEvaluacionId",
                table: "CierresCalificaciones",
                column: "ConfiguracionEvaluacionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesEvaluacion_AsignacionId_PeriodoId",
                table: "ConfiguracionesEvaluacion",
                columns: new[] { "AsignacionId", "PeriodoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesEvaluacion_AsignacionId_SeccionId",
                table: "ConfiguracionesEvaluacion",
                columns: new[] { "AsignacionId", "SeccionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesEvaluacion_ConfiguradoPorUsuarioId",
                table: "ConfiguracionesEvaluacion",
                column: "ConfiguradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesEvaluacion_PeriodoId_CicloEscolarId",
                table: "ConfiguracionesEvaluacion",
                columns: new[] { "PeriodoId", "CicloEscolarId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesEvaluacion_SeccionId_CicloEscolarId",
                table: "ConfiguracionesEvaluacion",
                columns: new[] { "SeccionId", "CicloEscolarId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesNotasAnuales_CicloEscolarId",
                table: "ConfiguracionesNotasAnuales",
                column: "CicloEscolarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesNotasAnuales_ConfiguradoPorUsuarioId",
                table: "ConfiguracionesNotasAnuales",
                column: "ConfiguradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesCalificaciones_CalificacionManualId",
                table: "HistorialesCalificaciones",
                column: "CalificacionManualId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesCalificaciones_EntregaId",
                table: "HistorialesCalificaciones",
                column: "EntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesCalificaciones_FechaCambio",
                table: "HistorialesCalificaciones",
                column: "FechaCambio");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesCalificaciones_IntentoCuestionarioId",
                table: "HistorialesCalificaciones",
                column: "IntentoCuestionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesCalificaciones_ModificadoPorUsuarioId",
                table: "HistorialesCalificaciones",
                column: "ModificadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialesCalificaciones_RespuestaAlumnoId",
                table: "HistorialesCalificaciones",
                column: "RespuestaAlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCierresCalificaciones_CierreCalificacionesId_Fec~",
                table: "MovimientosCierresCalificaciones",
                columns: new[] { "CierreCalificacionesId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCierresCalificaciones_RealizadoPorUsuarioId",
                table: "MovimientosCierresCalificaciones",
                column: "RealizadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PonderacionesPeriodosAnuales_ConfiguracionNotaAnualId_Ciclo~",
                table: "PonderacionesPeriodosAnuales",
                columns: new[] { "ConfiguracionNotaAnualId", "CicloEscolarId" });

            migrationBuilder.CreateIndex(
                name: "IX_PonderacionesPeriodosAnuales_ConfiguracionNotaAnualId_Perio~",
                table: "PonderacionesPeriodosAnuales",
                columns: new[] { "ConfiguracionNotaAnualId", "PeriodoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PonderacionesPeriodosAnuales_PeriodoId_CicloEscolarId",
                table: "PonderacionesPeriodosAnuales",
                columns: new[] { "PeriodoId", "CicloEscolarId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RespuestasAlumnosOpciones_OpcionesPreguntas_OpcionPreguntaI~",
                table: "RespuestasAlumnosOpciones",
                columns: new[] { "OpcionPreguntaId", "PreguntaId" },
                principalTable: "OpcionesPreguntas",
                principalColumns: new[] { "Id", "PreguntaId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RespuestasAlumnosOpciones_RespuestasAlumnos_RespuestaAlumno~",
                table: "RespuestasAlumnosOpciones",
                columns: new[] { "RespuestaAlumnoId", "PreguntaId" },
                principalTable: "RespuestasAlumnos",
                principalColumns: new[] { "Id", "PreguntaId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RespuestasAlumnosOpciones_OpcionesPreguntas_OpcionPreguntaI~",
                table: "RespuestasAlumnosOpciones");

            migrationBuilder.DropForeignKey(
                name: "FK_RespuestasAlumnosOpciones_RespuestasAlumnos_RespuestaAlumno~",
                table: "RespuestasAlumnosOpciones");

            migrationBuilder.DropTable(
                name: "HistorialesCalificaciones");

            migrationBuilder.DropTable(
                name: "MovimientosCierresCalificaciones");

            migrationBuilder.DropTable(
                name: "PonderacionesPeriodosAnuales");

            migrationBuilder.DropTable(
                name: "CalificacionesManuales");

            migrationBuilder.DropTable(
                name: "CierresCalificaciones");

            migrationBuilder.DropTable(
                name: "ConfiguracionesNotasAnuales");

            migrationBuilder.DropTable(
                name: "ActividadesEvaluables");

            migrationBuilder.DropTable(
                name: "CategoriasEvaluacion");

            migrationBuilder.DropTable(
                name: "ConfiguracionesEvaluacion");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Tareas_Id_AsignacionId",
                table: "Tareas");

            migrationBuilder.DropIndex(
                name: "IX_RespuestasAlumnosOpciones_OpcionPreguntaId_PreguntaId",
                table: "RespuestasAlumnosOpciones");

            migrationBuilder.DropIndex(
                name: "IX_RespuestasAlumnosOpciones_RespuestaAlumnoId_PreguntaId",
                table: "RespuestasAlumnosOpciones");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RespuestasAlumnos_Id_PreguntaId",
                table: "RespuestasAlumnos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Periodos_Id_CicloEscolarId",
                table: "Periodos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_OpcionesPreguntas_Id_PreguntaId",
                table: "OpcionesPreguntas");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Asignaciones_Id_SeccionId",
                table: "Asignaciones");

            migrationBuilder.DropColumn(
                name: "PreguntaId",
                table: "RespuestasAlumnosOpciones");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasAlumnosOpciones_OpcionPreguntaId",
                table: "RespuestasAlumnosOpciones",
                column: "OpcionPreguntaId");

            migrationBuilder.AddForeignKey(
                name: "FK_RespuestasAlumnosOpciones_OpcionesPreguntas_OpcionPreguntaId",
                table: "RespuestasAlumnosOpciones",
                column: "OpcionPreguntaId",
                principalTable: "OpcionesPreguntas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RespuestasAlumnosOpciones_RespuestasAlumnos_RespuestaAlumno~",
                table: "RespuestasAlumnosOpciones",
                column: "RespuestaAlumnoId",
                principalTable: "RespuestasAlumnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
