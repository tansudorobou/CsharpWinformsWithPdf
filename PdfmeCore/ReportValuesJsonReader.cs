using System.Text.Json;

namespace PdfmeCore;

public static class ReportValuesJsonReader
{
    public static Dictionary<string, string> Load(string path) => Parse(File.ReadAllText(path));

    public static Dictionary<string, string> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("値JSONは項目名をキーとするオブジェクトにしてください。");
        if (root.EnumerateObject().Count() == 1 &&
            root.TryGetProperty("values", out var nested) && nested.ValueKind == JsonValueKind.Object)
            root = nested;

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            values[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? "",
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.Value.ToString(),
                JsonValueKind.Null => "",
                _ => throw new InvalidOperationException($"{property.Name}: 値は文字列・数値・真偽値・nullにしてください。"),
            };
        }
        return values;
    }
}
