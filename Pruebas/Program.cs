using System.Text;
using ReproductorMusica.Estructuras;
using ReproductorMusica.Modelos;
using ReproductorMusica.Servicios;

int verificaciones = 0;
void Verificar(bool condicion, string mensaje)
{
    if (!condicion) throw new Exception("FALLÓ: " + mensaje);
    verificaciones++;
    Console.WriteLine("OK: " + mensaje);
}

var cola = new ColaReproduccion();
var b = new Cancion("B", "Artista B", "Rock", 6);
var a = new Cancion("A", "Artista A", "Pop", 3.5m);
var c = new Cancion("C", "Artista C", "Jazz", 5);
Verificar(cola.Desencolar() is null, "Desencolar vacía devuelve null");
cola.Encolar(b); cola.Encolar(a); cola.Encolar(c);
Verificar(cola.Cantidad == 3 && cola.TiempoTotal == 14.5m, "Cantidad y suma real con decimales");
Verificar(cola.Desencolar() == b && cola.Desencolar() == a && cola.Desencolar() == c, "Orden FIFO");
Verificar(cola.Frente is null && cola.Final is null && cola.Cantidad == 0 && cola.TiempoTotal == 0, "Vaciar reinicia ambos extremos");
cola.Encolar(a);
Verificar(cola.Frente == cola.Final && cola.Desencolar() == a, "Reutilizar una cola vaciada");
var arbol = new ArbolCanciones();
arbol.Insertar(b); arbol.Insertar(c); arbol.Insertar(a);
var repetida = new Cancion("B", "Otro artista", "Pop", 2);
arbol.Insertar(repetida);
var recorrido = new StringBuilder();
arbol.RecorrerInOrden(cancion => recorrido.Append(cancion.Titulo));
Verificar(recorrido.ToString() == "ABBC" && arbol.Cantidad == 4, "Inorden y conservación de títulos repetidos");
Verificar(arbol.Buscar(" b ") == b && arbol.Buscar("X") is null, "Búsqueda normalizada y título inexistente");

string temporal = Path.Combine(Path.GetTempPath(), "practica2-pruebas-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporal);
try
{
    string archivo = Path.Combine(temporal, "canciones.json");
    var datos = CargadorJson.Cargar(Path.Combine(AppContext.BaseDirectory, "Datos", "canciones.json"));
    Verificar(datos.Cola.Cantidad == 5 && datos.Arbol.Cantidad == 5 && datos.Cola.TiempoTotal == 25, "JSON de ejemplo: 5 canciones y 25 minutos");
    Verificar(datos.Cola.Desencolar()?.Titulo == "Bohemian Rhapsody" && datos.Cola.TiempoTotal == 19,
        "Reproducir resta 6 minutos, conserva orden del JSON");
    Verificar(datos.Arbol.Buscar("Bohemian Rhapsody") is not null && datos.Arbol.Cantidad == 5,
        "Reproducir no elimina canciones de la biblioteca");
    await File.WriteAllTextAsync(archivo, "[]");
    var vacios = CargadorJson.Cargar(archivo);
    Verificar(vacios.Cola.Cantidad == 0 && vacios.Arbol.Raiz is null, "JSON vacío válido");

    void Rechazar(string json, string nombre)
    {
        File.WriteAllText(archivo, json);
        bool rechazo = false;
        try { CargadorJson.Cargar(archivo); }
        catch (Exception error) when (error is FormatException or System.Text.Json.JsonException) { rechazo = true; }
        Verificar(rechazo, nombre);
    }
    Rechazar("{}", "Rechaza raíz que no es arreglo");
    Rechazar("[", "Rechaza sintaxis JSON inválida");
    Rechazar("[null]", "Rechaza elemento que no es objeto");
    Rechazar("[{\"titulo\":\"X\"}]", "Rechaza campos faltantes");
    Rechazar("[{\"titulo\":\" \",\"artista\":\"A\",\"genero\":\"Pop\",\"duracion\":3}]", "Rechaza título vacío");
    Rechazar("[{\"titulo\":\"X\",\"artista\":\"A\",\"genero\":\"Pop\",\"duracion\":-1}]", "Rechaza duración negativa");
    Rechazar("[{\"titulo\":\"X\",\"artista\":\"A\",\"genero\":\"Pop\",\"duracion\":0}]", "Rechaza duración cero");
    Rechazar("[{\"titulo\":\"X\",\"artista\":\"A\",\"genero\":\"Pop\",\"duracion\":\"3\"}]", "Rechaza duración guardada como texto");

    var especial = new Cancion("Título \"especial\" \\ salto\nñ", "Ártista", "Clásica", 2.25m);
    datos.Cola.Encolar(especial); datos.Arbol.Insertar(especial);
    Verificar(GeneradorDot.Cola(datos.Cola).Contains("\\\"especial\\\""), "Escapa comillas en DOT");
    async Task ProbarGrafica(string contenido, string nombre)
    {
        string png = Path.Combine(temporal, nombre + ".png");
        await ServicioGraphviz.RenderizarAsync(contenido, Path.Combine(temporal, nombre + ".dot"), png);
        using var imagen = System.Drawing.Image.FromFile(png);
        Verificar(imagen.Width > 0 && imagen.Height > 0, "PNG válido: " + nombre);
    }
    await ProbarGrafica(GeneradorDot.Cola(datos.Cola), "cola-con-caracteres-especiales");
    await ProbarGrafica(GeneradorDot.Arbol(datos.Arbol), "arbol-con-caracteres-especiales");
    await ProbarGrafica(GeneradorDot.Arbol(arbol), "arbol-con-titulos-repetidos");
    await ProbarGrafica(GeneradorDot.Cola(vacios.Cola), "cola-vacia");
    await ProbarGrafica(GeneradorDot.Arbol(vacios.Arbol), "arbol-vacio");
}
finally
{
    // El directorio se creó arriba con nombre único dentro del temporal del sistema.
    Directory.Delete(temporal, recursive: true);
}
Console.WriteLine($"\n{verificaciones} verificaciones correctas.");
