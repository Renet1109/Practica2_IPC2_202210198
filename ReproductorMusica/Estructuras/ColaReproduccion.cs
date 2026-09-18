using ReproductorMusica.Modelos;

namespace ReproductorMusica.Estructuras;

// FIFO: el primero en entrar es el primero en salir.
public sealed class ColaReproduccion
{
    public NodoCola? Frente { get; private set; }
    public NodoCola? Final { get; private set; }
    public int Cantidad { get; private set; }
    public decimal TiempoTotal { get; private set; }

    public void Encolar(Cancion cancion)
    {
        ArgumentNullException.ThrowIfNull(cancion);
        // Calcular antes de modificar enlaces mantiene la cola intacta si hay desbordamiento.
        decimal nuevoTotal = checked(TiempoTotal + cancion.Duracion);
        var nuevo = new NodoCola(cancion);
        if (Final is null)
            Frente = nuevo;
        else
            Final.Siguiente = nuevo;
        Final = nuevo;
        Cantidad++;
        TiempoTotal = nuevoTotal;
    }

    public Cancion? Desencolar()
    {
        if (Frente is null) return null;
        Cancion cancion = Frente.Cancion;
        Frente = Frente.Siguiente;
        if (Frente is null) Final = null;
        Cantidad--;
        TiempoTotal -= cancion.Duracion;
        return cancion;
    }

    public void Recorrer(Action<Cancion> visitar)
    {
        for (NodoCola? actual = Frente; actual is not null; actual = actual.Siguiente)
            visitar(actual.Cancion);
    }
}
