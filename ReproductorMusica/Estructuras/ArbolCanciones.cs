using System.Globalization;
using ReproductorMusica.Modelos;

namespace ReproductorMusica.Estructuras;

public sealed class ArbolCanciones
{
    // La misma comparación se utiliza tanto al insertar como al buscar.
    private static readonly StringComparer Comparador =
        StringComparer.Create(CultureInfo.GetCultureInfo("es-GT"), ignoreCase: true);

    public NodoArbol? Raiz { get; private set; }
    public int Cantidad { get; private set; }

    public void Insertar(Cancion cancion)
    {
        ArgumentNullException.ThrowIfNull(cancion);
        var nuevo = new NodoArbol(cancion);
        if (Raiz is null)
        {
            Raiz = nuevo;
            Cantidad++;
            return;
        }

        NodoArbol actual = Raiz;
        while (true)
        {
            if (Comparador.Compare(cancion.Titulo, actual.Cancion.Titulo) < 0)
            {
                if (actual.Izquierdo is null) { actual.Izquierdo = nuevo; break; }
                actual = actual.Izquierdo;
            }
            else
            {
                // Se conservan títulos repetidos: los iguales se insertan a la derecha.
                if (actual.Derecho is null) { actual.Derecho = nuevo; break; }
                actual = actual.Derecho;
            }
        }
        Cantidad++;
    }

    public Cancion? Buscar(string titulo)
    {
        NodoArbol? actual = Raiz;
        while (actual is not null)
        {
            int resultado = Comparador.Compare(titulo.Trim(), actual.Cancion.Titulo);
            if (resultado == 0) return actual.Cancion;
            actual = resultado < 0 ? actual.Izquierdo : actual.Derecho;
        }
        return null;
    }

    public void RecorrerInOrden(Action<Cancion> visitar) => InOrden(Raiz, visitar);

    private static void InOrden(NodoArbol? nodo, Action<Cancion> visitar)
    {
        if (nodo is null) return;
        InOrden(nodo.Izquierdo, visitar);
        visitar(nodo.Cancion);
        InOrden(nodo.Derecho, visitar);
    }
}
