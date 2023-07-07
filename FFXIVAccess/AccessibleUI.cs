using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.ContextMenu;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

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
      {"SelectYesno", typeof(AddonSelectYesno) },
      {"CharaSelect", typeof(AddonSelectString) },
      //{"AreaMap", typeof(AddonSelectString) },
      {"WorldTravelSelect", typeof(AddonSelectString) },
      //{ "ScreenFrameSystem", typeof(AddonSelectString) },
      //{"ContextMenu", typeof(AddonSelectString) },
      { "Teleport", typeof(AddonTeleport) },
      { "ParameterWidget", typeof(AddonSelectString) },
      { "EnemyList", typeof(AddonSelectString) },
      { "_TitleMenu", typeof(AddonSelectString) },
      { "_CharaSelectListMenu", typeof(AddonSelectString) },
      { "ConfigKeyBind", typeof(AddonSelectString) },
      { "SelectOk", typeof(AddonSelectString) },
      { "Macro", typeof(AddonSelectString) },
      { "MacroTextCommandList", typeof(AddonSelectString) },
      { "ConfigSystem", typeof(AddonSelectString) },
      { "ConfigCharacter", typeof(AddonSelectString) },
      { "ConfigCaraOpeGeneral", typeof(AddonSelectString) },
      { "ConfigCaraOpeTarget", typeof(AddonSelectString) },
      { "ConfigCaraOpeCircle", typeof(AddonSelectString) },
      { "ConfigCaraOpeChara", typeof(AddonSelectString) },
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
      try
      {
        var addonStructTes = Dalamud.SafeMemory.PtrToStructure<AddonSelectString>(obj);
      }
      catch (NullReferenceException e)
      {
        return;
      }
      var addonStruct = Dalamud.SafeMemory.PtrToStructure<AddonSelectString>(obj);
      if (addonStruct.HasValue)
      {
        var atk = addonStruct.Value.AtkUnitBase;
        ScreenReader.Output(MemoryHelper.ReadSeStringNullTerminated((IntPtr)atk.Name).TextValue);
        var values = addonStruct.Value.AtkUnitBase.AtkValues;
        for (int i = 0; i < addonStruct.Value.AtkUnitBase.AtkValuesCount; i++)
        {
          try
          {
            if (values[i].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String8 || values[i].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.AllocatedString || values[i].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String)
            {
              var text = Dalamud.Memory.MemoryHelper.ReadSeStringNullTerminated((IntPtr)values[i].String).TextValue;
              ScreenReader.Output(text);
            }
          }
          catch (NullReferenceException e)
          {
            continue;
          }
        }
      }
    }
    private bool isAnyKeyBind()
    {
      foreach (var key in keyState.GetValidVirtualKeys())
      {
        if (keyState[key])
        {
          return true;
        }
      }
      return false;
    }
    private unsafe AtkResNode? getTargetCursorNode(IntPtr addonPtr)
    {
      try
      {
        var addonStructTes = Dalamud.SafeMemory.PtrToStructure<AddonSelectString>(addonPtr);
      }
      catch (NullReferenceException e)
      {
        return null;
      }
      var addonStruct = Dalamud.SafeMemory.PtrToStructure<AddonSelectString>(addonPtr);
      return SafeMemory.PtrToStructure<AtkResNode>((IntPtr)addonStruct.Value.AtkUnitBase.CursorTarget);
    }
    private unsafe void onNodeFocusChanged(AtkResNode? node)
    {
      if (node != null)
        ScreenReader.Output($"change {node.Value.Type.ToString()} {node.Value.NodeID.ToString()}");
      if (node.HasValue && node.Value.Type == NodeType.Text)
      {
        var text = Dalamud.Memory.MemoryHelper.ReadSeStringNullTerminated((IntPtr)node.Value.GetAsAtkTextNode()->GetText());
        ScreenReader.Output(text.TextValue);
      }
    }
  }
}
