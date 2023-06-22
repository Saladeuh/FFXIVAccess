using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FmodAudio.DigitalSignalProcessing;
using FmodAudio.DigitalSignalProcessing.Effects;
using Lumina.Excel.GeneratedSheets;

namespace FFXIVAccess
{
  public partial class Plugin
  {
    private Dictionary<Vector3, bool> collisions { get; set; }
    private List<Vector3> rayOrientations = new List<Vector3> { new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1) };
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
    private void rayArrund()
    {
      RaycastHit hit;
      foreach (Vector3 orientation in rayOrientations)
      {
        BGCollisionModule.Raycast(clientState.LocalPlayer.Position, orientation, out hit, 100f);
        Vector3 roundPoint = Util.RoundVector3(hit.Point, 1);
        //collisions[roundPoint] = true;
        if (Vector3.Distance(roundPoint, clientState.LocalPlayer.Position) <= 5)
        {
          soundSystem.channelWall.Set3DAttributes(hit.Point, default, default);
          soundSystem.channelWall.Paused = false;
        }
      }
      //ScreenReader.Output($"{collisions.Count()}");
    }
  }
}
