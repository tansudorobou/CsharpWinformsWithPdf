namespace PdfmeCore;

/// <summary>既存の呼び出し元向け。新しいコードでは <see cref="ReportValuesJsonReader"/> を使用する。</summary>
public static class ReportValues
{
    public static Dictionary<string, string> Load(string path) => ReportValuesJsonReader.Load(path);
    public static Dictionary<string, string> Parse(string json) => ReportValuesJsonReader.Parse(json);
}
