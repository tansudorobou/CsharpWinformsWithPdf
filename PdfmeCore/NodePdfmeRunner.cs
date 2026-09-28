using System.Diagnostics;

namespace PdfmeCore;

internal sealed class NodePdfmeRunner : IPdfmeJobRunner
{
    public async Task RunAsync(string jobJson, string outputPath, string? runnerPath,
        CancellationToken cancellationToken)
    {
        var runner = runnerPath is null ? FindRunner() : Path.GetFullPath(runnerPath);
        if (!File.Exists(runner)) throw new FileNotFoundException("PDFme runnerが見つかりません。", runner);
        var jobPath = Path.Combine(Path.GetTempPath(), $"pdfme-job-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(jobPath, jobJson, cancellationToken);
            var bundledNode = Path.Combine(Path.GetDirectoryName(runner)!, "node.exe");
            var start = new ProcessStartInfo(File.Exists(bundledNode) ? bundledNode : "node")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            start.ArgumentList.Add(runner);
            start.ArgumentList.Add(jobPath);
            start.ArgumentList.Add(Path.GetFullPath(outputPath));
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Node.jsを起動できませんでした。");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            try { await process.WaitForExitAsync(cancellationToken); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw;
            }
            var error = await stderr;
            _ = await stdout;
            if (process.ExitCode != 0) throw new InvalidOperationException($"PDF生成に失敗しました。\n{error.Trim()}");
        }
        finally
        {
            if (File.Exists(jobPath)) File.Delete(jobPath);
        }
    }

    private static string FindRunner()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "pdfme-runner", "runner.mjs");
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("pdfme-runner/runner.mjs が見つかりません。runtimeを出力フォルダーに同梱してください。");
    }
}
