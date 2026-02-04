# Detailed Guide: Building a Plugin Architecture System

This document is structured to explain every design decision in detail, from Project Setup to Performance Optimization.

---

## 1. Project Setup

**Goal**: Establish a standard Enterprise project structure to ensure Separation of Concerns.

### Specific CLI Commands

Open the terminal at the root directory and run the following commands sequentially:

```bash
# 1. Create Solution container
dotnet new sln -n PluginArchitecture

# 2. Create Core Engine (The heart of the system)
dotnet new classlib -n CoreEngine

# 3. Create Unit Tests
dotnet new xunit -n CoreEngine.Tests

# 4. Create Console App (Demo application)
dotnet new console -n PluginDemo

# 5. Link projects to Solution
dotnet sln add CoreEngine/CoreEngine.csproj
dotnet sln add CoreEngine.Tests/CoreEngine.Tests.csproj
dotnet sln add PluginDemo/PluginDemo.csproj

# 6. Establish References
# Demo App needs to use Core
dotnet add PluginDemo/PluginDemo.csproj reference CoreEngine/CoreEngine.csproj
# Test needs to test Core
dotnet add CoreEngine.Tests/CoreEngine.Tests.csproj reference CoreEngine/CoreEngine.csproj
```

**Why do we do this?**
We create 3 clear layers:

- `CoreEngine`: Contains pure business logic, no UI dependencies.
- `PluginDemo`: Where the application is assembled and run.
- `Tests`: Ensures the logic is correct.

---

## 2. Abstractions

**Goal**: Define the plugin interface.

### Interface Code

File: `CoreEngine/Abstractions/IPlugin.cs`

```csharp
public interface IPlugin {
    PluginMetadata Metadata { get; }
    Task<bool> ExecuteAsync(PluginContext context);
}
```

### Why use Interface instead of Abstract Class?

| Option             | Pros                                                              | Cons                                                                                                                                                                        | Selection     |
| ------------------ | ----------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- |
| **Abstract Class** | Can contain shared logic.                                         | **Multiple Inheritance**: C# does not allow inheriting from 2 classes. If a Plugin already inherits from another class (e.g., `DbContext`), it cannot inherit `PluginBase`. | ❌            |
| **Interface**      | Absolute flexibility. A Plugin can implement multiple interfaces. | Cannot contain implementation (unless using C# 8 Default methods).                                                                                                          | ✅ **CHOSEN** |

**Deciding Factor**: Flexibility is the #1 priority for a public framework.

---

## 3. Registry

**Goal**: Manage and lookup plugins with O(1) speed.

### Explanation: Dictionary, FrozenDictionary, and Pre-calculation

File: `CoreEngine/Internal/PluginRegistry.cs`

**Problem**: If we have to loop through plugins to find a match for every request, it will be very slow (O(N)).
**Solution**: Pre-calculation.

1.  **Phase 1 (Startup)**: Accept CPU cost to sort, group, and convert list to Dictionary.
2.  **Phase 2 (Runtime)**: Use `FrozenDictionary`.

| Option                          | Startup Speed   | Runtime Speed (Hot Path) | Thread Safety               | Selection     |
| ------------------------------- | --------------- | ------------------------ | --------------------------- | ------------- |
| **Lazy Scan (Scan on request)** | Fast            | Slow (O(N) \* Requests)  | Low                         | ❌            |
| **Normal Dictionary**           | Average         | Fast (O(1))              | Needs Lock                  | ❌            |
| **FrozenDictionary**            | Slightly Slower | **Very Fast (O(1))**     | **Thread-Safe** (Read-only) | ✅ **CHOSEN** |

**Deciding Factor**: **Performance** and **Concurrency** requirements.

---

## 4. Engine

**Goal**: Coordinate execution flow and automatic recovery (Fallback).

### Explanation: Why use `for` loop instead of `foreach` (Zero-allocation)

File: `CoreEngine/PluginEngine.cs`

```csharp
// Instead of foreach
// foreach (var plugin in plugins) { ... }

// We use for loop on Span/Memory
var plan = _registry.GetOptimizationPlan(...);
for (int i = 0; i < plan.Length; i++) {
    var plugin = plan.Span[i];
    // Execute...
}
```

**Why?**

| Option                    | Allocations (Garbage created)           | Speed                                      | Selection     |
| ------------------------- | --------------------------------------- | ------------------------------------------ | ------------- |
| **foreach (IEnumerable)** | Creates an `Enumerator` object on Heap. | Slower due to Virtual Call.                | ❌            |
| **for (Span/Array)**      | **Zero (0)** allocation.                | Direct memory access (CPU Cache friendly). | ✅ **CHOSEN** |

**Deciding Factor**: **Zero-allocation**. In High-Load systems, reducing garbage (Garbage Collection) makes the system run much smoother.

---

## 5. Dynamic Loading

**Goal**: Allow adding/removing plugins without recompiling Core App (True OCP).

### Explanation: Why separate DLLs?

**Action**: Create completely separate projects (e.g., `Plugins.PdfExport.csproj`).
File: `CoreEngine/Extensions/CoreEngineExtensions.cs` (Method `AddPluginsFromPath`).

| Option                | Method                           | Pros                                                                                             | Cons                                                                                               | Selection     |
| --------------------- | -------------------------------- | ------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------- | ------------- |
| **Project Reference** | Add Reference to Demo App        | Easy to code, IDE support.                                                                       | **High Coupling**: Every time a plugin is added, you must edit `.csproj` and Deploy the App again. | ❌            |
| **Dynamic Loading**   | Load DLL from `./plugins` folder | **Absolute Decoupling**: App doesn't know Plugin exists at compile time. Just copy the DLL file. | Loading code is more complex (Reflection).                                                         | ✅ **CHOSEN** |

**Deciding Factor**: **Maintainability** and **Deployment**. You can send a fixed `PdfExport.dll` file to a client without forcing them to reinstall the entire Server software.
