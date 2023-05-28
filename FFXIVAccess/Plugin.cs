using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dalamud.ContextMenu;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Utility;
using DavyKager;
using FFXIVAccess.Windows;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Mappy;
using Mappy.System;

namespace FFXIVAccess
{
  public sealed partial class Plugin : IDalamudPlugin
  {
    public string Name => "FFXIVAccess";
    public string Version => "0.0.0";
    public static Lumina.Excel.ExcelSheet<CustomQuestSheet> questList;
    private Lumina.Excel.ExcelSheet<Item> itemList;
    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private DataManager dataManager { get; init; }
    private FlyTextGui flyTextGui { get; init; }
    public KeyState keyState { get; private set; }
    private ObjectTable gameObjects { get; init; }
    public GameGui gameGui { get; private set; }
    public SeStringManager seStringManager { get; private set; }
    private TitleScreenMenu titleScreenMenu { get; set; }
    public ClientState clientState { get; private set; }
    private ToastGui toastGui { get; set; }
    public QuestManager questManager = null!;
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("SamplePlugin");
    [PluginService] public static ChatGui Chat { get; set; } = null!;

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    public SoundSystem soundSystem { get; private set; }

    public event Action<nint, string> NewAddonOpenedEvent;
    public event Action<AtkResNode?> NodeFocusChangedEvent;
    public Plugin(
      [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
      ChatGui chat,
      ClientState clientState,
      [RequiredVersion("1.0")] CommandManager commandManager,
      Framework framework,
      FlyTextGui flyTextGui,
      GameGui gameGui,
      KeyState keyState,
      ToastGui toastGui,
      TitleScreenMenu titleScreenMenu,
      ObjectTable gameObjects,
      DataManager dataManager
      )
    {
      ScreenReader.Load(this.Name, this.Version);
      soundSystem= new SoundSystem();
      Tolk.Output("Screen Reader ready");
      // Mappy services
      PluginInterface = pluginInterface;
      pluginInterface.Create<Service>();
      Service.Cache = new CompositeLuminaCache();
      Service.ModuleManager = new ModuleManager();
      Service.QuestManager = new QuestManager();
      Service.MapManager = new MapManager();
      CommandManager = commandManager;
      this.titleScreenMenu = titleScreenMenu;
      this.clientState= clientState;
      this.gameObjects = gameObjects;
      this.gameGui = gameGui;
      this.questManager= new QuestManager();
      this.toastGui = toastGui;
      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(PluginInterface);
      //chat.ChatMessage += onChat;
      itemList = dataManager.GetExcelSheet<Item>();
      questList = dataManager.GetExcelSheet<CustomQuestSheet>();
      var contextMenu = new DalamudContextMenu();
      contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;
      contextMenu.OnOpenInventoryContextMenu += OpenInventory;
      framework.Update += OnFrameworkUpdate;
      flyTextGui.FlyTextCreated += onFlyTextCreated;
      gameGui.HoveredActionChanged += onHoveredActionChanged;
      gameGui.HoveredItemChanged += onHoveredItemChange;
      this.keyState = keyState;
      toastGui.Toast += onToast;
      toastGui.ErrorToast += onErrorToast;
      toastGui.QuestToast += onQuestToast;
      NewAddonOpenedEvent += onSelectString;
      //NodeFocusChangedEvent += onNodeFocusChanged;
      ConfigWindow = new ConfigWindow(this);
      CommandManager.AddHandler("/test", new CommandInfo(OnCommand)
      {
        HelpMessage = "A useful message to display in /xlhelp"
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

    private nint FocusedAddon = nint.Zero;
    private AtkResNode? _lastFocusedNode;
    SortedDictionary<string, nint> _lastAddons = new SortedDictionary<string, nint>();
    private System.Numerics.Vector3 _lastPosition;
    private bool _banging = false;
    DateTime lastTime = DateTime.Now;
    bool isHealed = true;
    public unsafe void OnFrameworkUpdate(Framework _)
    {
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
        if (position == _lastPosition && tryingToMove())
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
          ScreenReader.Output($"{Util.ConvertOrientationToVector(clientState.LocalPlayer.Rotation)} {position} {clientState.LocalPlayer.Rotation}");
          _banging = false;
        } else
        {
          _banging = false;
        }
        _lastPosition = position;
        float percHP = (clientState.LocalPlayer.CurrentHp / clientState.LocalPlayer.MaxHp * 100);
        if (percHP <= 98 && isHealed)
        {
          UIModule.PlayChatSoundEffect(11);
          isHealed = false;
        }
        else if (percHP >= 99)
        {
          isHealed = true;
        }
      }
      soundSystem.System.Set3DListenerAttributes(0, this.clientState.LocalPlayer.Position, default, in soundSystem.Forward, in soundSystem.Up);
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
      WindowSystem.RemoveAllWindows();

      ConfigWindow.Dispose();
      MainWindow.Dispose();

      CommandManager.RemoveHandler("/test");
      CommandManager.RemoveHandler("/quest");
      ScreenReader.Unload();
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
}
