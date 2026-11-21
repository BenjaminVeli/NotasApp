using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotasApp.Data.Migraciones.Local
{
    /// <summary>
    /// Índice de búsqueda de texto completo (FTS5) sobre notas, reuniones y archivos de código.
    /// Lo mantienen triggers de SQLite, así se actualiza con cualquier cambio (incluidos los
    /// que lleguen al sincronizar con Neon) sin que el código tenga que acordarse.
    /// </summary>
    public partial class Busqueda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // remove_diacritics: "reunion" encuentra "reunión".
            migrationBuilder.Sql("""
                CREATE VIRTUAL TABLE busqueda USING fts5(
                    tipo UNINDEXED, registro_id UNINDEXED, titulo, contenido, etiquetas,
                    tokenize = 'unicode61 remove_diacritics 2');
                """);

            // Notas y reuniones (tipo 'nota' / 'reunion').
            migrationBuilder.Sql("""
                CREATE TRIGGER busqueda_items_insertar AFTER INSERT ON items WHEN new.eliminado = 0 BEGIN
                    INSERT INTO busqueda (tipo, registro_id, titulo, contenido, etiquetas)
                    VALUES (new.tipo, new.id, new.titulo, new.contenido, new.etiquetas);
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER busqueda_items_actualizar AFTER UPDATE ON items BEGIN
                    DELETE FROM busqueda WHERE registro_id = old.id;
                    INSERT INTO busqueda (tipo, registro_id, titulo, contenido, etiquetas)
                    SELECT new.tipo, new.id, new.titulo, new.contenido, new.etiquetas WHERE new.eliminado = 0;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER busqueda_items_borrar AFTER DELETE ON items BEGIN
                    DELETE FROM busqueda WHERE registro_id = old.id;
                END;
                """);

            // Archivos de código (tipo 'archivo').
            migrationBuilder.Sql("""
                CREATE TRIGGER busqueda_archivos_insertar AFTER INSERT ON archivos WHEN new.eliminado = 0 BEGIN
                    INSERT INTO busqueda (tipo, registro_id, titulo, contenido, etiquetas)
                    VALUES ('archivo', new.id, new.nombre, new.contenido, new.etiquetas);
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER busqueda_archivos_actualizar AFTER UPDATE ON archivos BEGIN
                    DELETE FROM busqueda WHERE registro_id = old.id;
                    INSERT INTO busqueda (tipo, registro_id, titulo, contenido, etiquetas)
                    SELECT 'archivo', new.id, new.nombre, new.contenido, new.etiquetas WHERE new.eliminado = 0;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER busqueda_archivos_borrar AFTER DELETE ON archivos BEGIN
                    DELETE FROM busqueda WHERE registro_id = old.id;
                END;
                """);

            // Lo que ya existía antes de esta migración.
            migrationBuilder.Sql("""
                INSERT INTO busqueda (tipo, registro_id, titulo, contenido, etiquetas)
                SELECT tipo, id, titulo, contenido, etiquetas FROM items WHERE eliminado = 0;
                """);
            migrationBuilder.Sql("""
                INSERT INTO busqueda (tipo, registro_id, titulo, contenido, etiquetas)
                SELECT 'archivo', id, nombre, contenido, etiquetas FROM archivos WHERE eliminado = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var trigger in new[]
            {
                "busqueda_items_insertar", "busqueda_items_actualizar", "busqueda_items_borrar",
                "busqueda_archivos_insertar", "busqueda_archivos_actualizar", "busqueda_archivos_borrar",
            })
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {trigger};");
            migrationBuilder.Sql("DROP TABLE IF EXISTS busqueda;");
        }
    }
}
