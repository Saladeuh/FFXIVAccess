using System;
using System.Collections.Generic;
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
using Mappy;
using Dalamud.Game.Command;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using SightyFriend;

namespace FFXIVAccess;
public sealed unsafe partial class Plugin : IDalamudPlugin
{
  public static string Name => "FFXIVAccess";
  public static string Version => "0.0.0";
  private static Lumina.Excel.ExcelSheet<CustomQuestSheet> QuestList;
  private readonly Lumina.Excel.ExcelSheet<Item> itemList;
  private DalamudPluginInterface PluginInterface { get; init; }
  private ICommandManager CommandManager { get; init; }
  private IDataManager DataManager { get; init; }
  public IAddonLifecycle AddonLifeCycle { get; }
  public IAddonEventManager AddonEventManager { get; }
  public IFramework Framework { get; set; }
  private IFlyTextGui FlyTextGui { get; init; }
  public IKeyState KeyState { get; private set; }
  private IObjectTable GameObjects { get; init; }
  public IGameGui GameGui { get; private set; }


  public SeStringBuilder SeStringBuilder { get; }
  private ITitleScreenMenu TitleScreenMenu { get; }
  public IClientState ClientState { get; private set; }
  private IToastGui ToastGui { get; }
  public ITargetManager TargetManager;
  public Configuration Configuration { get; init; }
  public WindowSystem WindowSystem = new("SamplePlugin");
  [PluginService] public static IChatGui Chat { get; set; } = null!;

  private ConfigWindow ConfigWindow { get; init; }
  public Dictionary<VirtualKey, Action<string, string>> KeyActions { get; }
  public SoundSystem SoundSystem { get; private set; }

  public event Action<nint, string> OnNewAddonOpenedEvent;
  public event Action<AtkResNode?> OnNodeFocusChangedEvent;
  public Plugin(
    [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
    IChatGui chat,
    IClientState clientState,
    [RequiredVersion("1.0")] ICommandManager commandManager,
    IFramework framework,
    IFlyTextGui flyTextGui,
    IGameInteropProvider gameInteropProvider,
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
    var sf=new SightyFriendPlugin(PluginInterface, commandManager);
    CommandManager = commandManager;
    this.TitleScreenMenu = titleScreenMenu;
    this.ClientState = clientState;
    this.DataManager = dataManager;
    this.Framework = framework;
    this.GameObjects = gameObjects;
    this.GameGui = gameGui;
    this.KeyState = keyState;
    this.TargetManager = targetManager;
    this.AddonLifeCycle = addonLifeCycle;
    AddonEventManager = addonEventManager;
    this.ToastGui = toastGui;
    Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    Configuration.Initialize(PluginInterface);
    this._SetPositionHook = gameInteropProvider.HookFromAddress<SetPosition>(
      (nint)GameObject.Addresses.SetPosition.Value,
      DetourSetPosition);
    this._SetPositionHook.Enable();
    
    //chat.ChatMessage += onChat;
    itemList = dataManager.GetExcelSheet<Item>()!;
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
    SoundSystem = new SoundSystem(gameInteropProvider);
    ConfigWindow = new ConfigWindow(this);
    KeyActions = new Dictionary<VirtualKey, Action<string, string>>()
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
    var child = parentNode->ChildNode;
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
    ScreenReader.Output(SoundSystem.ObjChannels.Count.ToString());
    SoundSystem.CleanObjChannel();
  }
  private RaptureAtkModule* RaptureAtkModule => CSFramework.Instance()->GetUiModule()->GetRaptureAtkModule();
  private bool IsTextInputActive => RaptureAtkModule->AtkModule.IsTextInputActive();

  public uint CurrentMapId => AgentMap.Instance()->CurrentMapId;

  public float LastRotation { get; private set; }

  private readonly nint focusedAddon = nint.Zero;
  private AtkResNode? _lastFocusedNode;
  private readonly SortedDictionary<string, nint> _lastAddons = [];
  private System.Numerics.Vector3 _lastPosition;
  private bool _banging = false;
  private DateTime lastTime = DateTime.Now;
  private bool isHealed = true;
  public void OnFrameworkUpdate(IFramework _)
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
    if (ClientState.LocalPlayer != null)
    {
      SoundSystem.scanMapObject(GameObjects, this.ClientState.LocalPlayer, CurrentMapId);
      var rotation = this.ClientState.LocalPlayer.Rotation;
      if (Math.Abs(rotation - LastRotation) > TOLERANCE)
      {
        SoundSystem.System.Set3DListenerAttributes(0, ClientState.LocalPlayer.Position, default, Util.ConvertOrientationToVector(rotation), SoundSystem.Up);
        LastRotation = rotation;
      }
      var position = ClientState.LocalPlayer.Position;
      RaycastHit hit;
      var flags = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
      var orientation = Util.ConvertOrientationToVector(this.ClientState.LocalPlayer.Rotation);
      CSFramework.Instance()->BGCollisionModule->RaycastEx(&hit, ClientState.LocalPlayer.Position + new System.Numerics.Vector3(0, 2f, 0), orientation, 10000, 1, flags);
      if ((position == _lastPosition || Vector3.Distance(position, hit.Point) <= 0.2) && AgentMap.Instance()->IsPlayerMoving==1)
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
      else if (AgentMap.Instance()->IsPlayerMoving == 1)
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
      float percHP = (ClientState.LocalPlayer.CurrentHp / ClientState.LocalPlayer.MaxHp * 100);
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
      if (KeyState[VirtualKey.CONTROL])
      {
        foreach (var kvp in KeyActions)
        {
          if (KeyState[kvp.Key])
          {
            kvp.Value("", "");
            KeyState[kvp.Key] = false;
          }
        }
      }
    }
    SoundSystem.Update(UIInputData.Instance()->IsGameWindowFocused);
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
    this.Framework.Update -= OnFrameworkUpdate;
    SoundSystem.System.Release();
    //soundSystem.System.Dispose();
    ScreenReader.Unload();
    //WindowSystem.RemoveAllWindows();
    //ConfigWindow.Dispose();
    CommandManager.RemoveHandler("/test");
    CommandManager.RemoveHandler("/quest");
    CommandManager.RemoveHandler("/find");
    CommandManager.RemoveHandler("/tfm");
    CommandManager.RemoveHandler("/currentmapquest");
    AddonLifeCycle.UnregisterListener(OnPostRefresh);
  }
}
