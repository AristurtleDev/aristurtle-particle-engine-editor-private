// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class ImGuiEx
{
    public static void OutlinePreviousItemIfActive()
    {
        if (ImGui.IsItemActive())
        {
            SysVec2 min = ImGui.GetItemRectMin();
            SysVec2 max = ImGui.GetItemRectMax();
            uint col = ImGui.GetColorU32(new SysVec4(1, 1, 1, 1));
            ImDrawFlags flags = ImDrawFlags.None;
            ImGui.GetWindowDrawList().AddRect(min, max, col, 0.0f, flags, 1.0f);
        }
    }
}
