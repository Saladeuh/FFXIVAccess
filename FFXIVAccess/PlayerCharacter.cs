using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.GeneratedSheets;

namespace FFXIVAccess;
public unsafe partial class Plugin
{
  private readonly List<Vector3> rayOrientations = [new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1)];
  private void TargetLevelObj(Level level, Vector3 levelVector)
  {
    if (level.Object >= 1000000) // event NPC
    {
      var levelObj = GameObjects.FirstOrDefault(o => (Vector3.Distance(o.Position, levelVector) <= 2 && o.ObjectId != ClientState.LocalPlayer!.ObjectId));
      if (levelObj != null)
      {
        TargetManager.Target = levelObj;
      }
    }
    else if (level.Object > 2000000) // event object
    {
      var levelObjName = DataManager.GetExcelSheet<EObjName>()!.GetRow(level.Object)!.Singular.ToString();
      var levelObj = GameObjects.FirstOrDefault(o => o.Name.ToString().Contains(levelObjName));
      if (levelObj != null)
      {
        TargetManager.Target = levelObj;
      }
    }
  }
  public Dictionary<uint, HashSet<Vector3>> Walls = [];
  /*
  private void rayArround()
  {
    var currentMapId = this.CurrentMapId;
    RaycastHit hit;
    var flags = stackalloc int[] { 0x4000, 0, 0x4000, 0 };
    Walls.TryAdd(currentMapId, []);
    //foreach (Vector3 orientation in rayOrientations)
    //{
    var orientation = Util.ConvertOrientationToVector(this.ClientState.LocalPlayer!.Rotation);
    CSFramework.Instance()->BGCollisionModule->RaycastEx(&hit, ClientState.LocalPlayer.Position + new Vector3(0, 2f, 0), orientation, 10000, 4, flags);
    ////BGCollisionModule.Raycast((clientState.LocalPlayer.Position + new Vector3(0, 2f, 0)), orientation, out hit, 1000);
    currentMapId = this.CurrentMapId;
    var roundPoint = Util.RoundVector3(hit.Point, 0);
    Walls[currentMapId].Add(roundPoint);
    //}
    //ScreenReader.Output($"{Walls[mapId].Count()}");
  }
  */
  
  public static Vector3 FindGround(Vector3 position)
  {
    BGCollisionModule.Raycast(position, new Vector3(0, 1, 0), out var roof, 1000);// ray to the sky
    RaycastHit hit;
    if (roof.Point != new Vector3(0, 0, 0)) // indoor
    {
      BGCollisionModule.Raycast(roof.Point - new Vector3(0, 1, 0), new Vector3(0, -1, 0), out hit, 1000);
    }
    else
    {
      BGCollisionModule.Raycast(position + new Vector3(0, 1000, 0), new Vector3(0, -1, 0), out hit, 10000);
    }
    return hit.Point;
  }
  public class Point(Vector3 position, Point? origin = null)
  {
    public Vector3 Position { get; set; } = position;
    public Point? Origin { get; set; } = origin;
    public static Point? PathEnd { get; set; }

    public float DistanceOrigin()
    {
      return Vector3.Distance(Position, Origin!.Position);
    }
  }

  private readonly int intermediateFactor = 20;
  public List<Point> SearchFollowMePath(Vector3 start, int acceptanceRay = 50, List<Point>? points = null)
  {
    points ??= [new Point(start, null)];
    var result = new List<Point>(points.Count * 63);
    for (var iPoint = 0; iPoint < points.Count; iPoint++)
    {
      var origin = points[iPoint].Position;
      var distanceOriginFollowMe = Vector3.Distance(origin, SoundSystem.FollowMePoint);
      if (distanceOriginFollowMe < acceptanceRay) // find the end
      {
        Point.PathEnd = points[iPoint]; // use to extract path from result
        return result;
      }
      for (var i = -float.Pi; i < float.Pi; i += float.Pi / 10)
      {
        BGCollisionModule.Raycast((FindGround(origin) + new Vector3(0, 1.5f, 0)), Util.ConvertOrientationToVector(i), out var hit, 10000);
        var distanceToOrigin = Vector3.Distance(hit.Point, origin);
        if (distanceToOrigin > 3)
        {
          var hitPoint = new Point(hit.Point, points[iPoint]);
          result.Add(hitPoint);
          var distance = Vector3.Distance(hit.Point, SoundSystem.FollowMePoint);
          if (distance < acceptanceRay) // find the end
          {
            Point.PathEnd = hitPoint; // use to extract path from result
            return result;
          }
          if (distanceToOrigin < intermediateFactor * 2) // add intermediate points to result
          {
            while (distanceToOrigin > intermediateFactor)
            {
              var intermediatePoint = hit.Point * ((distanceToOrigin - intermediateFactor) / distanceToOrigin);
              result.Add(new Point(intermediatePoint, points[iPoint]));
              distanceToOrigin -= intermediateFactor;
            }
          }
        }
      }
    }
    ScreenReader.Output($"yo {result.Count}");
    result.Sort(delegate (Point x, Point y) // sort from the smallest to the biggest distance
    {
      var distanceX = Vector3.Distance(x.Position, SoundSystem.FollowMePoint);
      var distanceY = Vector3.Distance(y.Position, SoundSystem.FollowMePoint);
      if (Math.Abs(distanceX - distanceY) < TOLERANCE) return 0;
      else if (distanceX > distanceY) return -1;
      else if (distanceX < distanceY) return 1;
      else return x.Position.Y.CompareTo(y.Position.Y);
    });
    return SearchFollowMePath(start, acceptanceRay, result);
    //return searchFollowMePath(result.SkipLast(result.Count()/2).ToList());
  }
  private static List<Vector3> ExtractPath(List<Point> result)
  {
    var path = new List<Vector3>();
    var currentPoint = Point.PathEnd;
    while (currentPoint!.Origin != null)
    {
      path.Add(currentPoint.Position);
      currentPoint = currentPoint.Origin;
    }
    path.Reverse();
    return path;
  }

  private delegate void SetPosition(GameObject* self, float x, float y, float z);
  private readonly Hook<SetPosition>? _SetPositionHook;

  private void DetourSetPosition(GameObject* self, float x, float y, float z)
  {
    try
    {
      if (self->GetObjectID() == ClientState.LocalPlayer.ObjectId)
      {
        //ScreenReader.Output(AgentMap.Instance()->IsPlayerMoving.ToString());
        SoundSystem.GpsUpdate(ClientState.LocalPlayer.Position);
        SoundSystem.SetFollowMePlayingState(ref _lastPosition);
      }
    }
    catch { }
    this._SetPositionHook!.Original(self, x, y, z);
  }
}
