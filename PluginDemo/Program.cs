using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CoreEngine;
using CoreEngine.Abstractions;
using CoreEngine.Extensions;
// using PluginDemo.Plugins; // Removed direct reference

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Plugin Architecture Demo ===\n");

        // 1. Setup DI
        var services = new ServiceCollection();
        
        services.AddLogging(configure => 
        {
            // configure.AddConsole(); // Disable standard logger for clean demo output
            configure.SetMinimumLevel(LogLevel.None); 
        });

        // Register Core Engine
        services.AddCoreEngine();

        // Dynamic Loading from ./plugins folder
        var pluginsPath = System.IO.Path.Combine(AppContext.BaseDirectory, "plugins");
        Console.WriteLine($"Scanning plugins from: {pluginsPath}");
        
        services.AddPluginsFromPath(pluginsPath);

        // Verify count (optional debug)
        // var count = services.Count(s => s.ServiceType == typeof(IPlugin));
        // Console.WriteLine($"Found {count} plugins.");

        var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<IPluginEngine>();

        // 2. Scenario 1: Version Selection (Should pick PdfExportV2 - Priority 20)
        Console.WriteLine("--- Scenario 1: PDF Export (High Priority Selection) ---");
        var pdfContext = new PluginContext("ExportPdf");
        await engine.ExecuteAsync(pdfContext);
        
        // 3. Scenario 2: Different Capability (Should pick CsvExport)
        Console.WriteLine("\n--- Scenario 2: CSV Export (Capability Match) ---");
        var csvContext = new PluginContext("ExportCsv");
        await engine.ExecuteAsync(csvContext);

        // 4. Scenario 3: Robust Fallback (Broken High Priority -> Fallback Low Priority)
        Console.WriteLine("\n--- Scenario 3: Notification (Safe Fallback) ---");
        try 
        {
            // Note: BrokenNotificationPlugin (Prio 100) will fail, EmailNotificationPlugin (Prio 10) should take over
            var notifyContext = new PluginContext("Notify");
            await engine.ExecuteAsync(notifyContext);
            Console.WriteLine("Notification capability executed successfully even after partial failure!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL] System crashed: {ex.Message}");
        }

        Console.WriteLine("\n=== Demo Completed ===");
    }
}
