# Código completo de la práctica 2

Primero lee `LEEME_PASO_A_PASO.md`. Cada título de esta página es la ruta del archivo que debes crear dentro de `practica2_reproductor`. Los bloques contienen el código completo, sin fragmentos omitidos. Los archivos también están creados en las carpetas del proyecto.

## ReproductorMusica/ReproductorMusica.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <None Update="Datos\canciones.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

## ReproductorMusica/Program.cs

```csharp
using ReproductorMusica.Formularios;

namespace ReproductorMusica;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new FormPrincipal());
    }
}
```

## ReproductorMusica/Modelos/Cancion.cs

```csharp
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
```

## ReproductorMusica/Estructuras/NodoCola.cs

```csharp
using ReproductorMusica.Modelos;

namespace ReproductorMusica.Estructuras;

public sealed class NodoCola
{
    public Cancion Cancion { get; }
    public NodoCola? Siguiente { get; internal set; }

    public NodoCola(Cancion cancion) => Cancion = cancion;
}
```

## ReproductorMusica/Estructuras/ColaReproduccion.cs

```csharp
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
```

## ReproductorMusica/Estructuras/NodoArbol.cs

```csharp
using ReproductorMusica.Modelos;

namespace ReproductorMusica.Estructuras;

public sealed class NodoArbol
{
    public Cancion Cancion { get; }
    public NodoArbol? Izquierdo { get; internal set; }
    public NodoArbol? Derecho { get; internal set; }

    public NodoArbol(Cancion cancion) => Cancion = cancion;
}
```

## ReproductorMusica/Estructuras/ArbolCanciones.cs

```csharp
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
```

## ReproductorMusica/Servicios/CargadorJson.cs

```csharp
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
```

## ReproductorMusica/Servicios/GeneradorDot.cs

```csharp
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
```

## ReproductorMusica/Servicios/ServicioGraphviz.cs

```csharp
using System.Diagnostics;
using System.Text;

namespace ReproductorMusica.Servicios;

public static class ServicioGraphviz
{
    public static string LocalizarDot()
    {
        string? configurado = Environment.GetEnvironmentVariable("GRAPHVIZ_DOT");
        if (!string.IsNullOrWhiteSpace(configurado) && File.Exists(configurado)) return configurado;
        string normal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Graphviz", "bin", "dot.exe");
        if (File.Exists(normal)) return normal;
        return "dot"; // Si se agregó al PATH, Windows lo encuentra aquí.
    }

    public static async Task RenderizarAsync(string contenido, string rutaDot, string rutaPng)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(rutaDot))!);
        await File.WriteAllTextAsync(rutaDot, contenido, new UTF8Encoding(false));
        // No dejar una imagen anterior que pueda confundirse con el estado actual.
        if (File.Exists(rutaPng)) File.Delete(rutaPng);
        var inicio = new ProcessStartInfo(LocalizarDot())
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8
        };
        inicio.ArgumentList.Add("-Tpng");
        inicio.ArgumentList.Add(rutaDot);
        inicio.ArgumentList.Add("-o");
        inicio.ArgumentList.Add(rutaPng);
        using Process proceso = Process.Start(inicio)
            ?? throw new InvalidOperationException("No se pudo iniciar Graphviz.");
        Task<string> errores = proceso.StandardError.ReadToEndAsync();
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            await proceso.WaitForExitAsync(limite.Token);
        }
        catch (OperationCanceledException)
        {
            if (!proceso.HasExited) proceso.Kill(entireProcessTree: true);
            await proceso.WaitForExitAsync();
            await errores;
            throw new TimeoutException("Graphviz tardó más de 20 segundos.");
        }
        string detalle = await errores;
        if (proceso.ExitCode != 0 || !File.Exists(rutaPng))
            throw new InvalidOperationException("Graphviz no pudo crear la imagen. " + detalle);
    }
}
```

## ReproductorMusica/Formularios/FormPrincipal.cs

