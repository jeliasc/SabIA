using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarResolucionesAcademicasTraslado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResolucionesTrasladosAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    CicloEscolarId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false),
                    CursoId = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Vigente = table.Column<bool>(type: "boolean", nullable: false),
                    Justificacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ObservacionDecision = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreadaPorUsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecididaPorUsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    FechaDecisionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SustituyeResolucionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResolucionesTrasladosAcademicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResolucionesTrasladosAcademicos_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolucionesTrasladosAcademicos_AspNetUsers_CreadaPorUsuari~",
                        column: x => x.CreadaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolucionesTrasladosAcademicos_AspNetUsers_DecididaPorUsua~",
                        column: x => x.DecididaPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolucionesTrasladosAcademicos_CiclosEscolares_CicloEscola~",
                        column: x => x.CicloEscolarId,
                        principalTable: "CiclosEscolares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolucionesTrasladosAcademicos_Cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "Cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolucionesTrasladosAcademicos_Inscripciones_InscripcionId",
                        column: x => x.InscripcionId,
                        principalTable: "Inscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolucionesTrasladosAcademicos_Periodos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "Periodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResolucionesTrasladosAcademicos_ResolucionesTrasladosAcadem~",
                        column: x => x.SustituyeResolucionId,
                        principalTable: "ResolucionesTrasladosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuentesResolucionesTrasladosAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResolucionTrasladoAcademicoId = table.Column<int>(type: "integer", nullable: false),
                    AsignacionId = table.Column<int>(type: "integer", nullable: false),
                    HistorialTrasladoId = table.Column<int>(type: "integer", nullable: false),
                    Rol = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Evidencia = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuentesResolucionesTrasladosAcademicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuentesResolucionesTrasladosAcademicos_Asignaciones_Asignac~",
                        column: x => x.AsignacionId,
                        principalTable: "Asignaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuentesResolucionesTrasladosAcademicos_HistorialTraslados_H~",
                        column: x => x.HistorialTrasladoId,
                        principalTable: "HistorialTraslados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuentesResolucionesTrasladosAcademicos_ResolucionesTraslado~",
                        column: x => x.ResolucionTrasladoAcademicoId,
                        principalTable: "ResolucionesTrasladosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuentesResolucionesTrasladosAcademicos_AsignacionId",
                table: "FuentesResolucionesTrasladosAcademicos",
                column: "AsignacionId");

            migrationBuilder.CreateIndex(
                name: "IX_FuentesResolucionesTrasladosAcademicos_HistorialTrasladoId",
                table: "FuentesResolucionesTrasladosAcademicos",
                column: "HistorialTrasladoId");

            migrationBuilder.CreateIndex(
                name: "IX_FuentesResolucionesTrasladosAcademicos_ResolucionTrasladoAc~",
                table: "FuentesResolucionesTrasladosAcademicos",
                columns: new[] { "ResolucionTrasladoAcademicoId", "AsignacionId", "HistorialTrasladoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_AlumnoId",
                table: "ResolucionesTrasladosAcademicos",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_CicloEscolarId",
                table: "ResolucionesTrasladosAcademicos",
                column: "CicloEscolarId");

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_CreadaPorUsuarioId",
                table: "ResolucionesTrasladosAcademicos",
                column: "CreadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_CursoId",
                table: "ResolucionesTrasladosAcademicos",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_DecididaPorUsuarioId",
                table: "ResolucionesTrasladosAcademicos",
                column: "DecididaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_InscripcionId_CursoId_Peri~1",
                table: "ResolucionesTrasladosAcademicos",
                columns: new[] { "InscripcionId", "CursoId", "PeriodoId", "Vigente" },
                unique: true,
                filter: "\"Vigente\" = TRUE AND \"Estado\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_InscripcionId_CursoId_Perio~",
                table: "ResolucionesTrasladosAcademicos",
                columns: new[] { "InscripcionId", "CursoId", "PeriodoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_PeriodoId",
                table: "ResolucionesTrasladosAcademicos",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_ResolucionesTrasladosAcademicos_SustituyeResolucionId",
                table: "ResolucionesTrasladosAcademicos",
                column: "SustituyeResolucionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuentesResolucionesTrasladosAcademicos");

            migrationBuilder.DropTable(
                name: "ResolucionesTrasladosAcademicos");
        }
    }
}
