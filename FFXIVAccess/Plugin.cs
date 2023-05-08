using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Game;
using DavyKager;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using System.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using Dalamud.Game.Text.SeStringHandling;
using System;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Interface;
using static Dalamud.Interface.TitleScreenMenu;
using Dalamud.ContextMenu;
using Dalamud.Game.ClientState.Objects.Types;
using System.Runtime.Serialization;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Component.Excel;
using Lumina.Data.Parsing;
using Dalamud.Game.Gui.Toast;
using FFXIVAccess.Windows;
using Dalamud.Game.Text;
using System.Linq;
using Dalamud.Utility;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace FFXIVAccess
{
  public sealed class Plugin : IDalamudPlugin
  {
    public string Name => "FFXIVAccess";
    public string Version => "0.0.0";
    private const string CommandName = "/pmycommand";
    private Lumina.Excel.ExcelSheet<Item> listItems;
    private Lumina.Excel.ExcelSheet<Addon> listAddon;
    public static readonly Dictionary<string, Type> addonDict = new Dictionary<string, Type>
    {
    { "SelectString", typeof(AddonSelectString) },
    { "Character", typeof(AddonCharacterInspect) },
      { "TelepotTown", typeof(AddonTeleport) },
      {"SystemMenu", typeof(AddonSelectString) }
    };
    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private DataManager dataManager { get; init; }
    private FlyTextGui flyTextGui { get; init; }
    private ObjectTable gameObjects { get; init; }
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
      SeStringManager seStringManager,
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
      this.gameGui = gameGui;
      this.seStringManager = seStringManager;
      this.toastGui = toastGui;
      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(PluginInterface);
      //chat.ChatMessage += onChat;
      listItems = dataManager.GetExcelSheet<Item>();
      listAddon = dataManager.GetExcelSheet<Addon>();
      var contextMenu = new DalamudContextMenu();
      contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;
      contextMenu.OnOpenInventoryContextMenu += OpenInventory;
      framework.Update += OnFrameworkUpdate;
      flyTextGui.FlyTextCreated += onFlyTextCreated;
      gameGui.HoveredActionChanged += onHoveredActionChanged;
      gameGui.HoveredItemChanged += onHoveredItemChange;
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

    private unsafe void onSelectString(nint obj, string name)
    {
      ScreenReader.Output(name);
      var addon = Dalamud.SafeMemory.PtrToStructure<FFXIVClientStructs.FFXIV.Client.UI.AddonSelectString>(obj);
      if (addon != null)
      {
        var values = addon.Value.AtkUnitBase.AtkValues;
        for (int i = 0; i < 10; i++)
        {
          if (values[i].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String8 || values[i].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.AllocatedString || values[i].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String)
          {
            var text = Dalamud.Memory.MemoryHelper.ReadSeStringNullTerminated((IntPtr)values[i].String).TextValue;
            //text = SeString.Parse(values[i].String, 64).TextValue;
            ScreenReader.Output($"{text}");
          }
        }
      }
    }

    private nint _lastAddon = nint.Zero;
    public void OnFrameworkUpdate(Framework _)
    {
      nint addon = _lastAddon;
      foreach (var entry in addonDict)
      {
        addon = gameGui.GetAddonByName(entry.Key);
        if (addon != nint.Zero && _lastAddon != addon)
        {
          AddonEvent.Invoke(addon, entry.Key);
          _lastAddon = addon;
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
    private void onQuestToast(ref SeString message, ref QuestToastOptions options, ref bool isHandled)
    {
      ScreenReader.Output(message.TextValue);
    }

    private void onErrorToast(ref SeString message, ref bool isHandled)
    {
      ScreenReader.Output(message.TextValue);
    }

    private void onToast(ref SeString message, ref ToastOptions options, ref bool isHandled)
    {
      ScreenReader.Output(message.TextValue);
    }

    private void OpenInventory(InventoryContextMenuOpenArgs args)
    {
      ScreenReader.Output($"inv {args.ItemAmount.ToString()}");
    }

    private void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
      ScreenReader.Output($"t {args.ParentAddonName}: {args.Text}");
    }

    private void onHoveredItemChange(object? sender, ulong e)
    {
      var name = listItems.GetRow((uint)e).Name;
      var desc = listItems.GetRow((uint)e).Description;
      ScreenReader.Output($"{name}: {desc}");
    }
    private void onHoveredActionChanged(object? sender, HoveredAction e)
    {
      ScreenReader.Output(e.ActionKind.ToString());
    }

    public void Dispose()
    {
      WindowSystem.RemoveAllWindows();

      ConfigWindow.Dispose();
      MainWindow.Dispose();

      CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
      Tolk.Output("wwwaaaaaaaa");
      /*
      foreach (TitleScreenMenuEntry e in titleScreenMenu.Entries)
      {
        ScreenReader.Output(e.Name);
      }

      foreach (var o in gameObjects)
      {
          ScreenReader.Output(o.Name);
      }
      */
    }
    private void onFlyTextCreated(ref FlyTextKind kind, ref int val1, ref int val2, ref SeString text1, ref SeString text2, ref uint color, ref uint icon, ref uint damageTypeIcon, ref float yOffset, ref bool handled)
    {
      ScreenReader.Output($"{text1},{text2}");
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
