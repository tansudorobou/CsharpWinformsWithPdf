using PdfmeCore;

if (args.Length != 6)
{
    PrintUsage();
    return 2;
}

var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
for (var index = 0; index < args.Length; index += 2)
{
    if (args[index] is not ("--layout" or "--data" or "--json" or "--output") ||
        !options.TryAdd(args[index], args[index + 1]))
    {
        PrintUsage();
        return 2;
    }
}
if (!options.ContainsKey("--layout") || !options.ContainsKey("--output") ||
    (options.ContainsKey("--data") == options.ContainsKey("--json")))
{
    PrintUsage();
    return 2;
}

try
{
    var valuesJson = options.TryGetValue("--data", out var dataPath)
        ? await File.ReadAllTextAsync(dataPath)
        : options["--json"] == "-"
            ? await Console.In.ReadToEndAsync()
            : options["--json"];
    await new ReportGenerator().GenerateFromJsonAsync(options["--layout"], valuesJson, options["--output"]);
    Console.WriteLine(Path.GetFullPath(options["--output"]));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static void PrintUsage() => Console.Error.WriteLine("Usage: PdfmeCli --layout report.layout.json (--json '<json>' | --json - | --data values.json) --output output.pdf");

