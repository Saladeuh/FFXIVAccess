using System;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;
using ImGuiScene;
using Lumina.Data.Files;

namespace Mappy.System;

public class PenumbraIntegration
{
    private readonly ICallGateSubscriber<string, string> penumbraResolveDefaultSubscriber;
    private readonly ICallGateSubscriber<bool> penumbraGetEnabledState;

    public PenumbraIntegration()
    {
        penumbraResolveDefaultSubscriber = Service.PluginInterface.GetIpcSubscriber<string, string>("Penumbra.ResolveInterfacePath");
        penumbraGetEnabledState = Service.PluginInterface.GetIpcSubscriber<bool>("Penumbra.GetEnabledState");
    }

    private string ResolvePenumbraPath(string filePath)
    {
        try
        {
            return penumbraResolveDefaultSubscriber.InvokeFunc(filePath);
        }
        catch
        {
            return filePath;
        }
    }

    }
