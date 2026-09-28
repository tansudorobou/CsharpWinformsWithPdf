namespace PdfmeCore;

/// <summary>既存の呼び出し元向けの互換 API。新規コードでは <see cref="IReportGenerator"/> を使用します。</summary>
public static class PdfmeBridge
{
    public static Task GenerateFromFilesAsync(string layoutPath, string dataPath, string outputPath, string? runnerPath = null) =>
        PdfReportGenerator.GenerateFromFilesAsync(layoutPath, dataPath, outputPath, runnerPath);

    public static Task GenerateFromJsonAsync(string layoutPath, string valuesJson, string outputPath, string? runnerPath = null) =>
        PdfReportGenerator.GenerateFromJsonAsync(layoutPath, valuesJson, outputPath, runnerPath);

    public static Task GenerateAsync(LayoutDocument layout, string outputPath,
        IReadOnlyDictionary<string, string>? values = null, string? runnerPath = null) =>
        PdfReportGenerator.GenerateAsync(layout, outputPath, values, runnerPath);
}
