# High-Performance Extensible Plugin Architecture (.NET 8)

A robust, enterprise-grade plugin architecture designed for high throughput and extensibility. This project demonstrates how to build a system that supports dynamic plugin loading, O(1) resolution speed, and safe failure handling.

## 🚀 Key Features

- **Zero-Allocation Hot Path**: Uses `FrozenDictionary` and `ReadOnlyMemory<T>` to ensure O(1) lookup and zero heap allocations during request processing.
- **Dynamic extensibility**: Plugins are decoupled into separate assemblies (DLLs) and loaded dynamically at runtime via `System.Reflection`.
- **Resilience & Safety**: Implements a Chain of Responsibility with automatic Fallback. If a high-priority plugin fails, the system seamlessly degrades to the next available option without crashing.
- **Clean API**: Simple `IPlugin` contract, following SOLID principles (ISP, DIP).

## 📂 Project Structure

- **`CoreEngine`**: The heart of the system. Contains the `PluginRegistry`, `PluginEngine`, and Abstractions.
- **`CoreEngine.Tests`**: Unit tests verifying Priority Selection and Safe Fallback using xUnit and NSubstitute.
- **`Plugins/*`**: Independent Class Library projects for demonstration (PdfExport, CsvExport, Notification).
- **`PluginDemo`**: A Console Application acting as the Host. It scans the `./plugins` folder and runs demo scenarios.

## 📖 Documentation

- [**Technical Design Document**](technical_design.md): Deep dive into the architecture, design decisions, and performance analysis.
- [**Implementation Guide**](implementation_guide.md): Step-by-step tutorial for building this system from scratch, aimed at Junior to Senior developers.

## 🛠️ Getting Started

### Prerequisites

- .NET 8.0 SDK or later.

### How to Run the Demo

1.  **Build the Solution**:

    ```bash
    dotnet build
    ```

2.  **Deploy Plugins**:
    - _Note: In a real CI/CD pipeline, this is automated. For this demo, you may need to copy the plugin DLLs to the host's plugin folder if the build script doesn't handle it automatically._
    - The `PluginDemo` app looks for plugins in `bin/Debug/netX.X/plugins`.

3.  **Run the Console App**:
    ```bash
    dotnet run --project PluginDemo/PluginDemo.csproj
    ```

### Running Tests

```bash
dotnet test
```

## 🌟 Scenarios Demonstrated

1.  **Priority Selection**: The engine automatically picks `PdfExportV2` (Priority 20) over `PdfExportV1` (Priority 10).
2.  **Capability Matching**: The engine correctly routes "ExportCsv" requests to the `CsvExportPlugin`.
3.  **Safe Fallback**: The "Notify" request first attempts `BrokenNotificationPlugin` (Priority 100). When it fails, the engine catches the error and successfully executes `EmailNotificationPlugin` (Priority 10).

---

_Built as a reference implementation for Advanced .NET Architecture._
