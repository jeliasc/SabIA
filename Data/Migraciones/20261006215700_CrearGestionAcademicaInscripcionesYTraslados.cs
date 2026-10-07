using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class CrearGestionAcademicaInscripcionesYTraslados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Secciones_Id_CicloEscolarId",
                table: "Secciones",
                columns: new[] { "Id", "CicloEscolarId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Secciones_Id_GradoId",
                table: "Secciones",
                columns: new[] { "Id", "GradoId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Cursos_Id_GradoId",
                table: "Cursos",
                columns: new[] { "Id", "GradoId" });

            migrationBuilder.CreateTable(
                name: "Asignaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeccionId = table.Column<int>(type: "integer", nullable: false),
                    CursoId = table.Column<int>(type: "integer", nullable: false),
                    GradoId = table.Column<int>(type: "integer", nullable: false),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Cursos_CursoId_GradoId",
                        columns: x => new { x.CursoId, x.GradoId },
                        principalTable: "Cursos",
                        principalColumns: new[] { "Id", "GradoId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Docentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asignaciones_Secciones_SeccionId_GradoId",
                        columns: x => new { x.SeccionId, x.GradoId },
                        principalTable: "Secciones",
                        principalColumns: new[] { "Id", "GradoId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    SeccionId = table.Column<int>(type: "integer", nullable: false),
                    CicloEscolarId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscripciones", x => x.Id);
                    table.UniqueConstraint("AK_Inscripciones_Id_CicloEscolarId", x => new { x.Id, x.CicloEscolarId });
                    table.ForeignKey(
                        name: "FK_Inscripciones_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscripciones_Secciones_SeccionId_CicloEscolarId",
                        columns: x => new { x.SeccionId, x.CicloEscolarId },
                        principalTable: "Secciones",
                        principalColumns: new[] { "Id", "CicloEscolarId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialTraslados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InscripcionId = table.Column<int>(type: "integer", nullable: false),
                    CicloEscolarId = table.Column<int>(type: "integer", nullable: false),
                    SeccionOrigenId = table.Column<int>(type: "integer", nullable: false),
                    SeccionDestinoId = table.Column<int>(type: "integer", nullable: false),
                    FechaTraslado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RealizadoPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialTraslados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialTraslados_AspNetUsers_RealizadoPorUsuarioId",
                        column: x => x.RealizadoPorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialTraslados_Inscripciones_InscripcionId_CicloEscolar~",
                        columns: x => new { x.InscripcionId, x.CicloEscolarId },
                        principalTable: "Inscripciones",
                        principalColumns: new[] { "Id", "CicloEscolarId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialTraslados_Secciones_SeccionDestinoId_CicloEscolarId",
                        columns: x => new { x.SeccionDestinoId, x.CicloEscolarId },
                        principalTable: "Secciones",
                        principalColumns: new[] { "Id", "CicloEscolarId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialTraslados_Secciones_SeccionOrigenId_CicloEscolarId",
                        columns: x => new { x.SeccionOrigenId, x.CicloEscolarId },
                        principalTable: "Secciones",
                        principalColumns: new[] { "Id", "CicloEscolarId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_CursoId_GradoId",
                table: "Asignaciones",
                columns: new[] { "CursoId", "GradoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_DocenteId",
                table: "Asignaciones",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_SeccionId_CursoId",
                table: "Asignaciones",
                columns: new[] { "SeccionId", "CursoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asignaciones_SeccionId_GradoId",
                table: "Asignaciones",
                columns: new[] { "SeccionId", "GradoId" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialTraslados_InscripcionId_CicloEscolarId",
                table: "HistorialTraslados",
                columns: new[] { "InscripcionId", "CicloEscolarId" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialTraslados_InscripcionId_FechaTraslado",
                table: "HistorialTraslados",
                columns: new[] { "InscripcionId", "FechaTraslado" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialTraslados_RealizadoPorUsuarioId",
                table: "HistorialTraslados",
                column: "RealizadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialTraslados_SeccionDestinoId_CicloEscolarId",
                table: "HistorialTraslados",
                columns: new[] { "SeccionDestinoId", "CicloEscolarId" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialTraslados_SeccionOrigenId_CicloEscolarId",
                table: "HistorialTraslados",
                columns: new[] { "SeccionOrigenId", "CicloEscolarId" });

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_AlumnoId_CicloEscolarId",
                table: "Inscripciones",
                columns: new[] { "AlumnoId", "CicloEscolarId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_SeccionId",
                table: "Inscripciones",
                column: "SeccionId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscripciones_SeccionId_CicloEscolarId",
                table: "Inscripciones",
                columns: new[] { "SeccionId", "CicloEscolarId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asignaciones");

            migrationBuilder.DropTable(
                name: "HistorialTraslados");

            migrationBuilder.DropTable(
                name: "Inscripciones");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Secciones_Id_CicloEscolarId",
                table: "Secciones");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Secciones_Id_GradoId",
                table: "Secciones");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Cursos_Id_GradoId",
                table: "Cursos");
        }
    }
}
