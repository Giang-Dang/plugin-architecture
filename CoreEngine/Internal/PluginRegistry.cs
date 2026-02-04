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
    /// <summary>
    /// Contains a read-only mapping of capability names to arrays of plugins, where each array is pre-sorted by
    /// priority and version in descending order.
    /// </summary>
    /// <remarks>This dictionary enables efficient lookup of plugins that support a given capability, with
    /// higher-priority and newer-version plugins appearing first in each array. The collection is immutable and cannot
    /// be modified after initialization.</remarks>
    private readonly FrozenDictionary<string, IPlugin[]> _lookup;


    /// <summary>
    /// Initializes a new instance of the PluginRegistry class using the specified collection of plugins. Organizes
    /// plugins by capability, prioritizing higher priority and newer versions for fast lookup.
    /// </summary>
    /// <remarks>Plugins are grouped by their capability, then sorted by descending priority and version. This
    /// sorting occurs only during initialization, resulting in fast, O(1) lookups for subsequent queries. The
    /// constructor does not modify the input collection.</remarks>
    /// <param name="plugins">The collection of plugins to register. Each plugin must provide valid metadata for capability, priority, and
    /// version. Cannot be null.</param>
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
    /// <summary>
    /// Retrieves a read-only sequence of plugins that support the specified capability.
    /// </summary>
    /// <remarks>If the specified capability does not exist, the returned memory region will be empty. The
    /// returned sequence reflects the current set of plugins registered for the capability at the time of the
    /// call.</remarks>
    /// <param name="capability">The capability name to search for. This value is case-sensitive and cannot be null.</param>
    /// <returns>A read-only memory region containing plugins that support the specified capability. Returns an empty region if
    /// no plugins are found for the capability.</returns>
    public ReadOnlyMemory<IPlugin> GetOptimizationPlan(string capability)
    {
        if (_lookup.TryGetValue(capability, out var plugins))
        {
            return new ReadOnlyMemory<IPlugin>(plugins);
        }
        return ReadOnlyMemory<IPlugin>.Empty;
    }
}
