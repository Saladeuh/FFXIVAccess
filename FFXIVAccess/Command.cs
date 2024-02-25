using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Data.Parsing.Scd;
using Lumina.Excel.GeneratedSheets;
using Mappy.System;
using Mappy.Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FFXIVAccess;
public partial class Plugin
{

  private unsafe void OnCurrentMapQuestLevelCommand(string command, string args)
  {
    var currentMapId = this.currentMapId;
    //Service.MapManager.PlayerLocationMapID;
    var questArray = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->NormalQuestsSpan;
    var acceptedQuests = QuestHelpers.GetAcceptedQuests();
    for (int i = 0; i <= 100; i++)
    {
      var qStruct = questArray[i];
      int id = qStruct.QuestId;
      foreach (var extQuest in acceptedQuests)
      {
        if (extQuest.QuestId == id)
        {
          id += 65536;
          var name = questList.GetRow((uint)id).Name;
          var levels = QuestHelpers.GetActiveLevelsForQuest(name, currentMapId);
          var text = $"{name}: {levels.Count()}";
          var text2 = "";
          if (name.ToString().Contains(args))
          {
            foreach (var level in levels)
            {
              var levelPlace = level.Map.Value.RowId;
              if (currentMapId == levelPlace)
              {
                var levelVector = new System.Numerics.Vector3(level.X, level.Y, level.Z);
                soundSystem.updateFollowMe(levelVector, level.Radius, 1000f);
                targetLevelObj(level, levelVector);

                var characPosition = (FFXIVClientStructs.FFXIV.Common.Math.Vector3)clientState.LocalPlayer.Position;
                var path = (Vector3)levelVector - characPosition;
                text2 += $"{level.RowId}, {level.Object}, {level.Radius} {Vector3.Distance(characPosition, levelVector)} ";
                //$": {Vector3.Distance(characPosition, levelVector)}";
                float directionAngle;
                if (path.X > 0)
                {
                  if (path.Z > 0) // south-east
                    directionAngle = float.Pi / 4;
                  else if (path.Z < 0) // north-east
                    directionAngle = float.Pi / 2 + float.Pi / 4;
                  else // east
                    directionAngle = float.Pi / 2;
                }
                else if (path.X < 0)
                {
                  if (path.Z > 0) // south-west
                    directionAngle = -(float.Pi / 4);
                  else if (path.Z < 0) // north-west 
                    directionAngle = -(float.Pi / 2 + float.Pi / 4);
                  else // west
                    directionAngle = -(float.Pi / 2);
                }
                else
                {
                  if (path.Z > 0) // south
                    directionAngle = 0;
                  else // north
                    directionAngle = float.Pi;
                }
                //text2 += $"{extQuest.CurrentSequenceNumber.ToString()} {float.Round(directionAngle, 2)}";
                ScreenReader.Output(text);
                ScreenReader.Output(text2);
              }

            }
          }
        }
      }
    }
  }


  private unsafe void OnQuestCommand(string command, string args)
  {
    // send all quests and details
    var questArray = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->NormalQuestsSpan;
    var accepted = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->NumAcceptedQuests.ToString();
    var acceptedQuests = QuestHelpers.GetAcceptedQuests();
    for (int i = 0; i <= 100; i++)
    {
      var q = questArray[i];
      int id = q.QuestId;
      foreach (var extQuest in acceptedQuests)
      {
        if (extQuest.QuestId == id)
        {
          id += 65536;
          var name = questList.GetRow((uint)id).Name;
          var place = questList.GetRow((uint)id).PlaceName.Value.Name;
          ScreenReader.Output($"{name}: {place} {QuestHelpers.GetActiveLevelsForQuest(name, this.currentMapId).Count()} {this.currentMapId}");

        }
      }
    }
  }
  private unsafe void OnToggleFollowMe(string command, string args)
  {
    soundSystem.togleFollowMe();
  }
  private unsafe void OnFind(string command, string args)
  {
    foreach (var o in gameObjects)
    {
      if (o.Name.TextValue.ToLower().Contains(args.ToLower()))
      {
        soundSystem.updateFollowMe(o.Position, 3f, 300f);
        targetManager.Target = o;
        ScreenReader.Output(o.Name.ToString());
      }
    }
  }
  Thread thread = null;
  private unsafe void OnCommand(string command, string args)
  {
    thread = new Thread(() =>
    {
      var result = searchFollowMePath(clientState.LocalPlayer.Position, 50);
      var path = extractPath(result);
      //result = searchFollowMePath(path.Last(), 200);
      //path=path.Concat(extractPath(result)).ToList();
      soundSystem.GPSStart(path, clientState.LocalPlayer.Position);
      ScreenReader.Output($"{Vector3.Distance(path.Last(), soundSystem.FollowMePoint)}, {Vector3.Distance(path.Last(), _lastPosition)}, {path.Count()}");
    });
    thread.Start();
    /*
    var playerRotation = clientState.LocalPlayer.Rotation;
    Vector3 closestHit = Vector3.PositiveInfinity;
    float closestDistance = float.PositiveInfinity;
    for (float i = -float.Pi; i <float.Pi; i += float.Pi / 10)
    {
      RaycastHit hit;
      BGCollisionModule.Raycast((findGround(clientState.LocalPlayer.Position) + new System.Numerics.Vector3(0, 2, 0)), Util.ConvertOrientationToVector(i), out hit, 10000);
      float distance = Vector3.Distance(hit.Point, soundSystem.FollowMePoint);
      if (distance < closestDistance)
      {
        closestDistance = distance;
        closestHit = hit.Point;
      }
    }

    /*
    soundSystem.channelShortFollowMe.Set3DAttributes(closestHit, default, default);
    soundSystem.channelShortFollowMe.Paused= false;
    ScreenReader.Output(Vector3.Distance(closestHit,clientState.LocalPlayer.Position).ToString());
    /*
    uint markerRange=dataManager.GetExcelSheet<Map>().GetRow(Service.MapManager.LoadedMapId).MapMarkerRange;
    for (uint subId = 0; subId <= 20; subId++)
    {
      try
      {
        var marker = dataManager.GetExcelSheet<MapMarker>().GetRow(markerRange, subId);
        var distance = float.Round(Vector3.Distance(_lastPosition, new Vector3(marker.X, 0, marker.Y)),0);
        ScreenReader.Output($"{marker.PlaceNameSubtext.Value.Name.ToString()} {distance}");
      } catch { } // don't throw exception if subId not valid
    }
    */
  }
}
