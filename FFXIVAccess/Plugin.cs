using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.ContextMenu;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVAccess.Windows;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Mappy;
using Mappy.System;
using Dalamud.Plugin.Services;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Events;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonRelicNoteBook;

namespace FFXIVAccess;
public unsafe sealed partial class Plugin : IDalamudPlugin
{
  public string Name => "FFXIVAccess";
  public string Version => "0.0.0";
  public static Lumina.Excel.ExcelSheet<CustomQuestSheet> questList;
  private Lumina.Excel.ExcelSheet<Item> itemList;
  private DalamudPluginInterface PluginInterface { get; init; }
  private ICommandManager CommandManager { get; init; }
  private IDataManager dataManager { get; init; }
  public IAddonLifecycle addonLifeCycle { get; }
  public IAddonEventManager AddonEventManager { get; }
  public IFramework framework { get; set; }
  private IFlyTextGui flyTextGui { get; init; }
  public IKeyState keyState { get; private set; }
  private IObjectTable gameObjects { get; init; }
  public IGameGui gameGui { get; private set; }


  public SeStringBuilder seStringBuilder { get; private set; }
  private ITitleScreenMenu titleScreenMenu { get; set; }
  public IClientState clientState { get; private set; }
  private IToastGui toastGui { get; set; }
  public QuestManager questManager = null!;
  public ITargetManager targetManager = null;
  public Configuration Configuration { get; init; }
  public WindowSystem WindowSystem = new("SamplePlugin");
  [PluginService] public static IChatGui Chat { get; set; } = null!;

  private ConfigWindow ConfigWindow { get; init; }
  public Dictionary<VirtualKey, Action<string, string>> keyActions { get; }
  private MainWindow MainWindow { get; init; }
  public SoundSystem soundSystem { get; private set; }

