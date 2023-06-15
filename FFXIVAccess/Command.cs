// trash tests
using System;
using System.IO;
using System.Linq;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Data.Parsing.Scd;
using Lumina.Excel.GeneratedSheets;
using Mappy;
using Mappy.Utilities;

namespace FFXIVAccess
{
  public partial class Plugin
  {
    private unsafe void OnCurrentMapQuestLevelCommand(string command, string args)
    {
      var currentMapId = Service.MapManager.PlayerLocationMapID;
      var questArray = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->Quest;
      var acceptedQuests = Service.QuestManager.GetAcceptedQuests();
      for (int i = 0; i <= 100; i++)
      {
        var qStruct = questArray[i];
        if (qStruct != null)
        {
          int id = qStruct->QuestID;
          foreach (var extQuest in acceptedQuests)
          {
            if (extQuest.QuestID == id)
            {
              id += 65536;
              var name = questList.GetRow((uint)id).Name;
              var text = $"{name}: {Service.QuestManager.GetActiveLevelIndexes(extQuest).Count()}";
              var text2 = "";
              if (name.ToString().Contains(args))
              {
                foreach (var level in Service.QuestManager.GetLevelsForQuest(extQuest))
                {
                  var levelPlace = level.Map.Value.RowId;
                  if (currentMapId == levelPlace)
                  {
                    var levelVector = new System.Numerics.Vector3(level.X, level.Y, level.Z);
                    soundSystem.playFollowMe(levelVector, level.Radius, 1000f);
                    //var levelObjName=dataManager.GetExcelSheet<EObjName>().GetRow(level.Object);
                    var levelObj = gameObjects.FirstOrDefault(o => (Vector3.Distance(o.Position, levelVector) <= 20));
                    if (levelObj != null)
                    {
                      targetManager.SetTarget(levelObj);
                    }
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
    }
    private unsafe void OnQuestCommand(string command, string args)
    {
      // send all quests and details
      var questArray = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->Quest;
      var accepted = FFXIVClientStructs.FFXIV.Client.Game.QuestManager.Instance()->NumAcceptedQuests.ToString();
      var acceptedQuests = Service.QuestManager.GetAcceptedQuests();
      for (int i = 0; i <= 100; i++)
      {
        var q = questArray[i];
        if (q != null)
        {
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
                ScreenReader.Output($"{levelPlace}");
              }
            }
          }
        }
      }
    }
    private unsafe void OnCommand(string command, string args)
    {
      if(soundSystem.channelFollowMe.Volume>0)
      soundSystem.channelFollowMe.Volume = 0f;
      else soundSystem.channelFollowMe.Volume = 1f;
    }
  }
}
