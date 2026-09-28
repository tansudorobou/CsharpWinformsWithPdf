using System.Text.Json;

namespace PdfmeCore;

public static class PdfmeJsonSerializerOptions
{
    public static JsonSerializerOptions Default { get; } =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };
}

/// <summary>既存の呼び出し元向け。新しいコードでは <see cref="PdfmeJsonSerializerOptions"/> を使用する。</summary>
public static class JsonOptions
{
    public static readonly JsonSerializerOptions Pretty = PdfmeJsonSerializerOptions.Default;
}