```csharp
using ReproductorMusica.Estructuras;
using ReproductorMusica.Modelos;
using ReproductorMusica.Servicios;

namespace ReproductorMusica.Formularios;

// La interfaz se construye por código: no necesita archivos Designer ni controles arrastrados.
public sealed class FormPrincipal : Form
{
    private ColaReproduccion cola = new();
    private ArbolCanciones arbol = new();
    private readonly string rutaJson = Path.Combine(AppContext.BaseDirectory, "Datos", "canciones.json");
    private readonly string reportes = Path.Combine(AppContext.BaseDirectory, "Reportes");
    private readonly DataGridView biblioteca = CrearTabla();
    private readonly DataGridView pendientes = CrearTabla();
    private readonly TextBox busqueda = new() { Width = 240, PlaceholderText = "Título completo de la canción" };
    private readonly Label actual = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label resumen = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label estado = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly PictureBox graficaCola = CrearImagen();
    private readonly PictureBox graficaArbol = CrearImagen();
    private readonly FlowLayoutPanel acciones = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private bool trabajando;

    public FormPrincipal()
    {
        Text = "Práctica 2 | Reproductor de música";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1200, 820);
        MinimumSize = new Size(1020, 740);
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.White;

        var contenedor = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, ColumnCount = 1, Padding = new Padding(14) };
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        Controls.Add(contenedor);
        contenedor.Controls.Add(new Label
        {
            Text = "Biblioteca y cola de reproducción", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 18, FontStyle.Bold)
        }, 0, 0);

        var reproducir = Boton("Reproducir siguiente", ReproducirAsync);
        var encolar = Boton("Añadir seleccionada a cola", EncolarSeleccionadaAsync);
        var recargar = Boton("Recargar JSON", CargarAsync);
        var graficar = Boton("Actualizar gráficas", ActualizarGraficasAsync);
        var buscar = new Button { Text = "Buscar título", AutoSize = true, Padding = new Padding(5) };
        buscar.Click += (_, _) => Buscar();
        acciones.Controls.Add(reproducir);
        acciones.Controls.Add(encolar);
        acciones.Controls.Add(recargar);
        acciones.Controls.Add(graficar);
        acciones.Controls.Add(busqueda);
        acciones.Controls.Add(buscar);
        busqueda.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Buscar();
        };
        contenedor.Controls.Add(acciones, 0, 1);

        var tablas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tablas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tablas.Controls.Add(Grupo("Biblioteca (orden alfabético)", biblioteca), 0, 0);
        tablas.Controls.Add(Grupo("Cola (orden de llegada)", pendientes), 1, 0);
        contenedor.Controls.Add(tablas, 0, 2);
        contenedor.Controls.Add(actual, 0, 3);
        contenedor.Controls.Add(resumen, 0, 4);

        var pestanas = new TabControl { Dock = DockStyle.Fill };
        var tabCola = new TabPage("Graphviz: cola");
        var tabArbol = new TabPage("Graphviz: árbol binario");
        tabCola.Controls.Add(graficaCola);
        tabArbol.Controls.Add(graficaArbol);
        pestanas.TabPages.Add(tabCola);
        pestanas.TabPages.Add(tabArbol);
        contenedor.Controls.Add(pestanas, 0, 5);
        contenedor.Controls.Add(estado, 0, 6);

        actual.Text = "Aún no se ha reproducido ninguna canción.";
        Shown += async (_, _) => await EjecutarAsync(CargarAsync);
        // Evita cerrar y destruir controles mientras continúa una actualización asíncrona.
        FormClosing += (_, e) => { if (trabajando) e.Cancel = true; };
        FormClosed += (_, _) =>
        {
            graficaCola.Image?.Dispose();
            graficaArbol.Image?.Dispose();
        };
    }

    private Button Boton(string texto, Func<Task> accion)
    {
        var boton = new Button { Text = texto, AutoSize = true, Padding = new Padding(5) };
        boton.Click += async (_, _) => await EjecutarAsync(accion);
        return boton;
    }

    private async Task EjecutarAsync(Func<Task> accion)
    {
        if (trabajando) return;
        trabajando = true;
        acciones.Enabled = false;
        UseWaitCursor = true;
        try { await accion(); }
        catch (Exception error)
        {
            estado.Text = "Error: " + error.Message;
            MessageBox.Show(this, error.Message, "No se pudo completar la operación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
            acciones.Enabled = true;
            trabajando = false;
        }
    }

    private async Task CargarAsync()
    {
        // Solo reemplazar los datos cuando TODO el archivo sea válido.
        var datos = CargadorJson.Cargar(rutaJson);
        cola = datos.Cola;
        arbol = datos.Arbol;
        actual.Text = "Aún no se ha reproducido ninguna canción.";
        MostrarTablas();
        await ActualizarGraficasAsync();
    }

    private async Task ReproducirAsync()
    {
        Cancion? cancion = cola.Desencolar();
        if (cancion is null)
        {
            MessageBox.Show(this, "La cola de reproducción está vacía.", "Cola vacía");
            return;
        }
        actual.Text = $"Reproduciendo: {cancion.Titulo} | {cancion.Artista} | {cancion.Genero} | {cancion.Duracion:0.##} min";
        MostrarTablas();
        await ActualizarGraficasAsync();
    }

    private async Task EncolarSeleccionadaAsync()
    {
        if (biblioteca.CurrentRow?.Tag is not Cancion cancion)
        {
            MessageBox.Show(this, "Selecciona una canción de la biblioteca.", "Seleccionar canción");
            return;
        }
        cola.Encolar(cancion);
        MostrarTablas();
        await ActualizarGraficasAsync();
    }

    private void Buscar()
    {
        if (string.IsNullOrWhiteSpace(busqueda.Text))
        {
            MessageBox.Show(this, "Escribe el título que deseas buscar.", "Buscar");
            return;
        }
        // La búsqueda ocurre en el ABB, no en las filas de la tabla.
        Cancion? encontrada = arbol.Buscar(busqueda.Text);
        if (encontrada is null)
        {
            MessageBox.Show(this, "No se encontró una canción con ese título.", "Buscar");
            return;
        }
        foreach (DataGridViewRow fila in biblioteca.Rows)
        {
            if (!ReferenceEquals(fila.Tag, encontrada)) continue;
            biblioteca.ClearSelection();
            fila.Selected = true;
            biblioteca.CurrentCell = fila.Cells[0];
            biblioteca.FirstDisplayedScrollingRowIndex = fila.Index;
            break;
        }
        MessageBox.Show(this, $"Título: {encontrada.Titulo}\nArtista: {encontrada.Artista}\n" +
            $"Género: {encontrada.Genero}\nDuración: {encontrada.Duracion:0.##} minutos", "Canción encontrada");
    }

    private void MostrarTablas()
    {
        Cancion? seleccion = biblioteca.CurrentRow?.Tag as Cancion;
        biblioteca.Rows.Clear();
        pendientes.Rows.Clear();
        arbol.RecorrerInOrden(c => AgregarFila(biblioteca, c));
        cola.Recorrer(c => AgregarFila(pendientes, c));
        if (seleccion is not null)
            foreach (DataGridViewRow fila in biblioteca.Rows)
                if (ReferenceEquals(fila.Tag, seleccion)) biblioteca.CurrentCell = fila.Cells[0];
        resumen.Text = $"Biblioteca: {arbol.Cantidad} canciones    |    En espera: {cola.Cantidad}    |    " +
            $"Tiempo total pendiente: {cola.TiempoTotal:0.##} minutos";
    }

    private async Task ActualizarGraficasAsync()
    {
        estado.Text = "Actualizando Graphviz…";
        ReemplazarImagen(graficaCola, null);
        ReemplazarImagen(graficaArbol, null);
        try
        {
            Directory.CreateDirectory(reportes);
            string dotCola = Path.Combine(reportes, "cola.dot");
            string dotArbol = Path.Combine(reportes, "arbol.dot");
            string pngCola = Path.Combine(reportes, "cola.png");
            string pngArbol = Path.Combine(reportes, "arbol.png");
            // Guardar ambas descripciones incluso si Graphviz no está instalado.
            await File.WriteAllTextAsync(dotCola, GeneradorDot.Cola(cola));
            await File.WriteAllTextAsync(dotArbol, GeneradorDot.Arbol(arbol));
            await ServicioGraphviz.RenderizarAsync(GeneradorDot.Cola(cola), dotCola, pngCola);
            await ServicioGraphviz.RenderizarAsync(GeneradorDot.Arbol(arbol), dotArbol, pngArbol);
            ReemplazarImagen(graficaCola, LeerImagen(pngCola));
            ReemplazarImagen(graficaArbol, LeerImagen(pngArbol));
            estado.Text = "Gráficas actualizadas. Archivos en: " + reportes;
        }
        catch (Exception error)
        {
            // Los controles de reproducción siguen funcionando si falla Graphviz.
            estado.Text = "No se pudieron generar las gráficas. Instala Graphviz y pulsa Actualizar gráficas.";
            MessageBox.Show(this, error.Message + "\n\nInstala Graphviz o configura GRAPHVIZ_DOT con la ruta de dot.exe.",
                "Graphviz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static Image LeerImagen(string ruta)
    {
        // Copiar la imagen permite reescribir el PNG sin bloquear el archivo.
        using Image original = Image.FromFile(ruta);
        return new Bitmap(original);
    }

    private static void ReemplazarImagen(PictureBox destino, Image? nueva)
    {
        Image? anterior = destino.Image;
        destino.Image = nueva;
        anterior?.Dispose();
    }

    private static void AgregarFila(DataGridView tabla, Cancion cancion)
    {
        int fila = tabla.Rows.Add(cancion.Titulo, cancion.Artista, cancion.Genero, cancion.Duracion);
        tabla.Rows[fila].Tag = cancion;
    }

    private static DataGridView CrearTabla()
    {
        var tabla = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, RowHeadersVisible = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White, AutoGenerateColumns = false
        };
        tabla.Columns.Add("titulo", "Título");
        tabla.Columns.Add("artista", "Artista");
        tabla.Columns.Add("genero", "Género");
        tabla.Columns.Add("duracion", "Minutos");
        tabla.Columns[0].FillWeight = 34;
        tabla.Columns[1].FillWeight = 30;
        tabla.Columns[2].FillWeight = 21;
        tabla.Columns[3].FillWeight = 15;
        foreach (DataGridViewColumn columna in tabla.Columns)
            columna.SortMode = DataGridViewColumnSortMode.NotSortable;
        return tabla;
    }

    private static PictureBox CrearImagen() => new()
    {
        Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White
    };

    private static GroupBox Grupo(string titulo, Control contenido)
    {
        var grupo = new GroupBox { Text = titulo, Dock = DockStyle.Fill, Padding = new Padding(8) };
        grupo.Controls.Add(contenido);
        return grupo;
    }
}
```

