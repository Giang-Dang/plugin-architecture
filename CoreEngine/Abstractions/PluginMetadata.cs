using System;

namespace CoreEngine.Abstractions;

// 2. Metadata: Record (Immutable) to ensure thread-safety and performance.
public record PluginMetadata(
    string Name,
    Version Version,
    string Capability,
    int Priority, // Higher runs first
    bool IsDeprecated = false
);
