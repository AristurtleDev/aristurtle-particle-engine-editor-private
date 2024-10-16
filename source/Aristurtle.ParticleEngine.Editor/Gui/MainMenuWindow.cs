// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class MainMenuWindow
{
    public static SysVec2 Size = SysVec2.Zero;

    public static void Draw()
    {
        if (ImGui.BeginMainMenuBar())
        {
            Size = ImGui.GetWindowSize();

            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New")) { Project.CreateNew(); }
                if (ImGui.MenuItem("Open...")) { Project.OpenExisting(); }
                if (ImGui.MenuItem("Save")) { Project.Save(); }
                if (ImGui.MenuItem("Exit")) { Project.Exit(); }
            }
        }
    }
}
