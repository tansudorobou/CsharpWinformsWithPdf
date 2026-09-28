using System.Text.Json;

namespace PdfmeCore;

public sealed record LayoutField(
    string Name,
    string Kind,
    double X,
    double Y,
    double Width,
    double Height,
    double FontSize,
    string Value,
    string Alignment = "left",
    string VerticalAlignment = "top",
    Dictionary<string, JsonElement>? PdfmeOptions = null);

public sealed record PdfDocumentMetadata(
    string Title = "",
    string Author = "",
    string Subject = "",
    List<string>? Keywords = null,
    string Language = "ja",
    string Creator = "",
    string Producer = "");

public sealed record LayoutDocument(
    string BasePdfPath,
    string FontPath,
    List<LayoutField> Fields,
    double PageWidth = 210,
    double PageHeight = 297,
    string ReportName = "",
    int SchemaVersion = 1,
    PdfDocumentMetadata? Metadata = null);
