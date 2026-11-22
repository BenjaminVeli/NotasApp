namespace NotasApp.Services;

/// <summary>Atajos que la página actual interpreta (MainLayout los recibe de JS y los reenvía).</summary>
public static class Atajo
{
    public const string Nuevo = "nuevo";            // Ctrl+N
    public const string Guardar = "guardar";        // Ctrl+S
    public const string Exportar = "exportar";      // Ctrl+E
    public const string Copiar = "copiar";          // Ctrl+Shift+C
    public const string Favorito = "favorito";      // Ctrl+D
    public const string Completar = "completar";    // Ctrl+Enter
    public const string Eliminar = "eliminar";      // Supr
    public const string Abajo = "abajo";            // ↓
    public const string Arriba = "arriba";          // ↑
    public const string Abrir = "abrir";            // Enter
    public const string Escape = "escape";          // Esc
}

/// <summary>Reparte los atajos de teclado a la página que esté visible.</summary>
public class AtajosService
{
    public event Action<string>? Comando;

    public void Emitir(string atajo) => Comando?.Invoke(atajo);
}
