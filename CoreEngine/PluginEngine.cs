using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CoreEngine.Abstractions;
using CoreEngine.Internal;

namespace CoreEngine;

public class PluginEngine : IPluginEngine
{
    private readonly PluginRegistry _registry;
    private readonly ILogger<PluginEngine> _logger;
    
    // Observability: Tracing
    private static readonly ActivitySource _activitySource = new("CoreEngine");

    public PluginEngine(PluginRegistry registry, ILogger<PluginEngine> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task ExecuteAsync(PluginContext context)
    {
        using var activity = _activitySource.StartActivity($"PluginEngine.Execute:{context.Capability}");
        
        // 1. Fast Lookup (O(1))
        var candidates = _registry.GetOptimizationPlan(context.Capability);

        if (candidates.Length == 0)
        {
            _logger.LogWarning("No plugins found for capability: {Capability}", context.Capability);
            throw new NotSupportedException($"Capability {context.Capability} not handled.");
        }

        Exception? lastError = null;
        bool handled = false;

        // 2. Iterate candidates (Chain of Responsibility with Fallback)
        for (int i = 0; i < candidates.Length; i++)
        {
            var plugin = candidates.Span[i];

            // Skip if plugin does not support specific context (fine-grained filter)
            if (!plugin.CanHandle(context)) continue;

            // Observability: Log deprecation warning
            if (plugin.Metadata.IsDeprecated)
            {
                _logger.LogWarning("Executing DEPRECATED plugin: {Name} v{Version}", 
                    plugin.Metadata.Name, plugin.Metadata.Version);
            }

            try
            {
                // 3. Execution barrier
                _logger.LogDebug("Attempting plugin: {Name}", plugin.Metadata.Name);
                
                var success = await plugin.ExecuteAsync(context);
                if (success)
                {
                    handled = true;
                    // Metric: Record success plugin version
                    activity?.SetTag("plugin.selected", plugin.Metadata.Name);
                    break; // Done, break chain
                }
            }
            catch (Exception ex)
            {
                // 4. Safe Fallback: Log and try the next candidate
                lastError = ex;
                _logger.LogError(ex, "Plugin {Name} failed. Falling back to next candidate.", plugin.Metadata.Name);
                activity?.AddEvent(new ActivityEvent("PluginFailure"));
            }
        }

        if (!handled)
        {
            throw new InvalidOperationException(
                $"All plugins for {context.Capability} failed or declined.", lastError);
        }
    }
}
