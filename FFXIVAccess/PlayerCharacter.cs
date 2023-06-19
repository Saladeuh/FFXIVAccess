using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.GeneratedSheets;

namespace FFXIVAccess
{
  public partial class Plugin
  {
    private bool tryingToMove()
    {
      return (keyState[VirtualKey.A] || keyState[VirtualKey.E] || keyState[VirtualKey.Z] || keyState[VirtualKey.S]);
    }
    private unsafe void targetLevelObj(Level level, System.Numerics.Vector3 levelVector)
    {
      if (level.Object >= 1000000) // event NPC
      {
        var levelObj = gameObjects.FirstOrDefault(o => (Vector3.Distance(o.Position, levelVector) <= 2 && o.ObjectId != clientState.LocalPlayer.ObjectId));
        if (levelObj != null)
        {
          targetManager.SetTarget(levelObj);
        }
      }
      else if (level.Object > 2000000) // event object
      {
        var levelObjName = dataManager.GetExcelSheet<EObjName>().GetRow(level.Object).Singular.ToString();
        var levelObj = gameObjects.FirstOrDefault(o => o.Name.ToString().Contains(levelObjName));
        if (levelObj != null)
        {
          targetManager.SetTarget(levelObj);
        }
      }
    }
    private void rayToward()
    {
      RaycastHit hit;
      BGCollisionModule.Raycast(clientState.LocalPlayer.Position, Util.ConvertOrientationToVector(clientState.LocalPlayer.Rotation), out hit);
      ScreenReader.Output($"p{hit.Point.ToString()} d{hit.Distance} f{hit.Flags}");
    }
  }
}
