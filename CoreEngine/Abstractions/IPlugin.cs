using System.Threading.Tasks;

namespace CoreEngine.Abstractions;

// 3. Main Interface: Modules must implement this.
public interface IPlugin
{
    PluginMetadata Metadata { get; }
    
    // Returns bool to indicate if plugin executed successfully (for Fallback flow)
    Task<bool> ExecuteAsync(PluginContext context);
    
    // Quick check method to see if plugin supports this context (beyond capability)
    bool CanHandle(PluginContext context);
}
