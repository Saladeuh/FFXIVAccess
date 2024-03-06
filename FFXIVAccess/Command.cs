using System.Linq;
using System.Threading;
using FFXIVClientStructs.FFXIV.Common.Math;
using Mappy.Utility;

namespace FFXIVAccess;
public partial class Plugin
{

  private unsafe void OnCurrentMapQuestLevelCommand(string command, string args)
  {
    var currentMapId = this.CurrentMapId;
    //Service.MapManager.PlayerLocationMapID;
    var questArray = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->NormalQuestsSpan;
    var acceptedQuests = QuestHelpers.GetAcceptedQuests().ToList();
    for (var i = 0; i <= 100; i++)
    {
      var questWorks = acceptedQuests;
      var qStruct = questArray[i];
      int id = qStruct.QuestId;
      foreach (var extQuest in questWorks)
      {
        if (extQuest.QuestId == id)
        {
          id += 65536;
          var name = QuestList.GetRow((uint)id)!.Name;
          var levels = QuestHelpers.GetActiveLevelsForQuest(name, currentMapId)?? [];
          var text = $"{name}: {levels.ToArray().Length}";
          var text2 = "";
          if (name.ToString().Contains(args))
          {
            foreach (var level in levels)
            {
              var levelPlace = level.Map.Value!.RowId;
              if (currentMapId == levelPlace)
              {
                var levelVector = new System.Numerics.Vector3(level.X, level.Y, level.Z);
                SoundSystem.UpdateFollowMe(levelVector, level.Radius, 1000f);
                TargetLevelObj(level, levelVector);

                var characPosition = (Vector3)ClientState.LocalPlayer!.Position;
                var path = (Vector3)levelVector - characPosition;
                text2 += $"{level.RowId}, {level.Object}, {level.Radius} {Vector3.Distance(characPosition, levelVector)} ";
                //$": {Vector3.Distance(characPosition, levelVector)}";
                if (path.X > 0)
                {
                  if (path.Z > 0) // south-east
                  { }
                  else if (path.Z < 0) // north-east
                  { }
                  else // east
                  { }
                }
                else if (path.X < 0)
                {
                  if (path.Z > 0) // south-west
                  { }
                  else if (path.Z < 0) // north-west 
                  { }
                  else // west
                  { }
                }
                else
                {
                  if (path.Z > 0) // south
                  { }
                  else // north
                  { }
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
    var acceptedQuests = QuestHelpers.GetAcceptedQuests();
    for (var i = 0; i <= 100; i++)
    {
      var q = questArray[i];
      int id = q.QuestId;
      foreach (var extQuest in acceptedQuests)
      {
        if (extQuest.QuestId == id)
        {
          id += 65536;
          var name = QuestList.GetRow((uint)id)!.Name;
          var place = QuestList.GetRow((uint)id)!.PlaceName.Value!.Name;
          var levels = QuestHelpers.GetActiveLevelsForQuest(name, this.CurrentMapId) ?? [];
          ScreenReader.Output($"{name}: {place} {levels.Count()} {this.CurrentMapId}");

        }
      }
    }
  }
  private void OnToggleFollowMe(string command, string args)
  {
    SoundSystem.TogleFollowMe();
  }
  private void OnFind(string command, string args)
  {
    foreach (var o in GameObjects)
    {
      if (o.Name.TextValue.ToLower().Contains(args.ToLower()))
      {
        SoundSystem.UpdateFollowMe(o.Position, 3f, 300f);
        TargetManager.Target = o;
        ScreenReader.Output(o.Name.ToString());
      }
    }
  }

  private Thread thread;
  private void OnCommand(string command, string args)
  {
    thread = new Thread(() =>
    {
      var result = SearchFollowMePath(ClientState.LocalPlayer!.Position, 50);
      var path = ExtractPath(result);
      //result = searchFollowMePath(path.Last(), 200);
      //path=path.Concat(extractPath(result)).ToList();
      SoundSystem.GpsStart(path, ClientState.LocalPlayer.Position);
      ScreenReader.Output($"{Vector3.Distance(path.Last(), SoundSystem.FollowMePoint)}, {Vector3.Distance(path.Last(), _lastPosition)}, {path.Count}");
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
