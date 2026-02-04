using System;
using System.Threading.Tasks;
using CoreEngine.Abstractions;

namespace Plugins.CsvExport;

public class CsvExportPlugin : IPlugin
{
    public PluginMetadata Metadata => new(
        Name: "CsvExport",
        Version: new Version(1, 0, 0),
        Capability: "ExportCsv",
        Priority: 10
    );

    public bool CanHandle(PluginContext context) => true;

    public async Task<bool> ExecuteAsync(PluginContext context)
    {
        Console.WriteLine($"[CsvExport from DLL] Exporting CSV data...");
        return true;
    }
}
