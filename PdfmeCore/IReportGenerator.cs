namespace PdfmeCore;

public interface IReportGenerator
{
    Task GenerateFromFilesAsync(string layoutPath, string dataPath, string outputPath,
        string? runnerPath = null, CancellationToken cancellationToken = default);

    Task GenerateFromJsonAsync(string layoutPath, string valuesJson, string outputPath,
        string? runnerPath = null, CancellationToken cancellationToken = default);

    Task GenerateAsync(LayoutDocument layout, string outputPath,
        IReadOnlyDictionary<string, string>? values = null, string? runnerPath = null,
        CancellationToken cancellationToken = default);
}
