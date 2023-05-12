// trash tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVAccess
{
  public partial class Plugin
  {
    private unsafe void OnCommand(string command, string args)
    {
      /*
      var quests = QuestManager.Instance()->LeveQuests;
      var questArray = SafeMemory.PtrToStructure<QuestManager.QuestListArray>((nint)quests).Value;
      ScreenReader.Output(QuestManager.Instance()->NumAcceptedQuests.ToString());
      for (int i = 0; i < 10; i++)
      {
        var q = *questArray[i];
        var id = q.QuestID;
        ScreenReader.Output(id.ToString());
        var name = questList.GetRow(id).Name;
        ScreenReader.Output(name);
        ScreenReader.Output($"h {QuestManager.Instance()->GetLeveQuestById(id)->LeveId.ToString()}");
        }
      */
      //Tolk.Output($"{gameObjects[0].Name}: {gameObjects[^a,^ 0].Position.X}, {gameObjects[0].Position.Y} {gameObjects[0].Rotation}");

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
