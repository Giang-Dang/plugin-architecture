using System.Threading.Tasks;
using CoreEngine.Abstractions;

namespace CoreEngine;

public interface IPluginEngine
{
    /// <summary>
    /// Executes the plugin operation asynchronously using the specified context.
    /// </summary>
    /// <param name="context">The context that provides information and services required for the plugin execution. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous execution of the plugin operation.</returns>
    Task ExecuteAsync(PluginContext context);
}
