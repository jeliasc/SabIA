using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class CrearEstructuraPlanificacionesAcademicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Planificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrupoVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroVersion = table.Column<int>(type: "integer", nullable: false),
                    PlanificacionAnteriorId = table.Column<int>(type: "integer", nullable: true),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Objetivos = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEnvioRevision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AprobadoPorUsuarioId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Planificaciones_AspNetUsers_AprobadoPorUsuarioId",
                        column: x => x.AprobadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Planificaciones_Docentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Planificaciones_Planificaciones_PlanificacionAnteriorId",
                        column: x => x.PlanificacionAnteriorId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanificacionesArchivos",
                columns: table => new
                {
                    PlanificacionId = table.Column<int>(type: "integer", nullable: false),
                    ArchivoId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanificacionesArchivos", x => new { x.PlanificacionId, x.ArchivoId });
                    table.ForeignKey(
                        name: "FK_PlanificacionesArchivos_Archivos_ArchivoId",
                        column: x => x.ArchivoId,
                        principalTable: "Archivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanificacionesArchivos_Planificaciones_PlanificacionId",
                        column: x => x.PlanificacionId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanificacionesAsignaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanificacionId = table.Column<int>(type: "integer", nullable: false),
                    AsignacionId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: true),
                    UnidadAsignacionId = table.Column<int>(type: "integer", nullable: true),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanificacionesAsignaciones", x => x.Id);
                    table.CheckConstraint("CK_PlanificacionesAsignaciones_Fechas", "\"FechaInicio\" <= \"FechaFin\"");
                    table.ForeignKey(
                        name: "FK_PlanificacionesAsignaciones_Asignaciones_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanificacionesAsignaciones_Periodos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "Periodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanificacionesAsignaciones_Planificaciones_PlanificacionId",
                        column: x => x.PlanificacionId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanificacionesAsignaciones_UnidadesAsignaciones_UnidadAsig~",
                        column: x => x.UnidadAsignacionId,
                        principalTable: "UnidadesAsignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanificacionesDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanificacionId = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Competencia = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IndicadorLogro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Contenido = table.Column<string>(type: "text", nullable: false),
                    Actividades = table.Column<string>(type: "text", nullable: true),
                    Recursos = table.Column<string>(type: "text", nullable: true),
                    EstrategiaEvaluacion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanificacionesDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanificacionesDetalles_Planificaciones_PlanificacionId",
                        column: x => x.PlanificacionId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RevisionesPlanificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanificacionId = table.Column<int>(type: "integer", nullable: false),
                    Accion = table.Column<int>(type: "integer", nullable: false),
                    RealizadoPorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperacionLoteId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevisionesPlanificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevisionesPlanificaciones_AspNetUsers_RealizadoPorUsuarioId",
                        column: x => x.RealizadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RevisionesPlanificaciones_Planificaciones_PlanificacionId",
                        column: x => x.PlanificacionId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Planificaciones_AprobadoPorUsuarioId",
                table: "Planificaciones",
                column: "AprobadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Planificaciones_DocenteId_Estado",
                table: "Planificaciones",
                columns: new[] { "DocenteId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Planificaciones_Estado_FechaEnvioRevision",
                table: "Planificaciones",
                columns: new[] { "Estado", "FechaEnvioRevision" });

            migrationBuilder.CreateIndex(
                name: "IX_Planificaciones_GrupoVersionId_NumeroVersion",
                table: "Planificaciones",
                columns: new[] { "GrupoVersionId", "NumeroVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planificaciones_PlanificacionAnteriorId",
                table: "Planificaciones",
                column: "PlanificacionAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificacionesArchivos_ArchivoId",
                table: "PlanificacionesArchivos",
                column: "ArchivoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificacionesAsignaciones_AsignacionId",
                table: "PlanificacionesAsignaciones",
                column: "AsignacionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificacionesAsignaciones_PeriodoId",
                table: "PlanificacionesAsignaciones",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificacionesAsignaciones_PlanificacionId_AsignacionId",
                table: "PlanificacionesAsignaciones",
                columns: new[] { "PlanificacionId", "AsignacionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanificacionesAsignaciones_UnidadAsignacionId",
                table: "PlanificacionesAsignaciones",
                column: "UnidadAsignacionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanificacionesDetalles_PlanificacionId_Orden",
                table: "PlanificacionesDetalles",
                columns: new[] { "PlanificacionId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesPlanificaciones_OperacionLoteId",
                table: "RevisionesPlanificaciones",
                column: "OperacionLoteId");

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesPlanificaciones_PlanificacionId_Fecha",
                table: "RevisionesPlanificaciones",
                columns: new[] { "PlanificacionId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_RevisionesPlanificaciones_RealizadoPorUsuarioId",
                table: "RevisionesPlanificaciones",
                column: "RealizadoPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanificacionesArchivos");

            migrationBuilder.DropTable(
                name: "PlanificacionesAsignaciones");

            migrationBuilder.DropTable(
                name: "PlanificacionesDetalles");

            migrationBuilder.DropTable(
                name: "RevisionesPlanificaciones");

            migrationBuilder.DropTable(
                name: "Planificaciones");
        }
    }
}
