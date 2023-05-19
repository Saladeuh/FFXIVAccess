using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using Mappy.DataModels;
using Mappy.Interfaces;
using Mappy.Utilities;
using csFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace Mappy.System;

public unsafe class MapManager : IDisposable
{
  private AgentMap* MapAgent => csFramework.Instance()->GetUiModule()->GetAgentModule()->GetAgentMap();
  private TextureWrap? lastTexture;

  public List<Map> MapLayers { get; private set; } = new();
  public Map? Map { get; private set; }
  public bool PlayerInCurrentMap => MapAgent->CurrentMapId == LoadedMapId;
  public uint PlayerLocationMapID => MapAgent->CurrentMapId;
  public bool LoadingNextMap { get; private set; }
  public uint LoadedMapId { get; private set; }

  private uint lastMapId;
  private bool loadInProgress;
  private readonly Dictionary<uint, ViewportData> viewportPosition = new();

  public List<IMapComponent> MapComponents { get; }

  public MapManager()
  {
    MapComponents = Service.ModuleManager.GetMapComponents().ToList();

    Service.Framework.Update += OnFrameworkUpdate;
  }

  public void Dispose()
  {
    Service.Framework.Update -= OnFrameworkUpdate;
  }

  private void OnFrameworkUpdate(Framework framework)
  {
    if (MapAgent is null) return;
    if (Service.Condition[ConditionFlag.BetweenAreas] || Service.Condition[ConditionFlag.BetweenAreas51]) return;

    var currentMapId = MapAgent->CurrentMapId;
    if (lastMapId != currentMapId)
    {
      PluginLog.Debug($"Map ID Updated: {currentMapId}");
      LoadMap(MapAgent->CurrentMapId);

      lastMapId = currentMapId;
    }

  }


  public void LoadMap(uint mapId, Vector2? newViewportPosition = null)
  {
    if (!loadInProgress)
    {
      loadInProgress = true;
      Task.Run(() => InternalLoadMap(mapId, newViewportPosition));
    }
  }

  private void InternalLoadMap(uint mapID, Vector2? newViewportPosition)
  {
    if (LoadedMapId == mapID || mapID == uint.MaxValue)
    {
      loadInProgress = false;
      return;
    }

    PluginLog.Debug($"Loading Map: {mapID}");


    LoadedMapId = mapID;

    Map = Service.Cache.MapCache.GetRow(mapID);

    MapLayers = Service.DataManager.GetExcelSheet<Map>()!
        .Where(eachMap => eachMap.PlaceName.Row == Map.PlaceName.Row)
        .Where(eachMap => eachMap.MapIndex != 0)
        .OrderBy(eachMap => eachMap.MapIndex)
        .ToList();

    MapComponents.ForEach(component => component.Update(mapID));
    
    loadInProgress = false;
  }

}
