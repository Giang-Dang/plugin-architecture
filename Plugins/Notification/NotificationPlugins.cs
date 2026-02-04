using System;
using System.Threading.Tasks;
using CoreEngine.Abstractions;

namespace Plugins.Notification;

public class BrokenNotificationPlugin : IPlugin
{
    public PluginMetadata Metadata => new(
        Name: "FastNotification",
        Version: new Version(3, 0, 0),
        Capability: "Notify",
        Priority: 100
    );

    public bool CanHandle(PluginContext context) => true;

    public async Task<bool> ExecuteAsync(PluginContext context)
    {
        Console.WriteLine($"[BrokenNotification from DLL] Attempting to send...");
        throw new Exception("Remote Service Unavailable");
    }
}

public class EmailNotificationPlugin : IPlugin
{
    public PluginMetadata Metadata => new(
        Name: "EmailNotification",
        Version: new Version(1, 0, 0),
        Capability: "Notify",
        Priority: 10
    );

    public bool CanHandle(PluginContext context) => true;

    public async Task<bool> ExecuteAsync(PluginContext context)
    {
        Console.WriteLine($"[EmailNotification from DLL] Sending email via SMTP (Stable)... done.");
        return true;
    }
}
