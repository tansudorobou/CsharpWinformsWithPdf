namespace PdfmeCore;

/// <summary>既存の静的な呼び出し元向けのAPI。</summary>
public static class PdfReportGenerator
{
    public static Task GenerateFromFilesAsync(string layoutPath, string dataPath, string outputPath,
        string? runnerPath = null) =>
        new ReportGenerator().GenerateFromFilesAsync(layoutPath, dataPath, outputPath, runnerPath);

    public static Task GenerateFromJsonAsync(string layoutPath, string valuesJson, string outputPath,
        string? runnerPath = null) =>
        new ReportGenerator().GenerateFromJsonAsync(layoutPath, valuesJson, outputPath, runnerPath);

    public static Task GenerateAsync(LayoutDocument layout, string outputPath,
        IReadOnlyDictionary<string, string>? values = null, string? runnerPath = null,
        CancellationToken cancellationToken = default) =>
        new ReportGenerator().GenerateAsync(layout, outputPath, values, runnerPath, cancellationToken);
}
