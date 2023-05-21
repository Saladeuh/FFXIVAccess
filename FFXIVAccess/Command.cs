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
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Data.Parsing.Tex.Buffers;
using Lumina.Excel.GeneratedSheets;
using Mappy;
using Mappy.System;
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
              var text = $"{name}: {Service.MapManager.LoadedMapId}";
              var text2 = "";
              foreach (var level in Service.QuestManager.GetLevelsForQuest(extQuest))
              {
                var levelPlace = level.Map.Value.RowId;
                if (currentMapId == levelPlace)
                {
                  var levelX = level.X;
                  var levelY = level.Y;
                  var levelZ = level.Z;
                  var levelVector = new Vector3(levelX, levelY, levelZ);
                  var characPosition = (FFXIVClientStructs.FFXIV.Common.Math.Vector3)clientState.LocalPlayer.Position;
                  var path = levelVector - characPosition;
                  text2 += $" {path}: {Vector3.Distance(characPosition, levelVector)}";
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
                  text2 += $" {float.Round(directionAngle, 2)}";
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
      Random rnd = new Random();
      uint id = (uint)rnd.Next(10, 500);
      ScreenReader.Output($"{id}");
      UIModule.PlaySound(id, 0, 0, 0);
    }
  }
}
