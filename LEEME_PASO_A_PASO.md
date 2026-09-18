# Práctica 2: reproductor de música

Proyecto en C# y Windows Forms con cola FIFO y árbol binario de búsqueda implementados con nodos y referencias. El código está organizado por archivo y también reunido en `CODIGO_COMPLETO.md` para leerlo o copiarlo.

## 1. Lo que solicita el PDF

| Requisito | Dónde se implementa |
| --- | --- |
| Interfaz gráfica Windows Forms, en C# | `Formularios/FormPrincipal.cs` |
| Cargar canciones desde JSON al iniciar | `Servicios/CargadorJson.cs` y evento `Shown` del formulario |
| Título, artista, género y duración | `Modelos/Cancion.cs` |
| Cola manual, primero en entrar / primero en salir | `Estructuras/NodoCola.cs` y `ColaReproduccion.cs` |
| Árbol binario de búsqueda por título | `Estructuras/NodoArbol.cs` y `ArbolCanciones.cs` |
| Extraer y mostrar la primera canción al reproducir | `ReproducirAsync()` |
| Avisar cuando la cola esté vacía | `Desencolar()` devuelve `null` y el formulario muestra un aviso |
| Sumar duración de canciones pendientes | `ColaReproduccion.TiempoTotal` |
| Mostrar ambas estructuras con Graphviz | `GeneradorDot.cs`, `ServicioGraphviz.cs` y las pestañas del formulario |

No se utilizan `Queue<T>`, `List<T>`, `LinkedList<T>`, diccionarios ni árboles nativos para guardar o administrar las canciones. `JsonDocument` interpreta el JSON y las tablas de Windows Forms presentan los datos; las estructuras principales son los nodos propios.

### Detalles del enunciado que debes tener claros

- **Reproducir:** el PDF define esta acción como extraer la primera canción y mostrar su información. Esta solución hace esa simulación; no reproduce audio ni necesita MP3.
- **Duraciones:** el PDF muestra promedios por género (Pop 3, Rock 4, Jazz 5, Clásica 8), pero también exige usar la duración de cada canción en espera. Aquí se suma `duracion` del JSON. Por ejemplo, Bohemian Rhapsody dura 6 en el ejemplo del propio PDF: se suman 6, aunque el promedio de Rock sea 4. Los promedios no sustituyen los datos ni completan campos faltantes. Si el auxiliar pretende otro uso de la tabla, consulta esa ambigüedad.
- **Fechas contradictorias:** en Entregables se indica **16 de septiembre de 2026 a las 23:59**, mientras que el Cronograma indica **17 de septiembre de 2026 a las 23:59**. Confirma en UEDI cuál fecha aplica.
- **Títulos repetidos:** se conservan todos; los iguales se insertan a la derecha en el árbol. Buscar devuelve la primera coincidencia. Puedes seleccionar cualquiera de las filas para encolarla.
- **Búsqueda:** se utiliza el título completo, ignorando mayúsculas y espacios al principio/final. No es una búsqueda por fragmentos.
- **Actualización de gráficas:** se regeneran al cargar, recargar, encolar o reproducir. El árbol permanece igual al reproducir porque representa toda la biblioteca.

## 2. Programas necesarios

1. Windows.
2. **SDK de .NET 10**. El SDK sirve para compilar, no basta con instalar únicamente el runtime. Descarga oficial: https://dotnet.microsoft.com/download/dotnet/10.0
3. **Graphviz para Windows**: https://graphviz.org/download/. Si el instalador ofrece agregarlo al PATH, selecciona esa opción. Cierra y abre la terminal después.
4. Opcional: Visual Studio compatible con .NET 10 y la carga de trabajo **Desarrollo de escritorio de .NET**. Puedes usar una terminal sin Visual Studio.

En la computadora en la que se preparó el proyecto ya se detectaron el SDK 10.0.302 y `C:\Program Files\Graphviz\bin\dot.exe`.

Comprueba en PowerShell:

```powershell
dotnet --list-sdks
& 'C:\Program Files\Graphviz\bin\dot.exe' -V
```

Si Graphviz está en el PATH también puedes usar `dot -V`. Si lo instalaste en otro directorio y no está en el PATH, establece la ruta para esa terminal antes de ejecutar:

```powershell
$env:GRAPHVIZ_DOT = 'D:\Programas\Graphviz\bin\dot.exe'
```

Reemplaza esa ruta de ejemplo por la ubicación real. La aplicación revisa `GRAPHVIZ_DOT`, luego la ubicación habitual en Program Files y finalmente el PATH.

## 3. Estructura de carpetas

El ZIP ya contiene esta estructura. **Si lo extraes, no tienes que crear las carpetas de nuevo.**

