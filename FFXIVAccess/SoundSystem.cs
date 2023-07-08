using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FmodAudio;
using FmodAudio.Base;
using FmodAudio.DigitalSignalProcessing;
using FmodAudio.DigitalSignalProcessing.Effects;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVAccess;
public class SoundSystem
{
  public FmodSystem System { get; }
  public Channel? channelFollowMe, channelShortFollowMe;
  public Vector3 FollowMePoint { get; set; }
  public bool GPSState { get; set; }
  public List<Vector3> GPSPath { get; set; }
  public int GPSPlayingIndex { get; set; }

  private Vector3 ListenerPos = new Vector3() { Z = -1.0f };
  public Sound? EnnemySound, FollowMeSound, EventObjSound, TrackSound;
  public Vector3 Up = new Vector3(0, 1, 0), Forward = new Vector3(0, 0, -1);
  public Dictionary<uint, Channel> ObjChannels = new Dictionary<uint, Channel>();
  public Dictionary<Vector3, Channel> WallsChannels = new Dictionary<Vector3, Channel>();
  public Dictionary<uint, HashSet<Vector3>> Tracks = new Dictionary<uint, HashSet<Vector3>>();
  public SoundSystem()
  {
    //Creates the FmodSystem object
    System = FmodAudio.Fmod.CreateSystem();
    //System object Initialization
    System.Init(4093, InitFlags._3D_RightHanded);

    //Set the distance Units (Meters/Feet etc)
    System.Set3DSettings(1.0f, 1.0f, 1.0f);
    System.Set3DListenerAttributes(0, in ListenerPos, default, in Forward, in Up);
    //Load some sounds
    float min = 2f, max = 40f; // 40 is apprximatively
    Sound sound;
    GPSPath = new List<Vector3>();
    GPSState = false;
    EnnemySound = sound = System.CreateSound("test.wav", Mode._3D | Mode.Loop_Normal | Mode._3D_LinearSquareRolloff);
    sound.Set3DMinMaxDistance(min, max);

    FollowMeSound = sound = System.CreateSound("followMe.wav", Mode._3D | Mode.Loop_Normal | Mode._3D_LinearSquareRolloff);
    sound.Set3DMinMaxDistance(min, 1000f);

    EventObjSound = sound = System.CreateSound("eventObj.wav", Mode._3D | Mode.Loop_Normal | Mode._3D_LinearSquareRolloff);
    sound.Set3DMinMaxDistance(min, 40f);
    TrackSound = sound = System.CreateSound("track.wav", Mode._3D | Mode.Loop_Normal | Mode._3D_LinearSquareRolloff);
    sound.Set3DMinMaxDistance(0f, 20f);
    channelShortFollowMe = System.PlaySound(EventObjSound.Value, paused: true);
    channelShortFollowMe.Set3DMinMaxDistance(0f, 60f);
  }
  public bool WallMode = false;
  public void scanMapObject(ObjectTable gameObjects, Character localPlayer, uint mapId)
  {
    foreach (GameObject t in gameObjects)
    {
      associateSoundToObjects(ref localPlayer, t);
      // Add objects positions to save where it's possible to wallk
      if ((!t.IsDead) && t.ObjectId != localPlayer.ObjectId && t.ObjectKind == ObjectKind.Player && Vector3.Distance(t.Position, localPlayer.Position) <= 200)
      {
        Tracks.TryAdd(mapId, new HashSet<Vector3>());
        Tracks[mapId].Add(Util.RoundVector3(t.Position, 0));
      }
    }
  }
  private void associateSoundToObjects(ref Character localPlayer, GameObject t)
  {
    if (t.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && t.ObjectId != localPlayer.ObjectId)
    {
      BattleNpc npcT = (BattleNpc)t;
      if ((!t.IsDead) && (BattleNpcSubKind)t.SubKind == BattleNpcSubKind.Enemy && Vector3.Distance(t.Position, localPlayer.Position) <= 200)
      {
        if (!ObjChannels.ContainsKey(t.ObjectId))

        {
          Channel channelNPC;
          channelNPC = System.PlaySound(EnnemySound.Value, paused: false);
          ObjChannels[t.ObjectId] = channelNPC;
        }
        ObjChannels[t.ObjectId].Set3DAttributes(t.Position, default, default);
      }
      else
      {
        if (ObjChannels.ContainsKey(t.ObjectId))
        {
          ObjChannels[t.ObjectId].Stop();
          ObjChannels.Remove(t.ObjectId);
        }
      }
    }
    /*
    else if ((t.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj || t.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc) && t.ObjectId != localPlayer.ObjectId)
    {
      if (!t.IsDead && Vector3.Distance(t.Position, localPlayer.Position) <= 200)
      {
        if (!objChannels.ContainsKey(t.ObjectId))
        {
          Channel channelObj;
          channelObj = System.PlaySound(eventObjSound.Value, paused: false);
          objChannels[t.ObjectId] = channelObj;
        }
        objChannels[t.ObjectId].Set3DAttributes(t.Position, default, default);
      }
      else
      {
        if (objChannels.ContainsKey(t.ObjectId))
        {
          objChannels[t.ObjectId].Stop();
          objChannels.Remove(t.ObjectId);
        }
      }
    }
    */
  }
  public void updateWallSounds(uint mapId, Vector3 playerPosition, HashSet<Vector3> walls)
  {
    if (WallMode)
    {
      foreach (Vector3 wallPoint in walls)
      {
        float distance = Vector3.Distance(wallPoint, playerPosition);
        if (distance <= 5)
        {
          if (!WallsChannels.ContainsKey(wallPoint))
          {
            Channel wallTrack;
            wallTrack = System.PlaySound(TrackSound.Value, paused: false);
            wallTrack.Volume = 0.2f;
            wallTrack.Set3DAttributes(wallPoint, default, default);
            WallsChannels[wallPoint] = wallTrack;
          }
        }
        else
        {
          if (WallsChannels.ContainsKey(wallPoint))
          {
            WallsChannels[wallPoint].Stop();
            WallsChannels.Remove(wallPoint);
          }
        }
      }
    }
    else
    {
      foreach (Channel channel in WallsChannels.Values)
      {
        if (channel != null && channel.IsPlaying)
        {
          channel.Stop();
        }
      }
      WallsChannels.Clear();
    }
  }
  public void updateFollowMe(Vector3 position, float min, float max)
  {
    if (channelFollowMe == null)
    {
      channelFollowMe = System.PlaySound(FollowMeSound.Value, paused: true);
    }
    channelFollowMe.Set3DAttributes(position, default, default);
    channelFollowMe.Set3DMinMaxDistance(min, max);
    FollowMePoint = position;
  }
  public void setFollowMePlayingState(ref Vector3 characterPos)
  {
    if (channelFollowMe != null)
    {
      float min;
      float max;
      channelFollowMe.Get3DMinMaxDistance(out min, out max);
      if (Vector3.Distance(characterPos, FollowMePoint) <= min)
      {
        channelFollowMe.Paused = true;
      }
      else
      {
        channelFollowMe.Paused = false;
      }
    }
  }
  public void cleanObjChannel()
  {
    foreach (var t in ObjChannels.Values)
    {
      t.Stop();
    }
    ObjChannels.Clear();
  }
  public void togleFollowMe()
  {
    if (channelFollowMe.Volume > 0)
      channelFollowMe.Volume = 0f;
    else channelFollowMe.Volume = 1f;
  }
  public void GPSStart(List<Vector3> path, Vector3 playerPos)
  {
    GPSState = true;
    GPSPath = path;
    GPSPlayingIndex = 0;
    channelShortFollowMe.Set3DAttributes(path[GPSPlayingIndex], default, default);
    channelShortFollowMe.Set3DMinMaxDistance(0, Vector3.Distance(path[GPSPlayingIndex], playerPos) * 2f);
    channelShortFollowMe.Paused = false;
    channelFollowMe.Paused = true;
    ScreenReader.Output("c bon");
  }
  public void GPSUpdate(Vector3 playerPos)
  {
    if (GPSState)
    {
      if (Vector3.Distance(playerPos, GPSPath[GPSPlayingIndex]) <= 10)
      {
        if (GPSPlayingIndex == GPSPath.Count() - 1)
        {
          GPSState = false;
          channelFollowMe.Paused = false;
          ScreenReader.Output("fini");
          return;
        }
        GPSPlayingIndex++;
        channelShortFollowMe.Set3DAttributes(GPSPath[GPSPlayingIndex], default, default);
        channelShortFollowMe.Set3DMinMaxDistance(0, Vector3.Distance(GPSPath[GPSPlayingIndex], playerPos) * 2f);
        ScreenReader.Output(GPSPlayingIndex.ToString());
      }
    }
  }
}
