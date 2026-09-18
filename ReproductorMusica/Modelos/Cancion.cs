namespace ReproductorMusica.Modelos;

// Los datos no cambian después de crear la canción.
public sealed class Cancion
{
    public string Titulo { get; }
    public string Artista { get; }
    public string Genero { get; }
    public decimal Duracion { get; }

    public Cancion(string titulo, string artista, string genero, decimal duracion)
    {
        if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(artista)
            || string.IsNullOrWhiteSpace(genero))
            throw new ArgumentException("Título, artista y género son obligatorios.");
        if (duracion <= 0)
            throw new ArgumentException("La duración debe ser mayor que cero.");

        Titulo = titulo.Trim();
        Artista = artista.Trim();
        Genero = genero.Trim();
        Duracion = duracion;
    }
}
