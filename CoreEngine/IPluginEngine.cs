using System.Threading.Tasks;
using CoreEngine.Abstractions;

namespace CoreEngine;

public interface IPluginEngine
{
    Task ExecuteAsync(PluginContext context);
}
