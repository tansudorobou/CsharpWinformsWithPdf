namespace PdfmeCore;

/// <summary>既存の呼び出し元向け。新しいコードでは <see cref="ReportValuesClassGenerator"/> を使用する。</summary>
public static class LayoutClassGenerator
{
    public static string GetOutputPath(string layoutPath) => ReportValuesClassGenerator.GetOutputPath(layoutPath);
    public static void Write(LayoutDocument layout, string layoutPath) => ReportValuesClassGenerator.Write(layout, layoutPath);
    public static string Generate(LayoutDocument layout) => ReportValuesClassGenerator.Generate(layout);
}
