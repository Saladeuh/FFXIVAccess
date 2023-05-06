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

namespace FFXIVAccess
{
  public sealed class Plugin : IDalamudPlugin
  {
    public string Name => "FFXIVAccess";
    public string Version => "0.0.0";
    private const string CommandName = "/pmycommand";
    private Lumina.Excel.ExcelSheet<Item> listItems;
    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private DataManager dataManager { get; init; }
    private FlyTextGui flyTextGui { get; init; }
    private ObjectTable gameObjects { get; init; }
    public GameGui gameGui { get; private set; }
    private TitleScreenMenu titleScreenMenu { get; set; }
    private ToastGui toastGui { get; set; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("SamplePlugin");
    [PluginService] public static ChatGui Chat { get; set; } = null!;

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin(
      [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
      [RequiredVersion("1.0")] CommandManager commandManager,
      FlyTextGui flyTextGui,
      GameGui gameGui,
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
      this.toastGui = toastGui;
      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(PluginInterface);
      this.dataManager = dataManager;
      listItems = dataManager.GetExcelSheet<Item>();
      var contextMenu = new DalamudContextMenu();
      contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;
      contextMenu.OnOpenInventoryContextMenu += OpenInventory;
      flyTextGui.FlyTextCreated += onFlyTextCreated;
      gameGui.HoveredActionChanged += onHoveredActionChanged;
      gameGui.HoveredItemChanged += onHoveredItemChange;
      toastGui.Toast += onToast;
      toastGui.ErrorToast += onErrorToast;
      toastGui.QuestToast += onQuestToast;
      ConfigWindow = new ConfigWindow(this);
      CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
      {
        HelpMessage = "A useful message to display in /xlhelp"
      });

      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    private void onQuestToast(ref SeString message, ref QuestToastOptions options, ref bool isHandled)
    {
      Chat.Print(message);
    }

    private void onErrorToast(ref SeString message, ref bool isHandled)
    {
      Chat.Print(message);
    }

    private void onToast(ref SeString message, ref ToastOptions options, ref bool isHandled)
    {
      Chat.Print(message);
    }

    private void OpenInventory(InventoryContextMenuOpenArgs args)
    {
      Chat.Print($"inv {args.ItemAmount.ToString()}");
    }

    private void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
      Chat.Print($"t {args.ParentAddonName}: {args.Text}");
    }

    private void onHoveredItemChange(object? sender, ulong e)
    {
      var name = listItems.GetRow((uint)e).Name;
      var desc = listItems.GetRow((uint)e).Description;
      Chat.Print($"{name}: {desc}");
    }
    private void onHoveredActionChanged(object? sender, HoveredAction e)
    {
      Chat.Print(e.ActionKind.ToString());
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
      Chat.Print("lezzzgo"); Tolk.Output("wwwwé");
      /*
      foreach (TitleScreenMenuEntry e in titleScreenMenu.Entries)
      {
        Chat.Print(e.Name);
      }
      
      foreach (var o in gameObjects)
      {
          Chat.Print(o.Name);
      }
      */
      Chat.Print(Directory.GetCurrentDirectory());
    }
    private void onFlyTextCreated(ref FlyTextKind kind, ref int val1, ref int val2, ref SeString text1, ref SeString text2, ref uint color, ref uint icon, ref uint damageTypeIcon, ref float yOffset, ref bool handled)
    {
      Chat.Print($"{text1},{text2}");
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
