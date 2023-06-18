using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FmodAudio;
using FmodAudio.Base;
using FmodAudio.DigitalSignalProcessing;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVAccess
{
  public class SoundSystem
  {
    public FmodSystem System { get; }
    public Channel? channelFollowMe { get; set; }
    public Vector3 FollowMePoint { get; set; }

    private Vector3 ListenerPos = new Vector3() { Z = -1.0f };
    private Sound? EnnemySound, FollowMeSound, eventObjSound;
    public Vector3 Up = new Vector3(0, 1, 0), Forward = new Vector3(0, 0, -1);
    public Dictionary<uint, Channel> objChannels = new Dictionary<uint, Channel>();
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

      EnnemySound = sound = System.CreateSound("test.wav", Mode._3D | Mode.Loop_Normal | Mode._3D_LinearSquareRolloff);
      sound.Set3DMinMaxDistance(min, max);

      FollowMeSound = sound = System.CreateSound("followMe.wav", Mode._3D | Mode.Loop_Normal | Mode._3D_LinearSquareRolloff);
      sound.Set3DMinMaxDistance(min, 1000f);

      eventObjSound = sound = System.CreateSound("eventObj.wav", Mode._3D | Mode.Loop_Normal | Mode._3D_LinearSquareRolloff);
      sound.Set3DMinMaxDistance(min, 40f);
    }
    public void scanMapEnnemy(ObjectTable gameObjects, Character localPlayer)
    {
      foreach (GameObject t in gameObjects)
      {
        if (t.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc && t.ObjectId != localPlayer.ObjectId)
        {
          BattleNpc npcT = (BattleNpc)t;
          if ((!t.IsDead) && (BattleNpcSubKind)t.SubKind == BattleNpcSubKind.Enemy && Vector3.Distance(t.Position, localPlayer.Position) <= 200)
          {
            if (!objChannels.ContainsKey(t.ObjectId))
            {
              Channel channelNPC;
              channelNPC = System.PlaySound(EnnemySound.Value, paused: false);
              objChannels[t.ObjectId] = channelNPC;
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
      foreach (var t in objChannels.Values)
      {
        t.Stop();
      }
      objChannels.Clear();
    }
    public void togleFollowMe()
    {
      if (channelFollowMe.Volume > 0)
        channelFollowMe.Volume = 0f;
      else channelFollowMe.Volume = 1f;

    }
  }
}
