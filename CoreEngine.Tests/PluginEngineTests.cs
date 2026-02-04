using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using CoreEngine.Abstractions;
using CoreEngine.Internal;

namespace CoreEngine.Tests;

public class PluginEngineTests
{
    private readonly ILogger<PluginEngine> _logger;
    private readonly PluginEngine _engine;
    private readonly List<IPlugin> _plugins;
    private readonly PluginRegistry _registry;

    public PluginEngineTests()
    {
        _logger = Substitute.For<ILogger<PluginEngine>>();
        _plugins = new List<IPlugin>();
        
        // Note: Registry is rebuilt in each test or we can build it here if list is final
        // But for flexibility, we might want to build it inside tests if we vary plugins.
        // However, standard xUnit per-test instantiation allows us to build it here.
    }

    private PluginEngine CreateEngine()
    {
        var registry = new PluginRegistry(_plugins);
        return new PluginEngine(registry, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteHighPriorityPluginFirst()
    {
        // Arrange
        var context = new PluginContext("Export");

        var pluginLow = Substitute.For<IPlugin>();
        pluginLow.Metadata.Returns(new PluginMetadata("LowPrio", new Version(1, 0), "Export", Priority: 1));
        pluginLow.CanHandle(context).Returns(true);
        pluginLow.ExecuteAsync(context).Returns(true);

        var pluginHigh = Substitute.For<IPlugin>();
        pluginHigh.Metadata.Returns(new PluginMetadata("HighPrio", new Version(1, 0), "Export", Priority: 100));
        pluginHigh.CanHandle(context).Returns(true);
        pluginHigh.ExecuteAsync(context).Returns(true);

        _plugins.Add(pluginLow);
        _plugins.Add(pluginHigh);

        var engine = CreateEngine();

        // Act
        await engine.ExecuteAsync(context);

        // Assert
        // High priority should run. Since it returns true (Success), Low priority should NOT run.
        await pluginHigh.Received(1).ExecuteAsync(context);
        await pluginLow.DidNotReceive().ExecuteAsync(context);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFallback_WhenHighPriorityFails()
    {
        // Arrange
        var context = new PluginContext("Export");

        var pluginHigh = Substitute.For<IPlugin>();
        pluginHigh.Metadata.Returns(new PluginMetadata("HighPrio", new Version(1, 0), "Export", Priority: 100));
        pluginHigh.CanHandle(context).Returns(true);
        // Simulate failure by throwing exception
        pluginHigh.When(x => x.ExecuteAsync(context)).Do(x => throw new Exception("Boom!"));

        var pluginLow = Substitute.For<IPlugin>();
        pluginLow.Metadata.Returns(new PluginMetadata("LowPrio", new Version(1, 0), "Export", Priority: 1));
        pluginLow.CanHandle(context).Returns(true);
        pluginLow.ExecuteAsync(context).Returns(true); // Succeeds

        _plugins.Add(pluginHigh);
        _plugins.Add(pluginLow);

        var engine = CreateEngine();

        // Act
        await engine.ExecuteAsync(context);

        // Assert
        // High fail -> caught -> Low runs
        await pluginHigh.Received(1).ExecuteAsync(context);
        await pluginLow.Received(1).ExecuteAsync(context);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkip_PluginsThatCannotHandleContext()
    {
        // Arrange
        var context = new PluginContext("Export");

        var pluginHigh = Substitute.For<IPlugin>();
        pluginHigh.Metadata.Returns(new PluginMetadata("HighPrio", new Version(1, 0), "Export", Priority: 100));
        // High priority but declines to handle
        pluginHigh.CanHandle(context).Returns(false);

        var pluginLow = Substitute.For<IPlugin>();
        pluginLow.Metadata.Returns(new PluginMetadata("LowPrio", new Version(1, 0), "Export", Priority: 1));
        pluginLow.CanHandle(context).Returns(true);
        pluginLow.ExecuteAsync(context).Returns(true);

        _plugins.Add(pluginLow);
        _plugins.Add(pluginHigh);

        var engine = CreateEngine();

        // Act
        await engine.ExecuteAsync(context);

        // Assert
        await pluginHigh.DidNotReceive().ExecuteAsync(context);
        await pluginLow.Received(1).ExecuteAsync(context);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenAllPluginsFail()
    {
        // Arrange
        var context = new PluginContext("Export");

        var plugin = Substitute.For<IPlugin>();
        plugin.Metadata.Returns(new PluginMetadata("OnlyPlugin", new Version(1, 0), "Export", Priority: 1));
        plugin.CanHandle(context).Returns(true);
        plugin.ExecuteAsync(context).Returns(Task.FromException<bool>(new Exception("Fail")));

        _plugins.Add(plugin);
        var engine = CreateEngine();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => engine.ExecuteAsync(context));
        Assert.Contains("failed or declined", ex.Message);
    }
}
