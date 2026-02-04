using System.Collections.Generic;
using System.Threading;

namespace CoreEngine.Abstractions;

// 1. Context: Input data, use Dictionary or Strongly Typed Object depending on the problem.
// Use Key-Value for flexibility (Extensibility) but allow Extension methods for type-safety.
public class PluginContext
{
    public string Capability { get; }
    public Dictionary<string, object> Parameters { get; } = new();
    public CancellationToken CancellationToken { get; }

    public PluginContext(string capability, CancellationToken ct = default)
    {
        Capability = capability;
        CancellationToken = ct;
    }
}
