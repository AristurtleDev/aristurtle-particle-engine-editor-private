// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using ImGuiNET;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class PositionGizmo
{
    private const float LINE_LENGTH = 50.0f;
    private const float LINE_THICKNESS = 5.0f;
    private const float CENTER_HANDLE_RADIUS = 5.0f;
    private static readonly uint s_red = ImGui.GetColorU32(new Vec4(1, 0, 0, 1));
    private static readonly uint s_green = ImGui.GetColorU32(new Vec4(0, 1, 0, 1));
    private static readonly uint s_white = ImGui.GetColorU32(new Vec4(1, 1, 1, 1));

    public static void Draw(ref Vec2 position)
    {
        Vec2 windowPos;
        windowPos.X = position.X - LINE_LENGTH;
        windowPos.Y = position.Y - LINE_LENGTH;

        Vec2 windowSize = new Vec2(LINE_LENGTH * 2.0f);

        ImGui.SetNextWindowPos(windowPos);
        ImGui.SetNextWindowSize(windowSize);
        ImGui.Begin(nameof(PositionGizmo), ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoResize);
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            //  Draw the x-axis handle
            Vec2 xEnd;
            xEnd.X = position.X + LINE_LENGTH;
            xEnd.Y = position.Y;
            drawList.AddLine(position, xEnd, s_red, LINE_THICKNESS);

            //  Draw the y-axis handle
            Vec2 yEnd;
            yEnd.X = position.X;
            yEnd.Y = position.Y + -LINE_LENGTH;
            drawList.AddLine(position, yEnd, s_green, LINE_THICKNESS);

            //  Draw center handle
            drawList.AddCircleFilled(position, CENTER_HANDLE_RADIUS, s_white);
            drawList.AddCircle(position, CENTER_HANDLE_RADIUS, s_red, 10, 2.0f);

            //  Handle dragging
            ImGui.SetCursorScreenPos(position - new Vec2(CENTER_HANDLE_RADIUS));
            ImGui.InvisibleButton("GizmoHandle", new Vec2(CENTER_HANDLE_RADIUS * 2.0f));

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                position += ImGui.GetIO().MouseDelta;
            }
            ImGui.End();
        }
    }
}
