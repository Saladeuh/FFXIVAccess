using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FmodAudio.DigitalSignalProcessing;
using FmodAudio.DigitalSignalProcessing.Effects;
using Lumina.Excel.GeneratedSheets;
using Mappy;

namespace FFXIVAccess;
public partial class Plugin
{
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
  public Dictionary<uint, HashSet<Vector3>> Walls = new Dictionary<uint, HashSet<Vector3>>();
  private void rayArround()
  {
    uint mapId = Service.MapManager.PlayerLocationMapID;
    RaycastHit hit;
    foreach (Vector3 orientation in rayOrientations)
    {
      BGCollisionModule.Raycast((clientState.LocalPlayer.Position + new Vector3(0, 1f, 0)), orientation, out hit, 1000);
      Vector3 roundPoint = Util.RoundVector3(hit.Point, 0);
      Walls.TryAdd(mapId, new HashSet<Vector3>());
      Walls[mapId].Add(roundPoint);
      /*
      if (Vector3.Distance(roundPoint, clientState.LocalPlayer.Position) <= 5)
      {
        soundSystem.channelWall.Set3DAttributes(hit.Point, default, default);
        soundSystem.channelWall.Paused = false;
      }
      */
    }
    //ScreenReader.Output($"{collisions.Count()}");
  }
  public Vector3 findGround(Vector3 position)
  {
    RaycastHit roof;
    BGCollisionModule.Raycast(position, new Vector3(0, 1, 0), out roof, 1000);// ray to the sky
    RaycastHit hit;
    if (roof.Point != new Vector3(0, 0, 0)) // indoor
    {
      BGCollisionModule.Raycast(roof.Point - new System.Numerics.Vector3(0, 1, 0), new Vector3(0, -1, 0), out hit, 1000);
    }
    else
    {
      BGCollisionModule.Raycast(position + new Vector3(0, 1000, 0), new Vector3(0, -1, 0), out hit, 10000);
    }
    return hit.Point;
  }
  public class Point
  {
    public Vector3 Position { get; set; }
    public Point? Origin { get; set; }
    public static Point pathEnd { get; set; }
    public Point(Vector3 position, Point? origin = null)
    {
      Position = position;
      Origin = origin;
    }
    public float distanceOrigin()
    {
      return Vector3.Distance(Position, Origin.Position);
    }
  }
  int intermediateFactor = 20;
  public List<Point> searchFollowMePath(Vector3 start, int acceptanceRay = 50, List<Point>? points = null)
  {
    if (points == null)
    {
      points = new List<Point>();
      points.Add(new Point(start, null));
    }
    var result = new List<Point>(points.Count * 63);
    for (int iPoint = 0; iPoint < points.Count; iPoint++)
    {
      var origin = points[iPoint].Position;
      float distanceOriginFollowMe = Vector3.Distance(origin, soundSystem.FollowMePoint);
      if (distanceOriginFollowMe < acceptanceRay) // find the end
      {
        Point.pathEnd = points[iPoint]; // use to extract path from result
        return result;
      }
      for (float i = -float.Pi; i < float.Pi; i += float.Pi / 10)
      {
        RaycastHit hit;
        BGCollisionModule.Raycast((findGround(origin) + new System.Numerics.Vector3(0, 1.5f, 0)), Util.ConvertOrientationToVector(i), out hit, 10000);
        float distanceToOrigin = Vector3.Distance(hit.Point, origin);
        if (distanceToOrigin > 3)
        {
          Point hitPoint = new Point(hit.Point, points[iPoint]);
          result.Add(hitPoint);
          float distance = Vector3.Distance(hit.Point, soundSystem.FollowMePoint);
          if (distance < acceptanceRay) // find the end
          {
            Point.pathEnd = hitPoint; // use to extract path from result
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
    ScreenReader.Output($"yo {result.Count()}");
    result.Sort(delegate (Point x, Point y) // sort from the smallest to the biggest distance
    {
      float distanceX = Vector3.Distance(x.Position, soundSystem.FollowMePoint);
      float distanceY = Vector3.Distance(y.Position, soundSystem.FollowMePoint);
      if (distanceX == distanceY) return 0;
      else if (distanceX > distanceY) return -1;
      else if (distanceX < distanceY) return 1;
      else return x.Position.Y.CompareTo(y.Position.Y);
    });
    return searchFollowMePath(start, acceptanceRay, result);
    //return searchFollowMePath(result.SkipLast(result.Count()/2).ToList());
  }
  private List<Vector3> extractPath(List<Point> searchResult)
  {
    var path = new List<Vector3>();
    var currentPoint = Point.pathEnd;
    while (currentPoint.Origin != null)
    {
      path.Add(currentPoint.Position);
      currentPoint = currentPoint.Origin;
    }
    path.Reverse();
    return path;
  }
}
