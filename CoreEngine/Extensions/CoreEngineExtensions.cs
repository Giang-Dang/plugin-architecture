using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using CoreEngine.Abstractions;
using CoreEngine.Internal;

namespace CoreEngine.Extensions;

public static class CoreEngineExtensions
{
    /// <summary>
    /// Adds core plugin engine services to the specified service collection.
    /// </summary>
    /// <remarks>Registers the core plugin engine and registry as singletons. Call this method during
    /// application startup to enable plugin functionality.</remarks>
    /// <param name="services">The service collection to which the core engine services will be added. Cannot be null.</param>
    /// <returns>The same service collection instance with core engine services registered.</returns>
    public static IServiceCollection AddCoreEngine(this IServiceCollection services)
    {
        services.AddSingleton<PluginRegistry>();
        services.AddSingleton<IPluginEngine, PluginEngine>();
        return services;
    }

    /// <summary>
    /// Scans the specified directory for plugin assemblies and adds discovered plugins to the service collection.
    /// </summary>
    /// <remarks>This method attempts to load all .dll files in the specified directory and add plugins from
    /// each assembly. If an assembly fails to load, the error is ignored and processing continues with the remaining
    /// files. This method is typically used to enable plugin auto-discovery at application startup.</remarks>
    /// <param name="services">The service collection to which discovered plugins will be added.</param>
    /// <param name="path">The file system path to the directory containing plugin assemblies. Only files with a .dll extension are
    /// considered.</param>
    /// <returns>The original service collection with any discovered plugins added. If the directory does not exist or no plugins
    /// are found, the collection is returned unchanged.</returns>
    public static IServiceCollection AddPluginsFromPath(this IServiceCollection services, string path)
    {
        if (!System.IO.Directory.Exists(path))
        {
            return services;
        }

        var dlls = System.IO.Directory.GetFiles(path, "*.dll");
        foreach (var dll in dlls)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                services.AddPluginsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                // In real app: Log warning
                Console.WriteLine($"Failed to load plugin {dll}: {ex.Message}");
            }
        }
        return services;
    }

    /// <summary>
    /// Registers all non-abstract, non-interface types implementing the IPlugin interface from the specified assembly
    /// as singleton services in the dependency injection container.
    /// </summary>
    /// <remarks>Each discovered plugin type is registered as an IPlugin singleton, allowing them to be
    /// injected as a collection of IPlugin instances. This method does not register abstract classes or
    /// interfaces.</remarks>
    /// <param name="services">The IServiceCollection to which discovered plugin types will be registered.</param>
    /// <param name="assembly">The assembly to scan for types implementing the IPlugin interface.</param>
    /// <returns>The IServiceCollection instance with plugin types registered as singletons.</returns>
    public static IServiceCollection AddPluginsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var pluginType = typeof(IPlugin);
        var plugins = assembly.GetTypes()
            .Where(t => pluginType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var plugin in plugins)
        {
            // Register as IPlugin to be injected into Registry list
            services.AddSingleton(pluginType, plugin);
        }
        return services;
    }
}
