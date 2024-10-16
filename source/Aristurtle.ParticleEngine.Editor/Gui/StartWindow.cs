// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using ImGuiNET;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class StartWindow
{
    private const string ID = nameof(StartWindow);
    private const ImGuiWindowFlags WINDOW_FLAGS = ImGuiWindowFlags.None |
                                                  ImGuiWindowFlags.NoCollapse |
                                                  ImGuiWindowFlags.NoResize |
                                                  ImGuiWindowFlags.NoMove |
                                                  ImGuiWindowFlags.NoScrollbar |
                                                  ImGuiWindowFlags.NoTitleBar;

    public static void Draw()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        ImGuiStylePtr style = ImGui.GetStyle();

        SysVec2 displaySize = io.DisplaySize;
        SysVec2 halfDisplaySize = displaySize * 0.5f;

        SysVec2 pos = new SysVec2(0, MainMenuWindow.Size.Y);

        SysVec2 size = displaySize;
        size.Y -= MainMenuWindow.Size.Y;

        ImGui.SetNextWindowPos(pos);
        ImGui.SetNextWindowSize(size);

        ImGui.Begin($"##{ID}", WINDOW_FLAGS);

        ImGui.PushFont(Fonts.TitleFont);
        ImGui.Text("Turtle Particle Engine");
        ImGui.PopFont();

        SysVec2 buttonSize = new SysVec2(400, 100);
        SysVec2 topLeft;
        topLeft.X = halfDisplaySize.X - buttonSize.X * 0.5f;
        topLeft.Y = halfDisplaySize.Y - (buttonSize.Y * 2 + style.ItemSpacing.Y) * 0.5f;

        ImGui.PushFont(Fonts.HeadingFont);
        ImGui.SetCursorPos(topLeft);

        if (ImGui.Button("Create new Project##Button", buttonSize)) { Project.CreateNew(); }

        ImGui.SetCursorPosX(topLeft.X);

        if (ImGui.Button("Open Existing Project##Button", buttonSize)) { Project.OpenExisting(); }

        ImGui.PopFont();

        ImGui.End();

    }
}
