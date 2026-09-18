using ReproductorMusica.Modelos;

namespace ReproductorMusica.Estructuras;

public sealed class NodoArbol
{
    public Cancion Cancion { get; }
    public NodoArbol? Izquierdo { get; internal set; }
    public NodoArbol? Derecho { get; internal set; }

    public NodoArbol(Cancion cancion) => Cancion = cancion;
}
