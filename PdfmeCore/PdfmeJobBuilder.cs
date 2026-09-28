using System.Text.Json;

namespace PdfmeCore;

internal static class PdfmeJobBuilder
{
    public static string BuildJson(LayoutDocument layout, IReadOnlyDictionary<string, string>? values,
        string basePdfPath, string fontPath)
    {
        var fieldNames = layout.Fields.Select(field => field.Name).ToHashSet(StringComparer.Ordinal);
        if (values is not null)
        {
            var unknown = values.Keys.Where(name => !fieldNames.Contains(name)).ToArray();
            if (unknown.Length > 0)
                throw new InvalidOperationException($"配置にない項目名があります: {string.Join(", ", unknown)}");
        }
        var input = layout.Fields.ToDictionary(
            field => field.Name,
            field => values is not null && values.TryGetValue(field.Name, out var value) ? value : field.Value,
            StringComparer.Ordinal);
        var schemas = layout.Fields.Select(field =>
        {
            var fieldType = LayoutFieldTypes.FindById(field.Kind)
                ?? throw new InvalidOperationException($"{field.Name}: 未対応の種類です。");
            var schema = field.PdfmeOptions?.ToDictionary(
                option => option.Key,
                option => (object)option.Value,
                StringComparer.Ordinal) ?? new Dictionary<string, object>(StringComparer.Ordinal);

            // 基本項目の値はレイアウトと呼び出し側の入力から決める。
            schema.Remove("id");
            schema.Remove("content");
            schema.Remove("fontName");
            // pdfme は readOnly 項目では inputs ではなく schema.content を描画する。
            if (field.PdfmeOptions?.TryGetValue("readOnly", out var readOnly) == true &&
                readOnly.ValueKind == JsonValueKind.True)
                schema["content"] = input[field.Name];
            schema["name"] = field.Name;
            schema["type"] = fieldType.Id;
            schema["position"] = new { x = field.X, y = field.Y };
            schema["width"] = field.Width;
            schema["height"] = field.Height;
            if (fieldType.UsesTextFormatting)
            {
                schema["fontSize"] = field.FontSize;
                schema["alignment"] = field.Alignment;
                schema["verticalAlignment"] = field.VerticalAlignment;
                if (fontPath.Length > 0) schema["fontName"] = "ReportFont";
            }
            return schema;
        }).ToArray();
        var job = new
        {
            basePdfPath,
            fontPath,
            pageWidth = layout.PageWidth,
            pageHeight = layout.PageHeight,
            metadata = new
            {
                title = string.IsNullOrWhiteSpace(layout.Metadata?.Title) ? layout.ReportName : layout.Metadata.Title.Trim(),
                author = layout.Metadata?.Author.Trim() ?? "",
                subject = layout.Metadata?.Subject.Trim() ?? "",
                keywords = layout.Metadata?.Keywords?
                    .Select(keyword => keyword.Trim())
                    .Where(keyword => keyword.Length > 0)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray() ?? [],
                lang = layout.Metadata?.Language ?? "ja",
                creator = layout.Metadata?.Creator.Trim() ?? "",
                producer = layout.Metadata?.Producer.Trim() ?? "",
            },
            template = new { schemas = new[] { schemas } },
            inputs = new[] { input },
        };
        return JsonSerializer.Serialize(job, PdfmeJsonSerializerOptions.Default);
    }
}
