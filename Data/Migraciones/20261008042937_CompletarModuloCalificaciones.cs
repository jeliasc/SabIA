using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class CompletarModuloCalificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IntentosCuestionarios_CuestionarioId_AlumnoId_NumeroIntento",
                table: "IntentosCuestionarios");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_TareaId_AlumnoId_NumeroEnvio",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesManuales_ActividadEvaluableId_AlumnoId",
                table: "CalificacionesManuales");

            migrationBuilder.AddColumn<int>(
                name: "InscripcionId",
                table: "IntentosCuestionarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InscripcionId",
                table: "Entregas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "CategoriasEvaluacion",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "InscripcionId",
                table: "CalificacionesManuales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Inscripciones_Id_AlumnoId",
                table: "Inscripciones",
                columns: new[] { "Id", "AlumnoId" });

            migrationBuilder.CreateTable(
                name: "PlantillasEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CursoId = table.Column<int>(type: "integer", nullable: true),
                    MetodoCalculo = table.Column<int>(type: "integer", nullable: false),
                    DefinicionJson = table.Column<string>(type: "text", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasEvaluacion", x => x.Id);
                    table.CheckConstraint("CK_PlantillasEvaluacion_Definicion", "length(\"DefinicionJson\") > 0");
                    table.ForeignKey(
                        name: "FK_PlantillasEvaluacion_AspNetUsers_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlantillasEvaluacion_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultadosCalificacionesPeriodos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfiguracionEvaluacionId = table.Column<int>(type: "integer", nullable: false),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    Desempeno = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Actitudinal = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    NotaBimestral = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    AbacusDesempeno = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    AbacusActitudinal = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosCalificacionesPeriodos", x => x.Id);
                    table.UniqueConstraint("AK_ResultadosCalificacionesPeriodos_Id_InscripcionId_AlumnoId", x => new { x.Id, x.InscripcionId, x.AlumnoId });
                    table.CheckConstraint("CK_ResultadosCalificacionesPeriodos_Notas", "\"Desempeno\" >= 0 AND \"Actitudinal\" >= 0 AND \"NotaBimestral\" >= 0 AND \"AbacusDesempeno\" >= 0 AND \"AbacusActitudinal\" >= 0");
                    table.ForeignKey(
                        name: "FK_ResultadosCalificacionesPeriodos_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResultadosCalificacionesPeriodos_ConfiguracionesEvaluacion_~",
                        column: x => x.ConfiguracionEvaluacionId,
                        principalTable: "ConfiguracionesEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResultadosCalificacionesPeriodos_Inscripciones_InscripcionI~",
                        columns: x => new { x.InscripcionId, x.AlumnoId },
                        principalTable: "Inscripciones",
                        principalColumns: new[] { "Id", "AlumnoId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCorreccionesCalificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResultadoCalificacionPeriodoId = table.Column<int>(type: "integer", nullable: false),
                    ActividadEvaluableId = table.Column<int>(type: "integer", nullable: false),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    NotaAnterior = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    NotaPropuesta = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    MotivoSolicitud = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    SolicitadaPorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevisadaPorUsuarioId = table.Column<string>(type: "text", nullable: true),
                    FechaRevision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ObservacionRevision = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AplicadaPorUsuarioId = table.Column<string>(type: "text", nullable: true),
                    FechaAplicacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VersionConcurrencia = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCorreccionesCalificaciones", x => x.Id);
                    table.CheckConstraint("CK_SolicitudesCorreccionesCalificaciones_Estado", "(\"Estado\" = 1 AND \"RevisadaPorUsuarioId\" IS NULL AND \"FechaRevision\" IS NULL AND \"AplicadaPorUsuarioId\" IS NULL AND \"FechaAplicacion\" IS NULL) OR (\"Estado\" IN (2, 3) AND \"RevisadaPorUsuarioId\" IS NOT NULL AND \"FechaRevision\" IS NOT NULL AND \"AplicadaPorUsuarioId\" IS NULL AND \"FechaAplicacion\" IS NULL) OR (\"Estado\" = 4 AND \"RevisadaPorUsuarioId\" IS NOT NULL AND \"FechaRevision\" IS NOT NULL AND \"AplicadaPorUsuarioId\" IS NOT NULL AND \"FechaAplicacion\" IS NOT NULL)");
                    table.CheckConstraint("CK_SolicitudesCorreccionesCalificaciones_Notas", "\"NotaAnterior\" >= 0 AND \"NotaPropuesta\" >= 0");
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccionesCalificaciones_ActividadesEvaluables~",
                        column: x => x.ActividadEvaluableId,
                        principalTable: "ActividadesEvaluables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccionesCalificaciones_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccionesCalificaciones_AspNetUsers_AplicadaP~",
                        column: x => x.AplicadaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccionesCalificaciones_AspNetUsers_RevisadaP~",
                        column: x => x.RevisadaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccionesCalificaciones_AspNetUsers_Solicitad~",
                        column: x => x.SolicitadaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccionesCalificaciones_Inscripciones_Inscrip~",
                        columns: x => new { x.InscripcionId, x.AlumnoId },
                        principalTable: "Inscripciones",
                        principalColumns: new[] { "Id", "AlumnoId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCorreccionesCalificaciones_ResultadosCalificacio~",
                        columns: x => new { x.ResultadoCalificacionPeriodoId, x.InscripcionId, x.AlumnoId },
                        principalTable: "ResultadosCalificacionesPeriodos",
                        principalColumns: new[] { "Id", "InscripcionId", "AlumnoId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntentosCuestionarios_CuestionarioId_AlumnoId_NumeroIntento",
                table: "IntentosCuestionarios",
                columns: new[] { "CuestionarioId", "AlumnoId", "NumeroIntento" },
                unique: true,
                filter: "\"InscripcionId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntentosCuestionarios_CuestionarioId_InscripcionId_NumeroIn~",
                table: "IntentosCuestionarios",
                columns: new[] { "CuestionarioId", "InscripcionId", "NumeroIntento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntentosCuestionarios_InscripcionId_AlumnoId",
                table: "IntentosCuestionarios",
                columns: new[] { "InscripcionId", "AlumnoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_InscripcionId_AlumnoId",
                table: "Entregas",
                columns: new[] { "InscripcionId", "AlumnoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TareaId_AlumnoId_NumeroEnvio",
                table: "Entregas",
                columns: new[] { "TareaId", "AlumnoId", "NumeroEnvio" },
                unique: true,
                filter: "\"InscripcionId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TareaId_InscripcionId_NumeroEnvio",
                table: "Entregas",
                columns: new[] { "TareaId", "InscripcionId", "NumeroEnvio" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CategoriasEvaluacion_Tipo",
                table: "CategoriasEvaluacion",
                sql: "\"Tipo\" >= 1 AND \"Tipo\" <= 3");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesManuales_ActividadEvaluableId_AlumnoId",
                table: "CalificacionesManuales",
                columns: new[] { "ActividadEvaluableId", "AlumnoId" },
                unique: true,
                filter: "\"InscripcionId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesManuales_ActividadEvaluableId_InscripcionId",
                table: "CalificacionesManuales",
                columns: new[] { "ActividadEvaluableId", "InscripcionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesManuales_InscripcionId_AlumnoId",
                table: "CalificacionesManuales",
                columns: new[] { "InscripcionId", "AlumnoId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasEvaluacion_CreadoPorUsuarioId",
                table: "PlantillasEvaluacion",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasEvaluacion_CursoId_Nombre",
                table: "PlantillasEvaluacion",
                columns: new[] { "CursoId", "Nombre" },
                unique: true,
                filter: "\"CursoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasEvaluacion_Nombre",
                table: "PlantillasEvaluacion",
                column: "Nombre",
                unique: true,
                filter: "\"CursoId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosCalificacionesPeriodos_AlumnoId",
                table: "ResultadosCalificacionesPeriodos",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosCalificacionesPeriodos_ConfiguracionEvaluacionId_~",
                table: "ResultadosCalificacionesPeriodos",
                columns: new[] { "ConfiguracionEvaluacionId", "InscripcionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosCalificacionesPeriodos_InscripcionId_AlumnoId",
                table: "ResultadosCalificacionesPeriodos",
                columns: new[] { "InscripcionId", "AlumnoId" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccionesCalificaciones_ActividadEvaluableId",
                table: "SolicitudesCorreccionesCalificaciones",
                column: "ActividadEvaluableId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccionesCalificaciones_AlumnoId",
                table: "SolicitudesCorreccionesCalificaciones",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccionesCalificaciones_AplicadaPorUsuarioId",
                table: "SolicitudesCorreccionesCalificaciones",
                column: "AplicadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccionesCalificaciones_InscripcionId_AlumnoId",
                table: "SolicitudesCorreccionesCalificaciones",
                columns: new[] { "InscripcionId", "AlumnoId" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccionesCalificaciones_ResultadoCalificacio~1",
                table: "SolicitudesCorreccionesCalificaciones",
                columns: new[] { "ResultadoCalificacionPeriodoId", "InscripcionId", "AlumnoId" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccionesCalificaciones_ResultadoCalificacion~",
                table: "SolicitudesCorreccionesCalificaciones",
                columns: new[] { "ResultadoCalificacionPeriodoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccionesCalificaciones_RevisadaPorUsuarioId",
                table: "SolicitudesCorreccionesCalificaciones",
                column: "RevisadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCorreccionesCalificaciones_SolicitadaPorUsuarioId",
                table: "SolicitudesCorreccionesCalificaciones",
                column: "SolicitadaPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesManuales_Inscripciones_InscripcionId_AlumnoId",
                table: "CalificacionesManuales",
                columns: new[] { "InscripcionId", "AlumnoId" },
                principalTable: "Inscripciones",
                principalColumns: new[] { "Id", "AlumnoId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Entregas_Inscripciones_InscripcionId_AlumnoId",
                table: "Entregas",
                columns: new[] { "InscripcionId", "AlumnoId" },
                principalTable: "Inscripciones",
                principalColumns: new[] { "Id", "AlumnoId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntentosCuestionarios_Inscripciones_InscripcionId_AlumnoId",
                table: "IntentosCuestionarios",
                columns: new[] { "InscripcionId", "AlumnoId" },
                principalTable: "Inscripciones",
                principalColumns: new[] { "Id", "AlumnoId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "ResultadosCalificacionesPeriodos")
                        OR EXISTS (SELECT 1 FROM "SolicitudesCorreccionesCalificaciones")
                        OR EXISTS (SELECT 1 FROM "PlantillasEvaluacion")
                        OR EXISTS (SELECT 1 FROM "Entregas" WHERE "InscripcionId" IS NOT NULL)
                        OR EXISTS (SELECT 1 FROM "IntentosCuestionarios" WHERE "InscripcionId" IS NOT NULL)
                        OR EXISTS (SELECT 1 FROM "CalificacionesManuales" WHERE "InscripcionId" IS NOT NULL)
                        OR EXISTS (SELECT 1 FROM "CategoriasEvaluacion" WHERE "Tipo" <> 3)
                    THEN
                        RAISE EXCEPTION 'No se puede revertir CompletarModuloCalificaciones porque existen datos academicos que perderian integridad o trazabilidad.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesManuales_Inscripciones_InscripcionId_AlumnoId",
                table: "CalificacionesManuales");

            migrationBuilder.DropForeignKey(
                name: "FK_Entregas_Inscripciones_InscripcionId_AlumnoId",
                table: "Entregas");

            migrationBuilder.DropForeignKey(
                name: "FK_IntentosCuestionarios_Inscripciones_InscripcionId_AlumnoId",
                table: "IntentosCuestionarios");

            migrationBuilder.DropTable(
                name: "PlantillasEvaluacion");

            migrationBuilder.DropTable(
                name: "SolicitudesCorreccionesCalificaciones");

            migrationBuilder.DropTable(
                name: "ResultadosCalificacionesPeriodos");

            migrationBuilder.DropIndex(
                name: "IX_IntentosCuestionarios_CuestionarioId_AlumnoId_NumeroIntento",
                table: "IntentosCuestionarios");

            migrationBuilder.DropIndex(
                name: "IX_IntentosCuestionarios_CuestionarioId_InscripcionId_NumeroIn~",
                table: "IntentosCuestionarios");

            migrationBuilder.DropIndex(
                name: "IX_IntentosCuestionarios_InscripcionId_AlumnoId",
                table: "IntentosCuestionarios");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Inscripciones_Id_AlumnoId",
                table: "Inscripciones");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_InscripcionId_AlumnoId",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_TareaId_AlumnoId_NumeroEnvio",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_TareaId_InscripcionId_NumeroEnvio",
                table: "Entregas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CategoriasEvaluacion_Tipo",
                table: "CategoriasEvaluacion");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesManuales_ActividadEvaluableId_AlumnoId",
                table: "CalificacionesManuales");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesManuales_ActividadEvaluableId_InscripcionId",
                table: "CalificacionesManuales");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesManuales_InscripcionId_AlumnoId",
                table: "CalificacionesManuales");

            migrationBuilder.DropColumn(
                name: "InscripcionId",
                table: "IntentosCuestionarios");

            migrationBuilder.DropColumn(
                name: "InscripcionId",
                table: "Entregas");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "CategoriasEvaluacion");

            migrationBuilder.DropColumn(
                name: "InscripcionId",
                table: "CalificacionesManuales");

            migrationBuilder.CreateIndex(
                name: "IX_IntentosCuestionarios_CuestionarioId_AlumnoId_NumeroIntento",
                table: "IntentosCuestionarios",
                columns: new[] { "CuestionarioId", "AlumnoId", "NumeroIntento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_TareaId_AlumnoId_NumeroEnvio",
                table: "Entregas",
                columns: new[] { "TareaId", "AlumnoId", "NumeroEnvio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesManuales_ActividadEvaluableId_AlumnoId",
                table: "CalificacionesManuales",
                columns: new[] { "ActividadEvaluableId", "AlumnoId" },
                unique: true);
        }
    }
}
