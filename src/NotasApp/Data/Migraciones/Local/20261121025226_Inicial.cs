using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotasApp.Data.Migraciones.Local
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    lenguaje = table.Column<string>(type: "TEXT", nullable: false),
                    extension = table.Column<string>(type: "TEXT", nullable: false),
                    orden = table.Column<int>(type: "INTEGER", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tipo = table.Column<string>(type: "TEXT", nullable: false),
                    titulo = table.Column<string>(type: "TEXT", nullable: false),
                    contenido = table.Column<string>(type: "TEXT", nullable: false),
                    fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    hora = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    completado = table.Column<bool>(type: "INTEGER", nullable: false),
                    favorito = table.Column<bool>(type: "INTEGER", nullable: false),
                    etiquetas = table.Column<string>(type: "TEXT", nullable: false),
                    posponer_hasta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    orden = table.Column<int>(type: "INTEGER", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_pendientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tabla = table.Column<string>(type: "TEXT", nullable: false),
                    registro_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_pendientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "archivos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", nullable: false),
                    categoria_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    contenido = table.Column<string>(type: "TEXT", nullable: false),
                    favorito = table.Column<bool>(type: "INTEGER", nullable: false),
                    etiquetas = table.Column<string>(type: "TEXT", nullable: false),
                    orden = table.Column<int>(type: "INTEGER", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    eliminado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_archivos", x => x.id);
                    table.ForeignKey(
                        name: "FK_archivos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_archivos_actualizado_en",
                table: "archivos",
                column: "actualizado_en");

            migrationBuilder.CreateIndex(
                name: "IX_archivos_categoria_id",
                table: "archivos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_actualizado_en",
                table: "categorias",
                column: "actualizado_en");

            migrationBuilder.CreateIndex(
                name: "IX_items_actualizado_en",
                table: "items",
                column: "actualizado_en");

            migrationBuilder.CreateIndex(
                name: "IX_items_tipo_fecha",
                table: "items",
                columns: new[] { "tipo", "fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "archivos");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "sync_pendientes");

            migrationBuilder.DropTable(
                name: "categorias");
        }
    }
}
