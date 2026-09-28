namespace PdfmeCore;

public sealed class ReportGenerator : IReportGenerator
{
    private readonly IPdfmeJobRunner _runner;

    public ReportGenerator() : this(new NodePdfmeRunner()) { }

    public ReportGenerator(IPdfmeJobRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public Task GenerateFromFilesAsync(string layoutPath, string dataPath, string outputPath,
        string? runnerPath = null, CancellationToken cancellationToken = default)
    {
        var layout = LayoutStore.Load(layoutPath);
        var values = ReportValuesJsonReader.Load(dataPath);
        return GenerateAsync(layout, outputPath, values, runnerPath, cancellationToken);
    }

    public Task GenerateFromJsonAsync(string layoutPath, string valuesJson, string outputPath,
        string? runnerPath = null, CancellationToken cancellationToken = default)
    {
        var layout = LayoutStore.Load(layoutPath);
        var values = ReportValuesJsonReader.Parse(valuesJson);
        return GenerateAsync(layout, outputPath, values, runnerPath, cancellationToken);
    }

    public async Task GenerateAsync(LayoutDocument layout, string outputPath,
        IReadOnlyDictionary<string, string>? values = null, string? runnerPath = null,
        CancellationToken cancellationToken = default)
    {
        LayoutValidator.Validate(layout);
        var resolved = LayoutReferencedFiles.ResolveAndValidate(layout);
        if (resolved.BasePdfPath.Length > 0 &&
            resolved.BasePdfPath.Equals(Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("出力先には元PDFとは別のファイルを指定してください。");

        var jobJson = PdfmeJobBuilder.BuildJson(resolved, values, resolved.BasePdfPath, resolved.FontPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        await _runner.RunAsync(jobJson, outputPath, runnerPath, cancellationToken);
    }
}
