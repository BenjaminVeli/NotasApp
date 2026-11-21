using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotasApp.Data.Migraciones.Nube
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    lenguaje = table.Column<string>(type: "text", nullable: false),
                    extension = table.Column<string>(type: "text", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: false),
                    titulo = table.Column<string>(type: "text", nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    hora = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    completado = table.Column<bool>(type: "boolean", nullable: false),
                    favorito = table.Column<bool>(type: "boolean", nullable: false),
                    etiquetas = table.Column<string>(type: "jsonb", nullable: false),
                    posponer_hasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    eliminado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "archivos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contenido = table.Column<string>(type: "text", nullable: false),
                    favorito = table.Column<bool>(type: "boolean", nullable: false),
                    etiquetas = table.Column<string>(type: "jsonb", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    creado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    eliminado = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "categorias");
        }
    }
}
