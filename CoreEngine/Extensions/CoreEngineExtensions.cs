using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using CoreEngine.Abstractions;
using CoreEngine.Internal;

namespace CoreEngine.Extensions;

public static class CoreEngineExtensions
{
    public static IServiceCollection AddCoreEngine(this IServiceCollection services)
    {
        services.AddSingleton<PluginRegistry>();
        services.AddSingleton<IPluginEngine, PluginEngine>();
        return services;
    }

    // Auto-discovery feature
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

    /* Original Scan Assembly - Optimized to reuse for both scenarios */
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
