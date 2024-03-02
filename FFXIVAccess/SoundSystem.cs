using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.Interop.Attributes;
using FmodAudio;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace FFXIVAccess;
public unsafe class SoundSystem
{
  public FmodSystem System { get; }
  public Channel? channelFollowMe, channelShortFollowMe;
  public Vector3 FollowMePoint { get; set; }
  public bool GPSState { get; set; }
  public List<Vector3> GPSPath { get; set; }
  public int GPSPlayingIndex { get; set; }

  private Vector3 listenerPos = new() { Z = -1.0f };
  public Sound? EnnemySound, FollowMeSound, EventObjSound, TrackSound;
  public Vector3 Up = new(0, 1, 0), Forward = new(0, 0, -1);
  public Dictionary<uint, Channel> ObjChannels = [];
  public Dictionary<uint, HashSet<Vector3>> Tracks = [];
  public SoundSystem(IGameInteropProvider gameInteropProvider)
  {
    //Creates the FmodSystem object
    System = FmodAudio.Fmod.CreateSystem();
    //System object Initialization
    System.Init(4093, InitFlags._3D_RightHanded);

    //Set the distance Units (Meters/Feet etc)
    System.Set3DSettings(1.0f, 1.0f, 1.0f);
    System.Set3DListenerAttributes(0, in listenerPos, default, in Forward, in Up);
    //Load some sounds
    float min = 2f, max = 40f; // 40 is apprximatively
    Sound sound;
    GPSPath = [];
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
    channelShortFollowMe!.Set3DMinMaxDistance(0f, 60f);
    this._CreateBattleCharacterHook = gameInteropProvider.HookFromAddress<CreateBattleCharacter>(
      (nint)ClientObjectManager.Addresses.CreateBattleCharacter.Value,
      DetourCreateBattleCharacter);
    this._CreateBattleCharacterHook.Enable();
  }
  public void scanMapObject(IObjectTable gameObjects, Character localPlayer, uint mapId)
  {
    foreach (var t in gameObjects)
    {
      associateSoundToObjects(ref localPlayer, t);
      SaveTracaks(localPlayer, mapId, t);
    }
  }

  private void SaveTracaks(Character localPlayer, uint mapId, Dalamud.Game.ClientState.Objects.Types.GameObject t)
  {
    // Add objects positions to save where it's possible to wallk
    if ((!t.IsDead) && t.ObjectId != localPlayer.ObjectId && t.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player && Vector3.Distance(t.Position, localPlayer.Position) <= 200)
    {
      Tracks.TryAdd(mapId, []);
      Tracks[mapId].Add(Util.RoundVector3(t.Position, 0));
    }
  }

  private void associateSoundToObjects(ref Character localPlayer, Dalamud.Game.ClientState.Objects.Types.GameObject t)
  {
    if (t.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && t.ObjectId != localPlayer.ObjectId)
    {
      var npcT = (BattleNpc)t;
      if ((!t.IsDead) && (BattleNpcSubKind)t.SubKind == BattleNpcSubKind.Enemy && Vector3.Distance(t.Position, localPlayer.Position) <= 200)
      {
        if (!ObjChannels.TryGetValue(t.ObjectId, out var value))
        {
          Channel channelNPC;
          channelNPC = System.PlaySound(EnnemySound.Value, paused: false);
          value = channelNPC!;
          ObjChannels[t.ObjectId] = value;
        }
        value.Set3DAttributes(t.Position, default, default);
      }
      else
      {
        if (ObjChannels.TryGetValue(t.ObjectId, out var value))
        {
          value.Stop();
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
  public void updateFollowMe(Vector3 position, float min, float max)
  {
    if (channelFollowMe == null)
    {
      channelFollowMe = System.PlaySound(FollowMeSound.Value, paused: true);
    }
    channelFollowMe!.Set3DAttributes(position, default, default);
    channelFollowMe.Set3DMinMaxDistance(min, max);
    FollowMePoint = position;
  }
  public void setFollowMePlayingState(ref Vector3 characterPos)
  {
    if (channelFollowMe != null)
    {
      channelFollowMe.Get3DMinMaxDistance(out var min, out _);
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
    if (channelFollowMe!.Volume > 0)
      channelFollowMe.Volume = 0f;
    else channelFollowMe.Volume = 1f;
  }
  public void GPSStart(List<Vector3> path, Vector3 playerPos)
  {
    GPSState = true;
    GPSPath = path;
    GPSPlayingIndex = 0;
    channelShortFollowMe!.Set3DAttributes(path[GPSPlayingIndex], default, default);
    channelShortFollowMe.Set3DMinMaxDistance(0, Vector3.Distance(path[GPSPlayingIndex], playerPos) * 2f);
    channelShortFollowMe.Paused = false;
    channelFollowMe!.Paused = true;
    ScreenReader.Output("c bon");
  }
  public void GPSUpdate(Vector3 playerPos)
  {
    if (GPSState)
    {
      if (Vector3.Distance(playerPos, GPSPath[GPSPlayingIndex]) <= 10)
      {
        if (GPSPlayingIndex == GPSPath.Count - 1)
        {
          GPSState = false;
          channelFollowMe!.Paused = false;
          ScreenReader.Output("fini");
          return;
        }
        GPSPlayingIndex++;
        channelShortFollowMe!.Set3DAttributes(GPSPath[GPSPlayingIndex], default, default);
        channelShortFollowMe.Set3DMinMaxDistance(0, Vector3.Distance(GPSPath[GPSPlayingIndex], playerPos) * 2f);
        ScreenReader.Output(GPSPlayingIndex.ToString());
      }
    }
  }
  private delegate uint CreateBattleCharacter(ClientObjectManager* self, uint index = 4294967295, byte param = 0);
  private readonly Hook<CreateBattleCharacter>? _CreateBattleCharacterHook;
  public uint DetourCreateBattleCharacter(ClientObjectManager* self, uint index = 4294967295, byte param = 0)
  {
    ScreenReader.Output("bwaaa");
    return this._CreateBattleCharacterHook.Original(self, index, param);
  }
  public void Update(bool isWindowFocused)
  {
    if (isWindowFocused)
    {
      System.MasterChannelGroup.Paused = false;
      System.Update();
    } else
    {
      System.MasterChannelGroup.Paused = true;
    }
  }
}
