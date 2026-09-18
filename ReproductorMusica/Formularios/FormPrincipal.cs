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