## ReproductorMusica/Datos/canciones.json

```json
[
  { "titulo": "Bohemian Rhapsody", "artista": "Queen", "genero": "Rock", "duracion": 6 },
  { "titulo": "Blinding Lights", "artista": "The Weeknd", "genero": "Pop", "duracion": 3 },
  { "titulo": "Take Five", "artista": "Dave Brubeck", "genero": "Jazz", "duracion": 5 },
  { "titulo": "Clair de Lune", "artista": "Claude Debussy", "genero": "Clásica", "duracion": 8 },
  { "titulo": "Imagine", "artista": "John Lennon", "genero": "Pop", "duracion": 3 }
]
```

## Pruebas/Pruebas.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ReproductorMusica\ReproductorMusica.csproj" />
  </ItemGroup>
</Project>
```

## Pruebas/Program.cs

```csharp
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
```

## iniciar.cmd

```bat
@echo off
cd /d "%~dp0"
dotnet run --project "ReproductorMusica\ReproductorMusica.csproj"
if errorlevel 1 pause
```

## probar.cmd

```bat
@echo off
cd /d "%~dp0"
dotnet run --project "Pruebas\Pruebas.csproj"
pause
```

## .gitignore

```text
**/bin/
**/obj/
.vs/
*.user
*.suo
**/Reportes/*.png
**/Reportes/*.dot
```
