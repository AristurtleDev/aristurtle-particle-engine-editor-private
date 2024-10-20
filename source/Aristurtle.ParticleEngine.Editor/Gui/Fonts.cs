// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using ImGuiNET;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class Fonts
{
    public const string FONT_NAME = "UbuntuNerdFont-Regular";

    public static ImFontPtr NormalFont;

    public static void LoadFonts()
    {
        string path = Path.Combine(".", "Content", FONT_NAME + ".ttf");
        ImFontAtlasPtr fonts = ImGui.GetIO().Fonts;
        NormalFont = fonts.AddFontFromFileTTF(path, 16);
    }
}
