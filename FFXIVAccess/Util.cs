using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVAccess
{
  public class Util
  {
    public static Vector3 ConvertOrientationToVector(float angle)
    {
      double x = 0;
      double y = 0;
      double z = 0;
      /*
      // Détermination du vecteur en fonction de l'angle
      if (angle > 0)
      {
        if (angle < Math.PI / 2) // Quadrant sud-est
        {
          x = 1;
          z = 1;
        }
        else // Quadrant nord-est
        {
          x = 1;
          z = -1;
        }
      }
      else if (angle < 0)
      {
        if (angle > -Math.PI / 2) // Quadrant sud-ouest
        {
          x = -1;
          z = 1;
        }
        else // Quadrant nord-ouest
        {
          x = -1;
          z = -1;
        }
      }
      else
      {
        z = 1; // Direction nord
      }
      */
      x= (float)Math.Cos(angle);
      z= (float)Math.Sin(angle);
      return new Vector3(1*SoundSystem.DistanceFactor, 0, 0);
        //new Vector3((float)x * SoundSystem.DistanceFactor, (float)y, (float)z * SoundSystem.DistanceFactor);
    }
  }
}
