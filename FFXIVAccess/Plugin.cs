using System;
using System.Collections.Generic;
using Dalamud.ContextMenu;
using Dalamud.Data;
using Dalamud.Game;
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
using DavyKager;
using FFXIVAccess.Windows;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.GeneratedSheets;

namespace FFXIVAccess
{
  public sealed partial class Plugin : IDalamudPlugin
  {
    public string Name => "FFXIVAccess";
    public string Version => "0.0.0";
    private const string CommandName = "/pmycommand";
    private Lumina.Excel.ExcelSheet<Quest> questList;
    private Lumina.Excel.ExcelSheet<Item> itemList;
    private Lumina.Excel.ExcelSheet<Addon> addonList;
    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private DataManager dataManager { get; init; }
    private FlyTextGui flyTextGui { get; init; }
    public KeyState keyState { get; private set; }
    private ObjectTable gameObjects { get; init; }
    public Dalamud.Game.ClientState.Objects.Types.Character? character { get; set; }
    public GameGui gameGui { get; private set; }
    public SeStringManager seStringManager { get; private set; }
    private TitleScreenMenu titleScreenMenu { get; set; }
    private ToastGui toastGui { get; set; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("SamplePlugin");
    [PluginService] public static ChatGui Chat { get; set; } = null!;

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public event Action<nint, string> AddonEvent;
    public Plugin(
      [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
      ChatGui chat,
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
      Tolk.Output("Screen Reader ready");
      PluginInterface = pluginInterface;
      CommandManager = commandManager;
      this.titleScreenMenu = titleScreenMenu;
      this.gameObjects = gameObjects;
      this.character = (Dalamud.Game.ClientState.Objects.Types.Character)gameObjects[0];
      this.gameGui = gameGui;
      this.seStringManager = seStringManager;
      this.toastGui = toastGui;
      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(PluginInterface);
      //chat.ChatMessage += onChat;
      itemList = dataManager.GetExcelSheet<Item>();
      questList = dataManager.GetExcelSheet<Quest>();
      addonList = dataManager.GetExcelSheet<Addon>();
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
      AddonEvent += onSelectString;
      ConfigWindow = new ConfigWindow(this);
      CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
      {
        HelpMessage = "A useful message to display in /xlhelp"
      });

      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    SortedDictionary<string, nint> _lastAddons = new SortedDictionary<string, nint>();
    private System.Numerics.Vector3 _lastPosition;
    private bool _banging = false;
    DateTime lastTime = DateTime.Now;
    bool isHealed = true;
    public void OnFrameworkUpdate(Framework _)
    {
      character = (Character)gameObjects[0];
      nint addonPtr = nint.Zero;
      foreach (var entry in addonDict)
      {
        addonPtr = gameGui.GetAddonByName(entry.Key);
        if (addonPtr != nint.Zero && addonPtr != _lastAddons[entry.Key])
        {
          AddonEvent.Invoke(addonPtr, entry.Key);
        }
        _lastAddons[entry.Key] = addonPtr;
      }
      if (character != null)
      {
        var position = character.Position;
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
        else
        {
          _banging = false;
        }
        _lastPosition = position;

        float percHP = (character.CurrentHp / character.MaxHp * 100);
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

      CommandManager.RemoveHandler(CommandName);
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
