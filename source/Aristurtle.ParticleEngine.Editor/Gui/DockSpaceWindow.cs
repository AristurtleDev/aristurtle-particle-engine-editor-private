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
                                                  ImGuiWindowFlags.NoBackground |
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
        //---------------------------------------------------------------------------------------------------------------------
        //  Not drawing the docking space window at the moment, as it's causing random crashes that I'm not able to figure out.
        //  If you would like to contribute and figure it out be my guest.  To recreate a crash, do the following
        //
        //  1. Remove the return statement below
        //  2. Run the application
        //  3. Dock the emitter window
        //  4. Dock the modifier window inside the emitter window at the top so it creates tabs
        //  5. Add a new emitter
        //  6. Click the modifier window tab
        //
        //  at that point, the crash will happen due to something with the table draws, but the exception for for invalid
        //  memory access.  I don't know, until it's figured out, docking is disabled.
        //---------------------------------------------------------------------------------------------------------------------
        return;

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
