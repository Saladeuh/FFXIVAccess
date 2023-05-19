using System;
using System.Collections.Generic;
using System.Linq;
using Mappy.Interfaces;
using Mappy.Modules;

namespace Mappy.System;

public class ModuleManager : IDisposable
{
    private readonly List<IModule> modules = new()
    {
        new FocusLayer(),
        
            };

    public IEnumerable<IMapComponent> GetMapComponents() => modules.Select(module => module.MapComponent);

    public IEnumerable<IModuleSettings> GetModuleSettings() => modules.Select(module => module.Options).Reverse();

    public void Dispose()
    {
        // Nothing to see here, move along
    }
}
