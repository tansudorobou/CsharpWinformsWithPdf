namespace PdfmeCore;

public sealed record LayoutFieldType(string Id, bool UsesTextFormatting);

/// <summary>レイアウトで使用できる項目種類と、そのPDFme設定上の特性。</summary>
public static class LayoutFieldTypes
{
    public const string Text = "text";
    public const string QrCode = "qrcode";
    public const string Code128 = "code128";

    public static IReadOnlyList<LayoutFieldType> All { get; } =
    [
        new(Text, UsesTextFormatting: true),
        new(QrCode, UsesTextFormatting: false),
        new(Code128, UsesTextFormatting: false),
    ];

    public static LayoutFieldType? FindById(string? id) =>
        All.FirstOrDefault(type => string.Equals(type.Id, id, StringComparison.Ordinal));

    public static LayoutFieldType? Find(string? id) => FindById(id);
}
