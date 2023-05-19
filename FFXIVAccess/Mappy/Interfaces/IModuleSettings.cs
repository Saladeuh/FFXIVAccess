using ImGuiNET;
using Mappy.DataModels;

namespace Mappy.Interfaces;

public interface IModuleSettings
{
    public bool ShowInConfiguration() => true;

  void DrawLabel()
  {
  }
    
    void DrawSettings();
}
