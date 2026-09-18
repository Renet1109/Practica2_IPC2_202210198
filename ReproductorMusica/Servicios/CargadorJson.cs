using System.Text.Json;
using ReproductorMusica.Estructuras;
using ReproductorMusica.Modelos;

namespace ReproductorMusica.Servicios;

public static class CargadorJson
{
    public static (ColaReproduccion Cola, ArbolCanciones Arbol) Cargar(string ruta)
    {
        using JsonDocument documento = JsonDocument.Parse(File.ReadAllText(ruta));
        if (documento.RootElement.ValueKind != JsonValueKind.Array)
            throw new FormatException("El JSON debe contener un arreglo de canciones: [ ... ].");

        // Se crean estructuras nuevas. Un error no reemplaza la biblioteca actual.
        var cola = new ColaReproduccion();
        var arbol = new ArbolCanciones();
        int posicion = 0;
        // JsonDocument únicamente interpreta el archivo; no gestiona nuestra cola o árbol.
        foreach (JsonElement elemento in documento.RootElement.EnumerateArray())
        {
            posicion++;
            if (elemento.ValueKind != JsonValueKind.Object)
                throw new FormatException($"La canción {posicion} debe ser un objeto JSON.");

            string titulo = LeerTexto(elemento, "titulo", posicion);
            string artista = LeerTexto(elemento, "artista", posicion);
            string genero = LeerTexto(elemento, "genero", posicion);
            if (!elemento.TryGetProperty("duracion", out JsonElement duracion)
                || duracion.ValueKind != JsonValueKind.Number
                || !duracion.TryGetDecimal(out decimal minutos) || minutos <= 0)
                throw new FormatException($"La canción {posicion} requiere 'duracion' numérica mayor que 0.");

            var cancion = new Cancion(titulo, artista, genero, minutos);
            cola.Encolar(cancion);
            arbol.Insertar(cancion);
        }
        return (cola, arbol);
    }

    private static string LeerTexto(JsonElement elemento, string campo, int posicion)
    {
        if (!elemento.TryGetProperty(campo, out JsonElement valor)
            || valor.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(valor.GetString()))
            throw new FormatException($"La canción {posicion} requiere el campo '{campo}' con texto.");
        return valor.GetString()!.Trim();
    }
}
