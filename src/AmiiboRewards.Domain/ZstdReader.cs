using System.Diagnostics;

namespace AmiiboRewards.Domain;

/// <summary>Generic Zstandard bridge. The executable is deliberately isolated from game semantics.</summary>
public static class ZstdReader
{
    public static async Task DecompressAsync(string inputPath, string outputPath, string? dictionaryPath = null, CancellationToken cancellationToken = default)
    {
        var start = new ProcessStartInfo("zstd") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("-q"); start.ArgumentList.Add("-d");
        if (!string.IsNullOrEmpty(dictionaryPath)) { start.ArgumentList.Add("-D"); start.ArgumentList.Add(dictionaryPath); }
        start.ArgumentList.Add(inputPath); start.ArgumentList.Add("-o"); start.ArgumentList.Add(outputPath);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); timeout.CancelAfter(TimeSpan.FromSeconds(90));
        using var process = Process.Start(start) ?? throw new IOException("No se pudo iniciar zstd.");
        try
        {
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token); var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(timeout.Token));
            if (process.ExitCode != 0) throw new InvalidDataException($"No se pudo descomprimir el archivo Zstandard: {stderr.Result.Trim()}");
        }
        catch { if (!process.HasExited) process.Kill(true); throw; }
    }
}
