using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CoreEngine.Abstractions;
using CoreEngine.Internal;

namespace CoreEngine;

public class PluginEngine(PluginRegistry registry, ILogger<PluginEngine> logger) : IPluginEngine
{

    // Observability: Tracing
    /// <summary>
    /// Provides an <see cref="System.Diagnostics.ActivitySource"/> instance for creating and managing tracing
    /// activities within the CoreEngine component.
    /// </summary>
    /// <remarks>Use this activity source to start and track diagnostic activities related to CoreEngine
    /// operations. This enables distributed tracing and observability for requests and processes handled by CoreEngine.
    /// The activity source name is set to "CoreEngine" to allow consumers and monitoring tools to filter and correlate
    /// traces originating from this component.</remarks>
    private static readonly ActivitySource _activitySource = new("CoreEngine");

    /// <summary>
    /// Executes the appropriate plugin for the specified capability within the given context, using a
    /// chain-of-responsibility approach to select and invoke a suitable handler asynchronously.
    /// </summary>
    /// <remarks>Plugins are selected based on the capability specified in the context. If multiple plugins
    /// are available, each is tried in order until one successfully handles the request. Deprecated plugins may be
    /// executed, but a warning is logged. If no plugin can handle the capability, or all fail, an exception is thrown.
    /// This method is thread-safe and intended for use in asynchronous workflows.</remarks>
    /// <param name="context">The context containing the capability to be executed and any relevant data required by plugins. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous execution operation. The task completes when a plugin has successfully
    /// handled the capability or all candidates have been exhausted.</returns>
    /// <exception cref="NotSupportedException">Thrown if no plugins are registered to handle the specified capability.</exception>
    /// <exception cref="InvalidOperationException">Thrown if all candidate plugins either fail or decline to handle the capability. The inner exception contains
    /// the last error encountered, if any.</exception>
    public async Task ExecuteAsync(PluginContext context)
    {
        using var activity = _activitySource.StartActivity($"PluginEngine.Execute:{context.Capability}");
        
        // 1. Fast Lookup (O(1))
        var candidates = registry.GetOptimizationPlan(context.Capability);

        if (candidates.Length == 0)
        {
            logger.LogWarning("No plugins found for capability: {Capability}", context.Capability);
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
                logger.LogWarning("Executing DEPRECATED plugin: {Name} v{Version}", 
                    plugin.Metadata.Name, plugin.Metadata.Version);
            }

            try
            {
                // 3. Execution barrier
                logger.LogDebug("Attempting plugin: {Name}", plugin.Metadata.Name);
                
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
                logger.LogError(ex, "Plugin {Name} failed. Falling back to next candidate.", plugin.Metadata.Name);
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
