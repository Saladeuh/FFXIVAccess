using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Keys;

namespace FFXIVAccess
{
  public partial class Plugin
  {
    private bool tryingToMove()
    {
      return (keyState[VirtualKey.A] || keyState[VirtualKey.E] || keyState[VirtualKey.Z] || keyState[VirtualKey.S]);
    }
  }
}
