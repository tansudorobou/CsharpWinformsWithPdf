namespace PdfmeCore;

/// <summary>レイアウトが参照するファイルを、元のレイアウトの場所を基準に確定する。</summary>
public static class LayoutReferencedFiles
{
    public static LayoutDocument ResolveAndValidate(LayoutDocument layout, string? layoutPath = null)
    {
        var directory = layoutPath is null ? null : Path.GetDirectoryName(Path.GetFullPath(layoutPath));
        var basePdfPath = LayoutStore.ResolvePath(layout.BasePdfPath, directory);
        var fontPath = LayoutStore.ResolvePath(layout.FontPath, directory);
        if (basePdfPath.Length > 0 && !File.Exists(basePdfPath))
            throw new FileNotFoundException("元PDFが見つかりません。", basePdfPath);
        if (fontPath.Length > 0 && !File.Exists(fontPath))
            throw new FileNotFoundException("フォントが見つかりません。", fontPath);
        return layout with { BasePdfPath = basePdfPath, FontPath = fontPath };
    }
}
