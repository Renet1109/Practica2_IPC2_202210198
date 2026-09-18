using System.Text;
using ReproductorMusica.Estructuras;
using ReproductorMusica.Modelos;

namespace ReproductorMusica.Servicios;

public static class GeneradorDot
{
    // Evita que comillas, barras o saltos de línea del JSON rompan las etiquetas DOT.
    private static string Escapar(string texto) => texto.Replace("\\", "\\\\")
        .Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");

    private static string Etiqueta(Cancion cancion) =>
        $"{Escapar(cancion.Titulo)}\\n{Escapar(cancion.Artista)}\\n" +
        $"{Escapar(cancion.Genero)} · {cancion.Duracion:0.##} min";

    private static StringBuilder Inicio(string nombre, string direccion) => new(
        $"digraph {nombre} {{\nrankdir={direccion};\nbgcolor=\"white\";\n" +
        "node [shape=box, style=\"rounded,filled\", fillcolor=\"#E8F1FF\", " +
        "color=\"#4068A0\", fontname=\"Arial\"];\nedge [fontname=\"Arial\"];\n");

    public static string Cola(ColaReproduccion cola)
    {
        StringBuilder texto = Inicio("Cola", "LR");
        if (cola.Frente is null) texto.AppendLine("vacio [label=\"Cola vacía\"];");
        int id = 0;
        for (NodoCola? nodo = cola.Frente; nodo is not null; nodo = nodo.Siguiente)
        {
            string marca = id == 0 ? "FRENTE\\n" : "";
            if (nodo.Siguiente is null) marca += "FINAL\\n";
            texto.AppendLine($"n{id} [label=\"{marca}{Etiqueta(nodo.Cancion)}\"];");
            if (nodo.Siguiente is not null) texto.AppendLine($"n{id} -> n{id + 1};");
            id++;
        }
        return texto.AppendLine("}").ToString();
    }

    public static string Arbol(ArbolCanciones arbol)
    {
        StringBuilder texto = Inicio("Arbol", "TB");
        int siguienteId = 0;
        if (arbol.Raiz is null) texto.AppendLine("vacio [label=\"Biblioteca vacía\"];");
        else DibujarNodo(arbol.Raiz, texto, ref siguienteId);
        return texto.AppendLine("}").ToString();
    }

    private static int DibujarNodo(NodoArbol nodo, StringBuilder texto, ref int siguienteId)
    {
        // Identificadores únicos, incluso cuando hay títulos repetidos.
        int id = siguienteId++;
        texto.AppendLine($"n{id} [label=\"{Etiqueta(nodo.Cancion)}\"];");
        if (nodo.Izquierdo is not null)
        {
            int hijo = DibujarNodo(nodo.Izquierdo, texto, ref siguienteId);
            texto.AppendLine($"n{id} -> n{hijo} [label=\"izq\"];");
        }
        if (nodo.Derecho is not null)
        {
            int hijo = DibujarNodo(nodo.Derecho, texto, ref siguienteId);
            texto.AppendLine($"n{id} -> n{hijo} [label=\"der\"];");
        }
        return id;
    }
}
