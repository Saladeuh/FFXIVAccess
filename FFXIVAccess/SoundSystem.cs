using FmodAudio;
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
    public const float DistanceFactor = 0.5f;
    private float T = 0.0f;
    private Vector3 LastPos;
    private Vector3 ListenerPos = new Vector3() { Z = -1.0f * DistanceFactor };
    private Sound? s1, s2, s3;
    public Channel c1, c2, c3;
    public Vector3 Up = new Vector3(1, 0, 0), Forward = new Vector3(0, 0, -1);
    public SoundSystem()
    {
      //Creates the FmodSystem object
      System = FmodAudio.Fmod.CreateSystem();
      //System object Initialization
      System.Init(32, InitFlags._3D_RightHanded);

      //Set the distance Units (Meters/Feet etc)
      System.Set3DSettings(1.0f, DistanceFactor, 1.0f);
      System.Set3DListenerAttributes(0, in ListenerPos, default, in Forward, in Up);
      //Load some sounds
      float min = 0.5f * DistanceFactor, max = 5000.0f * DistanceFactor;
      Sound sound;
      s1 = sound = System.CreateSound("test.wav", Mode._3D | Mode.Loop_Normal);
      sound.Set3DMinMaxDistance(min, max);

      //Play sounds at certain positions
      Vector3 pos = default, vel = default;

      pos.X = -10.0f * DistanceFactor;

      c1 = System.PlaySound(s1.Value, paused: true);
      c1.Set3DAttributes(in pos, in vel, default);
    }
  }
}