```text
practica2_reproductor/
├── LEEME_PASO_A_PASO.md
├── CODIGO_COMPLETO.md
├── iniciar.cmd
├── probar.cmd
├── .gitignore
├── ReproductorMusica/
│   ├── ReproductorMusica.csproj
│   ├── Program.cs
│   ├── Modelos/
│   │   └── Cancion.cs
│   ├── Estructuras/
│   │   ├── NodoCola.cs
│   │   ├── ColaReproduccion.cs
│   │   ├── NodoArbol.cs
│   │   └── ArbolCanciones.cs
│   ├── Servicios/
│   │   ├── CargadorJson.cs
│   │   ├── GeneradorDot.cs
│   │   └── ServicioGraphviz.cs
│   ├── Formularios/
│   │   └── FormPrincipal.cs
│   └── Datos/
│       └── canciones.json
└── Pruebas/
    ├── Pruebas.csproj
    └── Program.cs
```

`Pruebas` es un ejecutable aparte para comprobar el proyecto. No es parte de la ventana del reproductor y no requiere paquetes externos.

### Si quieres crear todo manualmente

1. Abre una terminal PowerShell en la carpeta donde guardarás tu práctica.
2. Ejecuta:

```powershell
New-Item -ItemType Directory -Path practica2_reproductor
Set-Location practica2_reproductor
New-Item -ItemType Directory -Path ReproductorMusica, Pruebas
New-Item -ItemType Directory -Path ReproductorMusica\Modelos, ReproductorMusica\Estructuras, ReproductorMusica\Servicios, ReproductorMusica\Formularios, ReproductorMusica\Datos
```

3. Abre `CODIGO_COMPLETO.md` del paquete proporcionado. Cada bloque indica su ruta y contiene el archivo completo. Crea cada archivo en su carpeta, pega el contenido y guarda como UTF-8. Asegúrate de que los archivos `.cs` o `.json` no terminen en `.txt`.
4. Usa **exactamente** los nombres de archivos y espacios de nombres del ejemplo.
5. No necesitas crear `bin`, `obj` ni `Reportes`: los generan la compilación y el programa.

## 4. Ejecutar el proyecto ya preparado

### Opción A: doble clic

Abre `iniciar.cmd`. Compila el proyecto y abre la ventana. La primera vez puede tardar unos segundos. Los errores, si los hubiera, quedan visibles en la consola.

### Opción B: PowerShell

Abre una terminal dentro de `practica2_reproductor`:

```powershell
dotnet build .\ReproductorMusica\ReproductorMusica.csproj
dotnet run --project .\ReproductorMusica\ReproductorMusica.csproj
```

La copia que se preparó está en esta ubicación:

```powershell
Set-Location 'C:\Users\willi\OneDrive\Documentos\Playground\practica2_reproductor'
dotnet run --project .\ReproductorMusica\ReproductorMusica.csproj
```

### Opción C: Visual Studio

1. Selecciona **Abrir un proyecto o una solución**.
2. Abre `ReproductorMusica/ReproductorMusica.csproj`.
3. Si Visual Studio solicita componentes de escritorio de .NET, instálalos desde Visual Studio Installer.
4. Presiona **F5** o el botón Iniciar.

**La ventana se construye por código.** No necesitas arrastrar botones ni generar `FormPrincipal.Designer.cs`. Para estudiar o modificar la interfaz, abre `FormPrincipal.cs` con **Ver código**. Si partes de una plantilla de Windows Forms, reemplaza `Program.cs` por el incluido y elimina el formulario vacío de la plantilla para no confundirte; no pegues este formulario dentro del archivo Designer.

## 5. Cómo usar la ventana

1. Al iniciar se carga automáticamente `canciones.json`.
2. La tabla izquierda muestra la biblioteca en orden alfabético gracias al recorrido inorden del árbol.
3. La tabla derecha muestra la cola en el orden del JSON.
4. Presiona **Reproducir siguiente**. Se extrae una canción de la cola y se muestran sus cuatro datos debajo de las tablas. No hay temporizador: cada clic representa reproducir la siguiente canción.
5. El tiempo pendiente se actualiza y ya no incluye la canción extraída.
6. Selecciona una canción de la biblioteca y presiona **Añadir seleccionada a cola** para volver a colocarla al final. Puedes encolar la misma varias veces.
7. Escribe un título completo y presiona **Buscar título**. La búsqueda recorre el árbol y selecciona la canción encontrada.
8. Cambia entre las pestañas **Graphviz: cola** y **Graphviz: árbol binario**.
9. **Recargar JSON** reemplaza biblioteca y cola por el contenido del archivo y reinicia la canción mostrada. No acumula una segunda copia de la carga inicial.
10. **Actualizar gráficas** permite reintentar después de instalar o configurar Graphviz.

