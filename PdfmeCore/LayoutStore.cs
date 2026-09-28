using System.Text.Json;

namespace PdfmeCore;

public static class LayoutStore
{
    public const string BundledFontPath = @"Fonts\MPLUS1p-Regular.ttf";
    public const string BundledFont = BundledFontPath;
    public static LayoutDocument Load(string layoutPath)
    {
        var fullPath = Path.GetFullPath(layoutPath);
        var layout = JsonSerializer.Deserialize<LayoutDocument>(File.ReadAllText(fullPath), PdfmeJsonSerializerOptions.Default)
            ?? throw new InvalidOperationException("配置ファイルを読み取れません。");
        if (layout.SchemaVersion != 1)
            throw new InvalidOperationException($"未対応の配置ファイル形式です: {layout.SchemaVersion}");
        if (layout.Fields is null) throw new InvalidOperationException("配置ファイルに項目がありません。");
        var name = string.IsNullOrWhiteSpace(layout.ReportName)
            ? Path.GetFileNameWithoutExtension(fullPath).Replace(".layout", "", StringComparison.OrdinalIgnoreCase)
            : layout.ReportName.Trim();
        var resolved = layout with
        {
            ReportName = name,
            BasePdfPath = ResolvePath(layout.BasePdfPath, Path.GetDirectoryName(fullPath)!),
            FontPath = ResolvePath(layout.FontPath, Path.GetDirectoryName(fullPath)!),
            Fields = layout.Fields.Select(field => field with
            {
                Alignment = string.IsNullOrWhiteSpace(field.Alignment) ? "left" : field.Alignment,
                VerticalAlignment = string.IsNullOrWhiteSpace(field.VerticalAlignment) ? "top" : field.VerticalAlignment,
            }).ToList(),
        };
        LayoutValidator.Validate(resolved);
        return resolved;
    }

    public static void Save(LayoutDocument layout, string layoutPath)
    {
        var json = SerializeForSave(layout, layoutPath);
        var fullPath = Path.GetFullPath(layoutPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, json);
    }

    internal static string SerializeForSave(LayoutDocument layout, string layoutPath)
    {
        LayoutValidator.Validate(layout, requireReportName: true);
        var fullPath = Path.GetFullPath(layoutPath);
        var directory = Path.GetDirectoryName(fullPath)!;
        var portable = layout with
        {
            BasePdfPath = PortablePath(ResolvePath(layout.BasePdfPath, directory), directory),
            FontPath = PortablePath(ResolvePath(layout.FontPath, directory), directory),
        };
        return JsonSerializer.Serialize(portable, PdfmeJsonSerializerOptions.Default);
    }

    public static Dictionary<string, string> LoadValues(string dataPath)
    {
        return ReportValuesJsonReader.Load(dataPath);
    }

    public static Dictionary<string, string> ParseValues(string valuesJson)
    {
        return ReportValuesJsonReader.Parse(valuesJson);
    }

    public static string ResolvePath(string path, string? layoutDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
        if (layoutDirectory is not null)
        {
            var nearLayout = Path.GetFullPath(Path.Combine(layoutDirectory, path));
            if (File.Exists(nearLayout)) return nearLayout;
        }
        var appAsset = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        if (File.Exists(appAsset)) return appAsset;
        return layoutDirectory is null ? appAsset : Path.GetFullPath(Path.Combine(layoutDirectory, path));
    }

    public static void Validate(LayoutDocument layout, bool requireReportName = false) =>
        LayoutValidator.Validate(layout, requireReportName);

    private static string PortablePath(string path, string directory)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) return path;
        var bundled = ResolvePath(BundledFontPath);
        if (Path.GetFullPath(path).Equals(bundled, StringComparison.OrdinalIgnoreCase)) return BundledFontPath;
        var relative = Path.GetRelativePath(directory, path);
        return relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            ? path
            : relative;
    }
}
