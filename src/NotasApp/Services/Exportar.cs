using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace NotasApp.Services;

/// <summary>Exportar archivos de código con el diálogo nativo de Windows "Guardar como".</summary>
public static class Exportar
{
    /// <summary>Devuelve la ruta elegida, o null si se canceló.</summary>
    public static Task<string?> ArchivoAsync(string nombre, string extension, string lenguaje, string contenido) =>
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var ext = extension.TrimStart('.');
            var dialogo = new SaveFileDialog
            {
                Title = "Exportar archivo",
                FileName = nombre,
                DefaultExt = extension,
                AddExtension = true,
                Filter = $"{lenguaje} (*.{ext})|*.{ext}|Todos los archivos (*.*)|*.*",
                OverwritePrompt = true,
            };
            if (dialogo.ShowDialog(Application.Current.MainWindow) != true) return null;
            // UTF-8 sin BOM: lo que esperan la mayoría de editores y herramientas.
            File.WriteAllText(dialogo.FileName, contenido, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return dialogo.FileName;
        }).Task;
}
