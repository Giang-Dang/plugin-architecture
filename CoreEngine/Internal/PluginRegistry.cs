using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Linq;
using CoreEngine.Abstractions;

namespace CoreEngine.Internal;

// Class responsible for "Discovery" and "Resolution Strategy"
public class PluginRegistry
{
    // Key: Capability Name -> Value: Pre-sorted list of plugins (Priority DESC, Version DESC)
    private readonly FrozenDictionary<string, IPlugin[]> _lookup;

    public PluginRegistry(IEnumerable<IPlugin> plugins)
    {
        // Sorting logic resides here (Startup time cost only)
        // Group by Capability -> Sort by Priority -> Sort by Version
        var builder = new Dictionary<string, List<IPlugin>>();

        foreach (var plugin in plugins)
        {
            if (!builder.ContainsKey(plugin.Metadata.Capability))
            {
                builder[plugin.Metadata.Capability] = new List<IPlugin>();
            }
            builder[plugin.Metadata.Capability].Add(plugin);
        }

        // Finalize: Sort and convert to FrozenDictionary for ultra-fast O(1) reads
        var finalLookup = builder.ToDictionary(
            k => k.Key,
            v => v.Value
                .OrderByDescending(p => p.Metadata.Priority) // Highest priority first
                .ThenByDescending(p => p.Metadata.Version)   // Newest version first
                .ToArray()
        );

        _lookup = finalLookup.ToFrozenDictionary();
    }

    // O(1) Lookup
    public ReadOnlyMemory<IPlugin> GetOptimizationPlan(string capability)
    {
        if (_lookup.TryGetValue(capability, out var plugins))
        {
            return new ReadOnlyMemory<IPlugin>(plugins);
        }
        return ReadOnlyMemory<IPlugin>.Empty;
    }
}
