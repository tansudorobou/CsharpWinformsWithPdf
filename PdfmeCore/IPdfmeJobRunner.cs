namespace PdfmeCore;

/// <summary>生成ジョブをPDFに変換する外部プロセスとの境界。</summary>
public interface IPdfmeJobRunner
{
    Task RunAsync(string jobJson, string outputPath, string? runnerPath,
        CancellationToken cancellationToken);
}