Los botones se desactivan durante el dibujo para evitar operaciones simultáneas sobre estados diferentes. La generación tiene un límite de 20 segundos por imagen. Si Graphviz falla, se muestra el error y las operaciones de la biblioteca continúan disponibles.

## 6. JSON y archivos generados

Edita el archivo fuente:

```text
ReproductorMusica/Datos/canciones.json
```

Ejemplo mínimo válido, igual al del enunciado:

```json
[
  {
    "titulo": "Bohemian Rhapsody",
    "artista": "Queen",
    "genero": "Rock",
    "duracion": 6
  },
  {
    "titulo": "Blinding Lights",
    "artista": "The Weeknd",
    "genero": "Pop",
    "duracion": 3
  }
]
```

Reglas: los cuatro campos son obligatorios; la duración es un número positivo sin comillas; usa punto para decimales, por ejemplo `3.5`; los objetos se separan con coma; no dejes una coma después del último elemento. `[]` es una biblioteca vacía válida. No se rechazan otros géneros: el PDF no dice que los cuatro de la tabla sean los únicos permitidos.

Al compilar, el `.csproj` copia el JSON a:

```text
ReproductorMusica/bin/Debug/net10.0-windows/Datos/canciones.json
```

**El programa lee esa copia junto al ejecutable.** Para cambiar el archivo fuente, cierra la ventana, edita `ReproductorMusica/Datos/canciones.json` y vuelve a ejecutar con `iniciar.cmd` o `dotnet run`. Si quieres usar **Recargar JSON** sin cerrar la ventana, edita directamente la copia en `bin/Debug/net10.0-windows/Datos`; recuerda pasar esos cambios al archivo fuente si deseas conservarlos en el repositorio. En una compilación Release cambia `Debug` por `Release`.

Se generan automáticamente:

```text
ReproductorMusica/bin/Debug/net10.0-windows/Reportes/
├── cola.dot
├── cola.png
├── arbol.dot
└── arbol.png
```

Los `.dot` son descripciones de las estructuras; Graphviz los transforma en `.png`. Puedes abrir los PNG a tamaño completo para inspeccionar gráficas grandes.

## 7. Pruebas para tu demostración

El JSON incluido tiene **5 canciones y 25 minutos** en total. Las duraciones adicionales son datos de prueba.

| Acción | Resultado esperado |
| --- | --- |
| Iniciar | 5 en biblioteca, 5 en cola, 25 minutos pendientes |
| Ver biblioteca | Blinding Lights, Bohemian Rhapsody, Clair de Lune, Imagine, Take Five |
| Reproducir una vez | Bohemian Rhapsody; quedan 4 y 19 minutos |
| Reproducir otra vez | Blinding Lights; quedan 3 y 16 minutos |
| Reproducir tres veces más | Take Five, Clair de Lune e Imagine; cola vacía y 0 minutos |
| Reproducir estando vacía | Aviso de cola vacía |
| Buscar `imagine` | Encuentra Imagine aunque uses minúsculas y ya haya salido de la cola |
| Buscar `No existe` | Aviso de canción no encontrada |
| Encolar Imagine desde la biblioteca con cola vacía | 1 pendiente, 3 minutos, frente y final apuntan al mismo nodo |
| Recargar JSON | Se recuperan las 5 canciones y los 25 minutos iniciales |

Para las pruebas automáticas abre `probar.cmd` o ejecuta:

```powershell
dotnet run --project .\Pruebas\Pruebas.csproj
```

Se comprobaron **25 condiciones**, incluyendo estructuras vacías, FIFO, reutilización de cola, títulos repetidos, búsquedas, JSON incorrecto y PNG reales generados por Graphviz con caracteres especiales. Ambos proyectos compilaron sin errores ni advertencias. También se comprobó el inicio real de la ventana, la carga inicial y la presentación de ambas pestañas con sus gráficas. Sigue la tabla anterior para realizar tú la demostración de los botones.

## 8. Qué debes entender de cada archivo

- `Cancion.cs`: guarda la información musical. Se comparte una referencia a la misma canción entre nodos; desencolar no destruye la canción de la biblioteca.
- `NodoCola.cs`: contiene una canción y la referencia `Siguiente`.
- `ColaReproduccion.cs`: mantiene `Frente` y `Final`. Encolar conecta al final; desencolar mueve el frente al siguiente. Ambas operaciones son O(1). El total se actualiza al sumar o restar la duración.
- `NodoArbol.cs`: contiene una canción y referencias `Izquierdo` y `Derecho`.
- `ArbolCanciones.cs`: al insertar compara títulos; menores van a la izquierda y mayores o iguales a la derecha. Buscar sigue el mismo criterio. El recorrido izquierda-nodo-derecha produce el orden alfabético.
- `CargadorJson.cs`: valida todos los campos y alimenta cola y árbol, en el orden del archivo. Si el JSON es incorrecto, no reemplaza las estructuras actuales.
- `GeneradorDot.cs`: visita nodos reales y escribe nodos/aristas DOT. Usa identificadores numéricos para que títulos duplicados no se mezclen en la gráfica.
- `ServicioGraphviz.cs`: llama a `dot.exe -Tpng entrada.dot -o salida.png`, espera su finalización y revisa los errores.
- `FormPrincipal.cs`: conecta botones con operaciones y actualiza tablas, total e imágenes.
- `Program.cs`: configura Windows Forms y abre la ventana.

