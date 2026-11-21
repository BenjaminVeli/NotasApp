using NotasApp.Data;

namespace NotasApp.Services;

public static class Reordenar
{
    /// <summary>
    /// Mueve un elemento dentro de la lista visible y devuelve la lista completa en el nuevo
    /// orden. Con un filtro activo, los elementos ocultos conservan su lugar y los visibles
    /// ocupan sus posiciones en el orden nuevo. Devuelve null si el movimiento no aplica.
    /// </summary>
    public static List<Item>? Mover(List<Item> todos, List<Item> visibles, int desde, int hasta)
    {
        if (desde == hasta || desde < 0 || hasta < 0 || desde >= visibles.Count || hasta >= visibles.Count)
            return null;
        visibles = [.. visibles];
        var movida = visibles[desde];
        visibles.RemoveAt(desde);
        visibles.Insert(hasta, movida);

        var visiblesIds = visibles.Select(v => v.Id).ToHashSet();
        var cola = new Queue<Item>(visibles);
        return todos.Select(n => visiblesIds.Contains(n.Id) ? cola.Dequeue() : n).ToList();
    }
}
