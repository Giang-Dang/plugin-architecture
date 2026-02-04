using System;
using System.Threading.Tasks;
using CoreEngine.Abstractions;

namespace Plugins.PdfExport;

public class PdfExportV1 : IPlugin
{
    public PluginMetadata Metadata => new(
        Name: "PdfExport",
        Version: new Version(1, 0, 0),
        Capability: "ExportPdf",
        Priority: 10
    );

    public bool CanHandle(PluginContext context) => true;

    public async Task<bool> ExecuteAsync(PluginContext context)
    {
        Console.WriteLine($"[PdfExportV1 from DLL] Exporting PDF (Legacy Engine)... done.");
        return true;
    }
}

public class PdfExportV2 : IPlugin
{
    public PluginMetadata Metadata => new(
        Name: "PdfExport",
        Version: new Version(2, 0, 0),
        Capability: "ExportPdf",
        Priority: 20
    );

    public bool CanHandle(PluginContext context) => true;

    public async Task<bool> ExecuteAsync(PluginContext context)
    {
        Console.WriteLine($"[PdfExportV2 from DLL] Exporting PDF (Modern Fast Engine)... done.");
        return true;
    }
}