El ABB no se balancea: búsqueda e inserción cuestan O(h), donde h es la altura; en un árbol equilibrado h ronda log(n), pero si los títulos llegan ordenados puede llegar a n. Inorden y generación de gráficas recorren todos los nodos. La recursión de este proyecto está pensada para bibliotecas pequeñas de práctica, no para archivos enormes.

### Preguntas típicas de la evaluación

1. **¿Por qué una cola?** Porque conserva el orden de llegada, FIFO.
2. **¿Qué ocurre al sacar el último elemento?** Tanto `Frente` como `Final` quedan en `null`.
3. **¿Por qué la biblioteca no se vacía al reproducir?** Cola y árbol tienen nodos distintos; reproducir solo modifica los enlaces de la cola.
4. **¿Por qué inorden muestra títulos ordenados?** Visita primero todos los menores, luego el nodo y luego los mayores o iguales.
5. **¿Dónde ocurre la búsqueda?** En `ArbolCanciones.Buscar`, no mediante un filtro de la tabla.
6. **¿Qué dibuja Graphviz?** Los enlaces `Siguiente`, `Izquierdo` y `Derecho` del estado actual.
7. **¿Por qué el total inicial es 25?** 6 + 3 + 5 + 8 + 3, usando los datos del JSON.

## 9. Entrega

El PDF pide entregar mediante UEDI y agregar al auxiliar como colaborador del repositorio. No fija un nombre obligatorio de carpeta o repositorio, una rama, un formato de manual ni un ZIP obligatorio. Sigue cualquier instrucción adicional publicada por tu sección. El ZIP proporcionado aquí es para facilitarte los archivos.

1. Estudia, prueba y adapta el proyecto; completa tus datos donde los solicite el curso.
2. Crea un repositorio de GitHub y sube el código fuente, el `.csproj`, el JSON y la documentación. El `.gitignore` excluye `bin`, `obj` y temporales de Visual Studio.
3. En GitHub entra a **Settings → Collaborators** (el nombre puede variar según el tipo de repositorio) y agrega a tu auxiliar según esta tabla del PDF:

| Sección | Usuario |
| --- | --- |
| A | Eddy2109 |
| B | EliezerGG |
| C | DanielVelAv |
| D | javiermatg |
| N | jorgemejia25 |
| P | manuelimal25-dotcom |

4. Comprueba que el auxiliar tenga acceso e ingresa a UEDI para enviar el enlace o archivos que la actividad solicite.
5. Confirma la fecha, porque el documento contiene las dos fechas señaladas al comienzo.

No se ha publicado ni entregado nada automáticamente. El PDF exige comprender y referenciar los recursos externos; este proyecto es una base de estudio generada con asistencia de IA, no una declaración de autoría individual. Cita la ayuda según la política del curso.

## 10. Referencias

- Enunciado proporcionado: `Practica2.pdf`, 8 páginas.
- Windows Forms: https://learn.microsoft.com/dotnet/desktop/winforms/get-started/create-app-visual-studio
- JSON con System.Text.Json: https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/use-dom
- Graphviz: https://graphviz.org/download/
- Salida PNG de Graphviz: https://graphviz.org/docs/outputs/png/

## 11. Problemas frecuentes

- **`dotnet` no se reconoce:** instala el SDK de .NET 10 y abre una terminal nueva.
- **SDK incompatible / NETSDK1045:** estás usando un SDK anterior a .NET 10. Instala el SDK requerido; si trabajas en Visual Studio, actualízalo a una versión compatible.
- **No encuentra Graphviz:** revisa `dot -V`, la ruta habitual o `GRAPHVIZ_DOT`. Después presiona Actualizar gráficas.
- **No encuentra canciones.json:** revisa que exista `Datos/canciones.json` junto al ejecutable. El `.csproj` incluido lo copia al compilar.
- **Mis cambios al JSON no aparecen:** revisa la diferencia entre el archivo fuente y la copia de ejecución explicada en el paso 6.
- **Error por una duración:** usa un número positivo, sin comillas; por ejemplo `3.5`, no `"3.5"` ni `3,5`.
- **El formulario no aparece en el diseñador:** esta versión define controles por código; ejecútala para ver la ventana.
- **La gráfica de la cola cambia pero el árbol no:** es correcto al reproducir. La biblioteca conserva las canciones.
