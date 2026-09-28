using System.Text.Json;

namespace PdfmeCore;

public static class LayoutValidator
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.Ordinal)
    {
        "ar", "de", "en", "es", "fr", "it", "ja", "ko", "pl", "th", "zh", "zh-TW",
    };
    private static readonly HashSet<string> ReservedPdfmeOptionNames = new(StringComparer.Ordinal)
    {
        "id", "name", "type", "position", "width", "height", "content",
        "fontSize", "alignment", "verticalAlignment", "fontName",
    };

    public static void Validate(LayoutDocument layout, bool requireReportName = false)
    {
        if (layout.BasePdfPath is null || layout.FontPath is null)
            throw new InvalidOperationException("元PDFとフォントのパスを確認してください。");
        if (requireReportName && string.IsNullOrWhiteSpace(layout.ReportName))
            throw new InvalidOperationException("帳票名を入力してください。");
        if (layout.SchemaVersion != 1) throw new InvalidOperationException("未対応の配置ファイル形式です。");
        if (layout.Metadata is not null)
        {
            if (layout.Metadata.Title is null || layout.Metadata.Author is null || layout.Metadata.Subject is null ||
                layout.Metadata.Language is null || layout.Metadata.Creator is null || layout.Metadata.Producer is null ||
                layout.Metadata.Keywords?.Any(keyword => keyword is null) == true)
                throw new InvalidOperationException("PDF文書情報を確認してください。");
            if (!SupportedLanguages.Contains(layout.Metadata.Language))
                throw new InvalidOperationException($"未対応のPDF言語です: {layout.Metadata.Language}");
        }
        if (!double.IsFinite(layout.PageWidth) || !double.IsFinite(layout.PageHeight) ||
            layout.PageWidth < 1 || layout.PageHeight < 1 || layout.PageWidth > 2000 || layout.PageHeight > 2000)
            throw new InvalidOperationException("用紙サイズは1～2000 mmで指定してください。");
        ValidateFields(layout.Fields);
    }

    public static void ValidateFields(IReadOnlyList<LayoutField>? fields)
    {
        if (fields is null || fields.Count == 0)
            throw new InvalidOperationException("項目を1つ以上入力してください。");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name) || !names.Add(field.Name))
                throw new InvalidOperationException("項目名が空、または重複しています。");
            if (LayoutFieldTypes.FindById(field.Kind) is null)
                throw new InvalidOperationException($"{field.Name}: 未対応の種類です。");
            if (field.Value is null) throw new InvalidOperationException($"{field.Name}: 値を確認してください。");
            if (field.Alignment is not ("left" or "center" or "right" or "justify"))
                throw new InvalidOperationException($"{field.Name}: 横揃えを確認してください。");
            if (field.VerticalAlignment is not ("top" or "middle" or "bottom"))
                throw new InvalidOperationException($"{field.Name}: 縦揃えを確認してください。");
            if (field.PdfmeOptions is not null && field.PdfmeOptions.Any(option =>
                    string.IsNullOrWhiteSpace(option.Key) || option.Value.ValueKind == JsonValueKind.Undefined))
                throw new InvalidOperationException($"{field.Name}: PDFme詳細設定を確認してください。");
            var reservedOptions = field.PdfmeOptions?.Keys.Where(ReservedPdfmeOptionNames.Contains).ToArray();
            if (reservedOptions is { Length: > 0 })
                throw new InvalidOperationException(
                    $"{field.Name}: PDFme詳細設定に基本項目は指定できません: {string.Join(", ", reservedOptions)}");
            if (!double.IsFinite(field.X) || !double.IsFinite(field.Y) ||
                !double.IsFinite(field.Width) || !double.IsFinite(field.Height) || !double.IsFinite(field.FontSize) ||
                field.X < 0 || field.Y < 0 || field.Width <= 0 || field.Height <= 0 || field.FontSize <= 0)
                throw new InvalidOperationException($"{field.Name}: 座標・幅・高さ・文字サイズを確認してください。");
        }
    }

}
