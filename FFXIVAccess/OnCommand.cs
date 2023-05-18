// trash tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

namespace FFXIVAccess
{
  public partial class Plugin
  {
    private unsafe void OnCommand(string command, string args)
    {
      //onNodeFocusChanged(_lastFocusedNode);
    /*
      var questArray = QuestManager.Instance()->Quest;
      var accepted = QuestManager.Instance()->NumAcceptedQuests.ToString();
      for (int i = 0; i <= 100; i++)
      {
        var q = questArray[i];
        var id = q->QuestID + 65536;
        if (id != 65536)
        {
          var name = questList.GetRow((uint)id).Name;
          var genre = questList.GetRow((uint)id).JournalGenre.Value.Name;
          var place = questList.GetRow((uint)id).PlaceName.Value.Name;
          var npc = questList.GetRow((uint)id).SatisfactionNpc;
          var intro = questList.GetRow((uint)id).ScriptInstruction;
          var npcTitle = "";
          if (npc.IsValueCreated)
          {
            npcTitle = npc.Value.Npc.Value.Title;
          }
          ScreenReader.Output($"{name}: {place} {npcTitle} {genre}");
          foreach(SeString line in intro)
          {
            ScreenReader.Output(line.Payloads[0].Type.ToString());
          }
        }
        }
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
  }
}
