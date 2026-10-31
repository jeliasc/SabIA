using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarComentariosAcademicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComentariosAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TareaId = table.Column<int>(type: "integer", nullable: false),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    AutorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    Contenido = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComentariosAcademicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComentariosAcademicos_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComentariosAcademicos_AspNetUsers_AutorUsuarioId",
                        column: x => x.AutorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComentariosAcademicos_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComentariosAcademicosArchivos",
                columns: table => new
                {
                    ComentarioAcademicoId = table.Column<int>(type: "integer", nullable: false),
                    ArchivoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComentariosAcademicosArchivos", x => new { x.ComentarioAcademicoId, x.ArchivoId });
                    table.ForeignKey(
                        name: "FK_ComentariosAcademicosArchivos_Archivos_ArchivoId",
                        column: x => x.ArchivoId,
                        principalTable: "Archivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComentariosAcademicosArchivos_ComentariosAcademicos_Comenta~",
                        column: x => x.ComentarioAcademicoId,
                        principalTable: "ComentariosAcademicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosAcademicos_AlumnoId",
                table: "ComentariosAcademicos",
                column: "AlumnoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosAcademicos_AutorUsuarioId",
                table: "ComentariosAcademicos",
                column: "AutorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosAcademicos_TareaId_AlumnoId_FechaCreacion_Id",
                table: "ComentariosAcademicos",
                columns: new[] { "TareaId", "AlumnoId", "FechaCreacion", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ComentariosAcademicosArchivos_ArchivoId",
                table: "ComentariosAcademicosArchivos",
                column: "ArchivoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComentariosAcademicosArchivos");

            migrationBuilder.DropTable(
                name: "ComentariosAcademicos");
        }
    }
}
