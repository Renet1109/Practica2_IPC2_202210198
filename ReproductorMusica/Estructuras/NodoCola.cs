using ReproductorMusica.Modelos;

namespace ReproductorMusica.Estructuras;

public sealed class NodoCola
{
    public Cancion Cancion { get; }
    public NodoCola? Siguiente { get; internal set; }

    public NodoCola(Cancion cancion) => Cancion = cancion;
}
