using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.ContextMenu;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVAccess.Windows;
using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Mappy;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Events;

namespace FFXIVAccess;
public sealed unsafe partial class Plugin : IDalamudPlugin
{
  public static string Name => "FFXIVAccess";
  public static string Version => "0.0.0";
  private static Lumina.Excel.ExcelSheet<CustomQuestSheet> QuestList;
  private readonly Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item> itemList;
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
  public ITargetManager targetManager;
  public Configuration Configuration { get; init; }
  public WindowSystem WindowSystem = new("SamplePlugin");
  [PluginService] public static IChatGui Chat { get; set; } = null!;

  private ConfigWindow ConfigWindow { get; init; }
  public Dictionary<VirtualKey, Action<string, string>> keyActions { get; }
  private MainWindow MainWindow { get; init; }
  public SoundSystem soundSystem { get; private set; }

  public event Action<nint, string> OnNewAddonOpenedEvent;
  public event Action<AtkResNode?> OnNodeFocusChangedEvent;
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
    var mappyPlugin = new MappyPlugin(PluginInterface);
    CommandManager = commandManager;
    this.titleScreenMenu = titleScreenMenu;
    this.clientState = clientState;
    this.dataManager = dataManager;
    this.framework = framework;
    this.gameObjects = gameObjects;
    this.gameGui = gameGui;
    this.keyState = keyState;
    this.targetManager = targetManager;
    this.addonLifeCycle = addonLifeCycle;
    AddonEventManager = addonEventManager;
    this.toastGui = toastGui;
    Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    Configuration.Initialize(PluginInterface);
    //chat.ChatMessage += onChat;
    itemList = dataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()!;
    QuestList = dataManager.GetExcelSheet<CustomQuestSheet>()!;
    var contextMenu = new DalamudContextMenu(this.PluginInterface);
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
    toastGui.Toast += onToast;
    toastGui.ErrorToast += onErrorToast;
    toastGui.QuestToast += onQuestToast;
    OnNewAddonOpenedEvent += onSelectString;
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
    for (var i = 0; i < addon->UldManager.NodeListCount; i++)
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
    for (var i = 0; i < nbchilds; i++)
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
      this.onSelectString(args.Addon, args.AddonName);
    }
    else if (args is AddonFinalizeArgs finalArgs)
    {
      //ScreenReader.Output(finalArgs.AddonName);
      onSelectString(args.Addon, args.AddonName);
    }
  }

  private void onTerritoryChanged(ushort e)
  {
    ScreenReader.Output(soundSystem.ObjChannels.Count.ToString());
    soundSystem.cleanObjChannel();
  }
  private RaptureAtkModule* RaptureAtkModule => CSFramework.Instance()->GetUiModule()->GetRaptureAtkModule();
  private bool IsTextInputActive => RaptureAtkModule->AtkModule.IsTextInputActive();

  public uint currentMapId
  {
    get
    {
      return AgentMap.Instance()->CurrentMapId;
    }
  }

  public float lastRotation { get; private set; }

  private readonly nint focusedAddon = nint.Zero;
  private AtkResNode? _lastFocusedNode;
  private readonly SortedDictionary<string, nint> _lastAddons = [];
  private System.Numerics.Vector3 _lastPosition;
  private bool _banging = false;
  private DateTime lastTime = DateTime.Now;
  private bool isHealed = true;
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
      if (position != _lastPosition)
      {
        soundSystem.GPSUpdate(clientState.LocalPlayer.Position);
        soundSystem.setFollowMePlayingState(ref _lastPosition);
      }

      if ((position == _lastPosition || Vector3.Distance(position, hit.Point) <= 0.2) && tryingToMove())
      {
        if (_banging)
        {
          var now = DateTime.Now;
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
        //rayArround();
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
      if (rotation != lastRotation)
      {
        soundSystem.System.Set3DListenerAttributes(0, clientState.LocalPlayer.Position, default, Util.ConvertOrientationToVector(rotation), soundSystem.Up);
        lastRotation = rotation;
      }
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
