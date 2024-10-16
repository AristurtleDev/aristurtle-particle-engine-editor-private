// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using ImGuiNET;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class DockSpaceWindow
{
    private const string ID = nameof(DockSpaceWindow);

    private const ImGuiWindowFlags WINDOW_FLAGS = ImGuiWindowFlags.None |
                                                  ImGuiWindowFlags.NoTitleBar |
                                                  ImGuiWindowFlags.NoResize |
                                                  ImGuiWindowFlags.NoMove |
                                                  ImGuiWindowFlags.NoBringToFrontOnFocus |
                                                  ImGuiWindowFlags.NoNavFocus |
                                                  ImGuiWindowFlags.NoBringToFrontOnFocus;

    private const ImGuiDockNodeFlags DOCK_FLAGS = ImGuiDockNodeFlags.None |
                                                  ImGuiDockNodeFlags.PassthruCentralNode |
                                                  ImGuiDockNodeFlags.NoDockingOverCentralNode;

    public static void Draw()
    {
        SysVec2 pos = new SysVec2(0, MainMenuWindow.Size.Y);

        SysVec2 size = ImGui.GetIO().DisplaySize;
        size.Y -= MainMenuWindow.Size.Y;

        ImGui.SetNextWindowPos(pos);
        ImGui.SetNextWindowSize(size);

        ImGui.Begin(ID, WINDOW_FLAGS);
        uint id = ImGui.GetID(ID);
        ImGui.DockSpace(id, SysVec2.Zero, DOCK_FLAGS);
        ImGui.End();
    }
}
