using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.ContextMenu;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace FFXIVAccess
{
  public partial class Plugin
  {
    public static readonly Dictionary<string, Type> addonDict = new Dictionary<string, Type>
    {
    { "SelectString", typeof(AddonSelectString) },
    { "Character", typeof(AddonCharacterInspect) },
      { "TelepotTown", typeof(AddonTeleport) },
      {"SystemMenu", typeof(AddonSelectString) },
      {"Journal", typeof(AddonSelectString) },
      {"MonsterNote", typeof(AddonSelectString) },
      //{"AreaMap", typeof(AddonSelectString) },
      {"WorldTravelSelect", typeof(AddonSelectString) },
      //{ "ScreenFrameSystem", typeof(AddonSelectString) },
      //{"ContextMenu", typeof(AddonSelectString) },
      //{"AddonContextMenuTitle", typeof(AddonSelectString)},
      { "Telepot", typeof(AddonTeleport) },
      { "ParameterWidget", typeof(AddonSelectString) },
      { "EnemyList", typeof(AddonSelectString) },
    };

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
      var name = itemList.GetRow((uint)e).Name;
      var desc = itemList.GetRow((uint)e).Description;
      ScreenReader.Output($"{name}: {desc}");
    }
    private void onHoveredActionChanged(object? sender, HoveredAction e)
    {
      ScreenReader.Output(e.ActionKind.ToString());
    }
    private void onFlyTextCreated(ref FlyTextKind kind, ref int val1, ref int val2, ref SeString text1, ref SeString text2, ref uint color, ref uint icon, ref uint damageTypeIcon, ref float yOffset, ref bool handled)
    {
      ScreenReader.Output($"{text1},{text2}");
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
            ScreenReader.Output(text);
          }
        }
      }
    }

  }
}
