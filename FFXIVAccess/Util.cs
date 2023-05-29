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
    public static (Vector3, Vector3) ConvertOrientationToVector(float angle)
    {
      float x = 0;
      float z = 0;
      // Détermination du vecteur en fonction de l'angle
      /*
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
      x= (float)Math.Sin(angle);
      z = (float)Math.Cos(angle);
      return (new Vector3((float)x, 0, (float)z), new Vector3(0, 1, 0));
      //return (new Vector3((float)(1/Math.Sqrt(2)), 0, (float)(1 / Math.Sqrt(2))), new Vector3(0, 1, 0));
      //new Vector3((float)x * SoundSystem.DistanceFactor, (float)y, (float)z * SoundSystem.DistanceFactor);
    }
  }
}
