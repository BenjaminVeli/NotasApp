using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace NotasApp.Services;

/// <summary>Combinación de teclas para el atajo global, p. ej. Ctrl+Alt+N.</summary>
public readonly record struct Combinacion(bool Ctrl, bool Alt, bool Shift, bool Win, string Tecla, uint CodigoVirtual)
{
    public override string ToString() =>
        string.Join("+", new[] { Ctrl ? "Ctrl" : null, Alt ? "Alt" : null, Shift ? "Mayús" : null, Win ? "Win" : null, Tecla }
            .Where(p => p is not null));

    /// <summary>Desde un evento de teclado (code = "KeyN", "Digit1", "F5"). Exige Ctrl, Alt o Win.</summary>
    public static Combinacion? DesdeTeclado(string code, bool ctrl, bool alt, bool shift, bool win)
    {
        if (!(ctrl || alt || win)) return null;
        (string, uint)? tecla = code switch
        {
            _ when code.StartsWith("Key") && code.Length == 4 => (code[3..], code[3]),
            _ when code.StartsWith("Digit") && code.Length == 6 => (code[5..], code[5]),
            _ when code.StartsWith('F') && int.TryParse(code[1..], out var n) && n is >= 1 and <= 24 => (code, (uint)(0x6F + n)),
            "Space" => ("Espacio", 0x20),
            _ => null,
        };
        return tecla is { } t ? new Combinacion(ctrl, alt, shift, win, t.Item1, t.Item2) : null;
    }

    /// <summary>Lee el texto guardado en la configuración ("Ctrl+Alt+N").</summary>
    public static Combinacion? Leer(string texto)
    {
        var partes = texto.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length < 2) return null;
        var mods = partes[..^1];
        var tecla = partes[^1];
        var code = tecla switch
        {
            { Length: 1 } when char.IsLetter(tecla[0]) => "Key" + char.ToUpperInvariant(tecla[0]),
            { Length: 1 } when char.IsDigit(tecla[0]) => "Digit" + tecla,
            "Espacio" => "Space",
            _ => tecla,
        };
        return DesdeTeclado(code, mods.Contains("Ctrl"), mods.Contains("Alt"), mods.Contains("Mayús"), mods.Contains("Win"));
    }
}

/// <summary>
/// Atajo global de Windows (por defecto Ctrl+Alt+N) que muestra u oculta la app aunque esté en la
/// bandeja. Usa RegisterHotKey sobre una ventana invisible (solo mensajes), así funciona aunque la
/// ventana principal todavía no exista.
/// </summary>
public sealed class AtajoGlobalService(ConfiguracionService config) : IDisposable
{
    // En desarrollo otra combinación, para no chocar con la app instalada.
    public const string PorDefecto = Entorno.Desarrollo ? "Ctrl+Alt+Mayús+N" : "Ctrl+Alt+N";

    private const int Id = 0x4E41; // "NA"
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x1, ModCtrl = 0x2, ModShift = 0x4, ModWin = 0x8, ModNoRepeat = 0x4000;

    private HwndSource? ventana;
    private Action? alPulsar;
    private bool registrado;

    /// <summary>Combinación activa (la de la configuración, si es válida).</summary>
    public Combinacion Actual { get; private set; } = Combinacion.Leer(PorDefecto)!.Value;

    /// <summary>Mensaje si no se pudo registrar (normalmente porque otra app ya usa esa combinación).</summary>
    public string? Error { get; private set; }

    public void Iniciar(Action alPulsar)
    {
        this.alPulsar = alPulsar;
        // HWND_MESSAGE (-3): ventana que solo recibe mensajes, nunca se muestra.
        ventana = new HwndSource(new HwndSourceParameters("NotasApp.AtajoGlobal") { ParentWindow = new IntPtr(-3) });
        ventana.AddHook(ProcesarMensaje);
        var guardada = Combinacion.Leer(config.Actual.AtajoGlobal ?? PorDefecto) ?? Actual;
        Registrar(guardada);
    }

    /// <summary>Cambia el atajo. Si otra app ya lo usa, conserva el anterior y devuelve false.</summary>
    public bool Cambiar(Combinacion nueva)
    {
        var anterior = Actual;
        if (Registrar(nueva))
        {
            config.Guardar(c => c.AtajoGlobal = nueva.ToString());
            return true;
        }
        var error = Error;
        Registrar(anterior);
        Error = error;
        return false;
    }

    /// <summary>Lo suelta mientras se captura uno nuevo en Configuración (si no, ocultaría la app).</summary>
    public void Suspender()
    {
        if (registrado && ventana is not null) UnregisterHotKey(ventana.Handle, Id);
        registrado = false;
    }

    public void Reanudar()
    {
        if (!registrado) Registrar(Actual);
    }

    private bool Registrar(Combinacion c)
    {
        if (ventana is null) return false;
        Suspender();
        var mods = ModNoRepeat | (c.Ctrl ? ModCtrl : 0) | (c.Alt ? ModAlt : 0) | (c.Shift ? ModShift : 0) | (c.Win ? ModWin : 0);
        registrado = RegisterHotKey(ventana.Handle, Id, mods, c.CodigoVirtual);
        Error = registrado ? null : Marshal.GetLastWin32Error() == 1409 // ERROR_HOTKEY_ALREADY_REGISTERED
            ? $"Otra aplicación ya usa {c}. Elige otra combinación."
            : $"No se pudo registrar {c}.";
        Actual = c;
        return registrado;
    }

    private IntPtr ProcesarMensaje(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool manejado)
    {
        if (msg == WmHotkey && wParam.ToInt32() == Id)
        {
            alPulsar?.Invoke();
            manejado = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Suspender();
        ventana?.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