  public event Action<nint, string> NewAddonOpenedEvent;
  public event Action<AtkResNode?> NodeFocusChangedEvent;
  public Plugin(
    [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
    IChatGui chat,
    IClientState clientState,
    [RequiredVersion("1.0")] ICommandManager commandManager,
    IFramework framework,
    IFlyTextGui flyTextGui,
    IGameGui gameGui,
    IKeyState keyState,
    IToastGui toastGui,
    ITitleScreenMenu titleScreenMenu,
    IObjectTable gameObjects,
    IDataManager dataManager,
    ITargetManager targetManager,
  IAddonLifecycle addonLifeCycle,
  IAddonEventManager addonEventManager
    )
  {
    ScreenReader.Load(pluginInterface.InternalName, Version);
    ScreenReader.Output("Screen Reader ready");
    // Mappy services
    PluginInterface = pluginInterface;
    pluginInterface.Create<Service>();
    Service.Cache = new CompositeLuminaCache();
    Service.ModuleManager = new ModuleManager();
    Service.QuestManager = new QuestManager();
    Service.MapManager = new MapManager();
    CommandManager = commandManager;
    this.titleScreenMenu = titleScreenMenu;
    this.clientState = clientState;
    this.dataManager = dataManager;
    this.framework = framework;
    this.gameObjects = gameObjects;
    this.gameGui = gameGui;
    this.targetManager = targetManager;
    this.addonLifeCycle = addonLifeCycle;
    AddonEventManager = addonEventManager;
    this.questManager = new QuestManager();
    this.toastGui = toastGui;
    Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    Configuration.Initialize(PluginInterface);
    //chat.ChatMessage += onChat;
    itemList = dataManager.GetExcelSheet<Item>();
    questList = dataManager.GetExcelSheet<CustomQuestSheet>();
    var contextMenu = new DalamudContextMenu(pluginInterface);
    contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;
    contextMenu.OnOpenInventoryContextMenu += OpenInventory;
    framework.Update += OnFrameworkUpdate;
    clientState.TerritoryChanged += onTerritoryChanged;
    flyTextGui.FlyTextCreated += onFlyTextCreated;
    gameGui.HoveredActionChanged += onHoveredActionChanged;
    gameGui.HoveredItemChanged += onHoveredItemChange;
    //addonLifeCycle.RegisterListener(AddonEvent.PreFinalize, OnPostRefresh);
    addonLifeCycle.RegisterListener(AddonEvent.PostSetup, OnPostRefresh);
    //addonLifeCycle.RegisterListener(AddonEvent.PostSetup, addonDict.Keys, OnPostSetup);
    this.keyState = keyState;
    toastGui.Toast += onToast;
    toastGui.ErrorToast += onErrorToast;
    toastGui.QuestToast += onQuestToast;
    NewAddonOpenedEvent += onSelectString;
    soundSystem = new SoundSystem();
    ConfigWindow = new ConfigWindow(this);
    keyActions = new Dictionary<VirtualKey, Action<string, string>>()
    {
      { VirtualKey.A, OnCommand },
      { VirtualKey.Z, OnCommand  },
      { VirtualKey.E, OnToggleFollowMe },
      { VirtualKey.R, OnQuestCommand },
      { VirtualKey.T, OnCurrentMapQuestLevelCommand }
    };
    CommandManager.AddHandler("/test", new CommandInfo(OnCommand)
    {
      HelpMessage = ""
    });
    CommandManager.AddHandler("/find", new CommandInfo(OnFind)
    {
      HelpMessage = "Find the specified object"
    });
    CommandManager.AddHandler("/tfm", new CommandInfo(OnToggleFollowMe)
    {
      HelpMessage = "Toggle the follow me sound"
    });
    CommandManager.AddHandler("/quest", new CommandInfo(OnQuestCommand)
    {
      HelpMessage = "A useful message to display in /xlhelp"
    });
    CommandManager.AddHandler("/currentmapquest", new CommandInfo(OnCurrentMapQuestLevelCommand)
    {
      HelpMessage = "A useful message to display in /xlhelp"
    });

    PluginInterface.UiBuilder.Draw += DrawUI;
    PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
  }

  private void OnPostSetup(AddonEvent type, AddonArgs args)
  {
    var addon = (AtkUnitBase*)args.Addon;
    for (int i = 0; i < addon->UldManager.NodeListCount; i++)
    {
      var node = addon->UldManager.NodeList[i];
      AddonEventManager.AddEvent((nint)addon, (nint)node, AddonEventType.FocusStart, TextHandler);
      AddonEventManager.AddEvent((nint)addon, (nint)node, AddonEventType.MouseOver, TextHandler);
    }
  }
  private void TextHandler(AddonEventType type, IntPtr addon, IntPtr node)
  {
    var text = "";
    var parentNode = ((AtkResNode*)node)->ParentNode;
    var nbchilds = parentNode->ChildCount;
    var child = (AtkResNode*)parentNode->ChildNode;
    for (int i = 0; i < nbchilds; i++)
    {
      if (child != null)
      {
        if (child->Type == NodeType.Text)
        {
          text += child->GetAsAtkTextNode()->NodeText.ToString();
        }
        child = child->NextSiblingNode;
      }
    }
    ScreenReader.Output(text);
  }
  private void OnPostRefresh(AddonEvent type, AddonArgs args)
  {
    if (args is AddonSetupArgs setupArgs)
    {
      //ScreenReader.Output(refreshArgs.AddonName);
      onSelectString(args.Addon, args.AddonName);
    }
    else if (args is AddonFinalizeArgs finalArgs)
    {
      //ScreenReader.Output(finalArgs.AddonName);
      onSelectString(args.Addon, args.AddonName);
    }
  }

  private void onTerritoryChanged(ushort e)
  {
    ScreenReader.Output(soundSystem.ObjChannels.Count().ToString());
    soundSystem.cleanObjChannel();
  }
  private RaptureAtkModule* RaptureAtkModule => CSFramework.Instance()->GetUiModule()->GetRaptureAtkModule();
  private bool IsTextInputActive => RaptureAtkModule->AtkModule.IsTextInputActive();
  private nint FocusedAddon = nint.Zero;
  private AtkResNode? _lastFocusedNode;
  SortedDictionary<string, nint> _lastAddons = new SortedDictionary<string, nint>();
  private System.Numerics.Vector3 _lastPosition;
  private bool _banging = false;
  DateTime lastTime = DateTime.Now;
  bool isHealed = true;
  public unsafe void OnFrameworkUpdate(IFramework _)
  {
    /*
    nint addonPtr = nint.Zero;

    foreach (var entry in addonDict)
    {
      addonPtr = gameGui.GetAddonByName(entry.Key);
      if (addonPtr != nint.Zero && addonPtr != _lastAddons[entry.Key])
      {
        NewAddonOpenedEvent.Invoke(addonPtr, entry.Key);
        FocusedAddon = addonPtr;
      }
      _lastAddons[entry.Key] = addonPtr;
    }
    */
    /*
    var focusedNode = AtkStage.GetSingleton()->GetFocus();
    if ((_lastFocusedNode!=null && focusedNode!=null && _lastFocusedNode.Value.NodeID != focusedNode->NodeID) || focusedNode!=null)
    {
      NodeFocusChangedEvent.Invoke(*focusedNode);
    }
    _lastFocusedNode = *focusedNode;
    */
    if (clientState.LocalPlayer != null)
    {
      var position = clientState.LocalPlayer.Position;
      RaycastHit hit;
      var flags = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
      var orientation = Util.ConvertOrientationToVector(this.clientState.LocalPlayer.Rotation);
      CSFramework.Instance()->BGCollisionModule->RaycastEx(&hit, clientState.LocalPlayer.Position + new System.Numerics.Vector3(0, 2f, 0), orientation, 10000, 1, flags);
      if ((position == _lastPosition || Vector3.Distance(position, hit.Point) <= 0.2) && tryingToMove())
      {
        if (_banging)
        {
          DateTime now = DateTime.Now;
          if (now.Subtract(lastTime).TotalMilliseconds >= 650)
          {
            UIModule.PlayChatSoundEffect(16);
            lastTime = now;
          }
        }
        _banging = true;
      }
      else if (tryingToMove())
      {
        rayArround();
        //ScreenReader.Output($"{clientState.LocalPlayer.Rotation}");
        _banging = false;
      }
      else
      {
        _banging = false;
      }
      _lastPosition = position;
      float percHP = (clientState.LocalPlayer.CurrentHp / clientState.LocalPlayer.MaxHp * 100);
      if (percHP <= 50 && isHealed)
      {
        UIModule.PlayChatSoundEffect(11);
        isHealed = false;
      }
      else if (percHP >= 99)
      {
        isHealed = true;
      }
    }
    if (!IsTextInputActive && !ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
    {
      if (keyState[VirtualKey.G])
      {
        soundSystem.WallMode = !soundSystem.WallMode;
        keyState[VirtualKey.G] = false;
      }
      if (keyState[VirtualKey.CONTROL])
      {
        foreach (var kvp in keyActions)
        {
          if (keyState[kvp.Key])
          {
            kvp.Value("", "");
            keyState[kvp.Key] = false;
          }
        }
      }
    }
    if (this.clientState.LocalPlayer != null)
    {
      var rotation = this.clientState.LocalPlayer.Rotation;
      soundSystem.System.Set3DListenerAttributes(0, clientState.LocalPlayer.Position, default, Util.ConvertOrientationToVector(rotation), soundSystem.Up);
      soundSystem.scanMapObject(
        this.gameObjects, clientState.LocalPlayer, Service.MapManager.LoadedMapId);
      soundSystem.GPSUpdate(clientState.LocalPlayer.Position);
      var currentMapWalls = new HashSet<System.Numerics.Vector3>();
      if (Walls.TryGetValue(Service.MapManager.PlayerLocationMapID, out currentMapWalls))
      {
        soundSystem.updateWallSounds(Service.MapManager.PlayerLocationMapID, _lastPosition, currentMapWalls);
      }
      soundSystem.setFollowMePlayingState(ref _lastPosition);
    }
    soundSystem.System.Update();
  }
  /*
  private void onChat(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
  {
    string senderText = sender.TextValue;
    string messageText = message.TextValue;
    string typeText= type.ToString();
    if (senderText.Substring(1).All(char.IsDigit))
    {
      senderText= string.Empty;
    }
    if (typeText.IsNullOrEmpty())
    {
      ScreenReader.Output(message.TextValue);
    }
    else if (type == XivChatType.Echo)
    {
      ScreenReader.Output($"{sender.TextValue}: {message.TextValue}");
    } else
    {
      ScreenReader.Output($"{sender.TextValue} {type} : {message.TextValue}");
    }
  }
      */
  public void Dispose()
  {
    this.framework.Update -= OnFrameworkUpdate;
    soundSystem.System.Release();
    //soundSystem.System.Dispose();
    ScreenReader.Unload();
    //WindowSystem.RemoveAllWindows();
    //ConfigWindow.Dispose();
    CommandManager.RemoveHandler("/test");
    CommandManager.RemoveHandler("/quest");
    CommandManager.RemoveHandler("/find");
    CommandManager.RemoveHandler("/tfm");
    CommandManager.RemoveHandler("/currentmapquest");
    addonLifeCycle.UnregisterListener(OnPostRefresh);
  }

  private void DrawUI()
  {
    WindowSystem.Draw();
  }

  public void DrawConfigUI()
  {
    ConfigWindow.IsOpen = true;
  }
}
