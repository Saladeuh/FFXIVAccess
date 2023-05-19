// trash tests
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Lumina.Excel.GeneratedSheets;
using Mappy;
using Mappy.Utilities;

namespace FFXIVAccess
{
  public partial class Plugin
  {
    private unsafe void OnCommand(string command, string args)
    {
      /*
      var space = SafeMemory.PtrToStructure<EnvSpace>((IntPtr)EnvManager.Instance()->EnvScene->EnvSpaces);
      if (space.HasValue)
      {
        ScreenReader.Output(space.Value.DrawObject.Object.NextSiblingObject->GetObjectType().ToString());
        ScreenReader.Output(space.Value.DrawObject.Object.PreviousSiblingObject->GetObjectType().ToString());
        //ScreenReader.Output(space.Value.DrawObject.Object.GetObjectType().ToString());
      }
      */

      var questArray = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->Quest;
      var accepted = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->NumAcceptedQuests.ToString();
      var acceptedQuests = Service.QuestManager.GetAcceptedQuests();
      for (int i = 0; i <= 100; i++)
      {
        var q = questArray[i];
        if (q != null) { 
        int id = q->QuestID;
        foreach (var extQuest in acceptedQuests)
        {
            if (extQuest.QuestID == id)
            {
              id += 65536;
              var name = questList.GetRow((uint)id).Name;
              var place = questList.GetRow((uint)id).PlaceName.Value.Name;
              ScreenReader.Output($"{name}: {place} {Service.QuestManager.GetLevelsForQuest(extQuest).Count()} {Service.MapManager.LoadedMapId}");
              foreach (var level in Service.QuestManager.GetLevelsForQuest(extQuest))
              {
                var levelPlace = level.Map.Value.GetName();
                var levelX = level.X;
                var levelY = level.Y;
                ScreenReader.Output($"{levelPlace}: {levelX},{levelY}");
              }
            }

        }
        }
      }
    }
  }
}
