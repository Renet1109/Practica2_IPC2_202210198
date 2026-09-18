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
