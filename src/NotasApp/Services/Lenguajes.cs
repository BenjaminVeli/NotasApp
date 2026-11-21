namespace NotasApp.Services;

/// <param name="Id">Id de lenguaje en Monaco.</param>
public record Lenguaje(string Id, string Nombre, string Extension, string Icono, string Color);

/// <summary>Lenguajes disponibles para las categorías de código.</summary>
public static class Lenguajes
{
    public static readonly IReadOnlyList<Lenguaje> Todos =
    [
        new("sql", "SQL", ".sql", "ti-database", "var(--cyan)"),
        new("css", "CSS", ".css", "ti-brand-css3", "var(--orange)"),
        new("csharp", "C#", ".cs", "ti-hash", "var(--success)"),
        new("javascript", "JavaScript", ".js", "ti-brand-javascript", "var(--yellow)"),
        new("typescript", "TypeScript", ".ts", "ti-brand-typescript", "var(--accent)"),
        new("html", "HTML", ".html", "ti-brand-html5", "var(--danger)"),
        new("json", "JSON", ".json", "ti-braces", "var(--yellow)"),
        new("python", "Python", ".py", "ti-brand-python", "var(--cyan)"),
        new("java", "Java", ".java", "ti-coffee", "var(--orange)"),
        new("xml", "XML", ".xml", "ti-file-code-2", "var(--orange)"),
        new("markdown", "Markdown", ".md", "ti-markdown", "var(--text-nav)"),
        new("powershell", "PowerShell", ".ps1", "ti-terminal-2", "var(--accent)"),
        new("shell", "Bash", ".sh", "ti-terminal", "var(--success)"),
        new("yaml", "YAML", ".yml", "ti-file-settings", "var(--purple)"),
        new("plaintext", "Texto", ".txt", "ti-file-text", "var(--text-2)"),
    ];

    public static Lenguaje Obtener(string id) =>
        Todos.FirstOrDefault(l => l.Id == id) ?? Todos[^1];
}
