//  ImguiRenderer.cs
//  https://github.com/ImGuiNET/ImGui.NET/blob/master/src/ImGui.NET.SampleProgram.XNA/ImGuiRenderer.cs
//  Licensed under MIT License
//  https://github.com/ImGuiNET/ImGui.NET/blob/master/LICENSE
//  The MIT License (MIT)
//
//  Copyright (c) 2017 Eric Mellino and ImGui.NET contributors
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Aristurtle.ParticleEngine.Editor.Gui;

namespace Aristurtle.ParticleEngine.Editor.Graphics;

/// <summary>
/// ImGui renderer for use with XNA-likes (FNA & MonoGame)
/// </summary>
public class ImGuiRenderer
{
    private Game _game;

    // Graphics
    private GraphicsDevice _graphicsDevice;

    private BasicEffect _effect;
    private RasterizerState _rasterizerState;

    private byte[] _vertexData;
    private VertexBuffer _vertexBuffer;
    private int _vertexBufferSize;

    private byte[] _indexData;
    private IndexBuffer _indexBuffer;
    private int _indexBufferSize;

    // Textures
    private Dictionary<nint, Texture2D> _loadedTextures;

    private int _textureId;
    private nint? _fontTextureId;

    // Input
    private int _scrollWheelValue;
    private int _horizontalScrollWheelValue;
    private readonly float WHEEL_DELTA = 120;
    private Keys[] _allKeys = Enum.GetValues<Keys>();

    public ImGuiRenderer(Game game)
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        _game = game ?? throw new ArgumentNullException(nameof(game));
        _graphicsDevice = game.GraphicsDevice;

        _loadedTextures = new Dictionary<nint, Texture2D>();

        _rasterizerState = new RasterizerState()
        {
            CullMode = CullMode.None,
            DepthBias = 0,
            FillMode = FillMode.Solid,
            MultiSampleAntiAlias = false,
            ScissorTestEnable = true,
            SlopeScaleDepthBias = 0,
        };



        SetupInput();
        Fonts.LoadFonts();
        RebuildFontAtlas();
    }

    #region ImGuiRenderer

    /// <summary>
    /// Creates a texture and loads the font data from ImGui. Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done
    /// </summary>
    public virtual unsafe void RebuildFontAtlas()
    {
        // Get font texture from ImGui
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        // Copy the data to a managed array
        var pixels = new byte[width * height * bytesPerPixel];
        unsafe { Marshal.Copy(new nint(pixelData), pixels, 0, pixels.Length); }

        // Create and register the texture as an XNA texture
        var tex2d = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
        tex2d.SetData(pixels);

        // Should a texture already have been build previously, unbind it first so it can be deallocated
        if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);

        // Bind the new texture to an ImGui-friendly id
        _fontTextureId = BindTexture(tex2d);

        // Let ImGui know where to find the texture
        io.Fonts.SetTexID(_fontTextureId.Value);
        io.Fonts.ClearTexData(); // Clears CPU side texture data
    }

    /// <summary>
    /// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. That pointer is then used by ImGui to let us know what texture to draw
    /// </summary>
    public virtual nint BindTexture(Texture2D texture)
    {
        var id = new nint(_textureId++);

        _loadedTextures.Add(id, texture);


        return id;
    }

    /// <summary>
    /// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
    /// </summary>
    public virtual void UnbindTexture(nint textureId)
    {
        _loadedTextures.Remove(textureId);
    }

    /// <summary>
    /// Sets up ImGui for a new frame, should be called at frame start
    /// </summary>
    public virtual void BeforeLayout(GameTime gameTime)
    {
        ImGui.GetIO().DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateInput();

        //if(_clientResized)
        //{
        //    ImGuiIOPtr io = ImGui.GetIO();
        //    Vec2 displaySize = io.DisplaySize;
        //    float baseWidth = 1280.0f; // Reference width
        //    float baseHeight = 720.0f; // Reference height

        //    float scaleX = displaySize.X / baseWidth;
        //    float scaleY = displaySize.Y / baseHeight;
        //    float scale = Math.Min(scaleX, scaleY);
        //    io.FontGlobalScale = (int)scale;

        //    ImGui.GetStyle().ScaleAllSizes(scale);
        //    _clientResized = false;
        //}

        //SetupImGuiStyle();
        ImGui.NewFrame();
    }



    public static void SetupImGuiStyle(float scaleFactor)
    {
        // Moonlight styleMadam-Herta from ImThemes
        var style = ImGui.GetStyle();

        float rounding = 5.0f;

        style.Alpha = 1.0f;
        style.DisabledAlpha = 1.0f;
        style.WindowPadding = new Vec2(12.0f, 12.0f) * scaleFactor;
        style.WindowRounding = rounding * scaleFactor;
        style.WindowBorderSize = 0.0f * scaleFactor;
        style.WindowMinSize = new Vec2(20.0f, 20.0f) * scaleFactor;
        style.WindowTitleAlign = new Vec2(0.5f, 0.5f);
        style.WindowMenuButtonPosition = ImGuiDir.Right;
        style.ChildRounding = 0.0f;
        style.ChildBorderSize = 1.0f * scaleFactor;
        style.PopupRounding = 0.0f;
        style.PopupBorderSize = 1.0f * scaleFactor;
        style.FramePadding = new Vec2(20.0f, 3.400000095367432f) * scaleFactor;
        style.FrameRounding = rounding;
        style.FrameBorderSize = 0.0f;
        style.ItemSpacing = new Vec2(4.300000190734863f, 5.5f) * scaleFactor;
        style.ItemInnerSpacing = new Vec2(7.099999904632568f, 1.799999952316284f) * scaleFactor;
        style.CellPadding = new Vec2(12.10000038146973f, 9.199999809265137f) * scaleFactor;
        style.IndentSpacing = 0.0f;
        style.ColumnsMinSpacing = 4.900000095367432f * scaleFactor;
        style.ScrollbarSize = 11.60000038146973f * scaleFactor;
        style.ScrollbarRounding = rounding * scaleFactor;
        style.GrabMinSize = 3.700000047683716f * scaleFactor;
        style.GrabRounding = rounding;
        style.TabRounding = 0.0f;
        style.TabBorderSize = 0.0f;
        style.TabMinWidthForCloseButton = 0.0f;
        style.ColorButtonPosition = ImGuiDir.Right;
        style.ButtonTextAlign = new Vec2(0.5f, 0.5f);
        style.SelectableTextAlign = new Vec2(0.0f, 0.0f);



        style.Colors[(int)ImGuiCol.Text] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vec4(0.2745098173618317f, 0.3176470696926117f, 0.4509803950786591f, 1.0f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vec4(0.09411764889955521f, 0.1019607856869698f, 0.1176470592617989f, 1.0f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.Border] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vec4(0.1137254908680916f, 0.125490203499794f, 0.1529411822557449f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vec4(0.09803921729326248f, 0.105882354080677f, 0.1215686276555061f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.CheckMark] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vec4(1.0f, 0.7960784435272217f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.Button] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vec4(0.1803921610116959f, 0.1882352977991104f, 0.196078434586525f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vec4(0.1529411822557449f, 0.1529411822557449f, 0.1529411822557449f, 1.0f);
        style.Colors[(int)ImGuiCol.Header] = new Vec4(0.1411764770746231f, 0.1647058874368668f, 0.2078431397676468f, 1.0f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vec4(0.105882354080677f, 0.105882354080677f, 0.105882354080677f, 1.0f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.Separator] = new Vec4(0.1294117718935013f, 0.1490196138620377f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vec4(0.1568627506494522f, 0.1843137294054031f, 0.250980406999588f, 1.0f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vec4(0.1568627506494522f, 0.1843137294054031f, 0.250980406999588f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vec4(0.1450980454683304f, 0.1450980454683304f, 0.1450980454683304f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.Tab] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.TabSelected] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLines] = new Vec4(0.5215686559677124f, 0.6000000238418579f, 0.7019608020782471f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vec4(0.03921568766236305f, 0.9803921580314636f, 0.9803921580314636f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vec4(0.8823529481887817f, 0.7960784435272217f, 0.5607843399047852f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vec4(0.95686274766922f, 0.95686274766922f, 0.95686274766922f, 1.0f);
        style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderLight] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBg] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vec4(0.09803921729326248f, 0.105882354080677f, 0.1215686276555061f, 1.0f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vec4(0.9372549057006836f, 0.9372549057006836f, 0.9372549057006836f, 1.0f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new Vec4(0.4980392158031464f, 0.5137255191802979f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.NavHighlight] = new Vec4(0.2666666805744171f, 0.2901960909366608f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vec4(0.4980392158031464f, 0.5137255191802979f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vec4(0.196078434586525f, 0.1764705926179886f, 0.5450980663299561f, 0.501960813999176f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vec4(0.196078434586525f, 0.1764705926179886f, 0.5450980663299561f, 0.501960813999176f);
    }

    public static void SetupImGuiStyle()
    {
        var style = ImGui.GetStyle();
        //float rounding = 5.0f;

        //style.Alpha = 1.0f;
        //style.DisabledAlpha = 1.0f;
        //style.WindowPadding = new Vec2(12.0f, 12.0f);
        //style.WindowRounding = rounding;
        //style.WindowBorderSize = 1.0f;
        //style.WindowMinSize = new Vec2(20.0f, 20.0f);
        //style.WindowTitleAlign = new Vec2(0.5f, 0.5f);
        //style.WindowMenuButtonPosition = ImGuiDir.Right;
        //style.ChildRounding = 0.0f;
        //style.ChildBorderSize = 1.0f;
        //style.PopupRounding = 0.0f;
        //style.PopupBorderSize = 1.0f;
        //style.FramePadding = new Vec2(20.0f, 3.400000095367432f);
        //style.FrameRounding = rounding;
        //style.FrameBorderSize = 0.0f;
        //style.ItemSpacing = new Vec2(4.300000190734863f, 5.5f);
        //style.ItemInnerSpacing = new Vec2(7.099999904632568f, 1.799999952316284f);
        //style.CellPadding = new Vec2(12.10000038146973f, 9.199999809265137f);
        //style.IndentSpacing = 0.0f;
        //style.ColumnsMinSpacing = 4.900000095367432f;
        //style.ScrollbarSize = 11.60000038146973f;
        //style.ScrollbarRounding = rounding;
        //style.GrabMinSize = 3.700000047683716f;
        //style.GrabRounding = rounding;
        //style.TabRounding = 0.0f;
        //style.TabBorderSize = 0.0f;
        //style.TabMinWidthForCloseButton = 0.0f;
        //style.ColorButtonPosition = ImGuiDir.Right;
        //style.ButtonTextAlign = new Vec2(0.5f, 0.5f);
        //style.SelectableTextAlign = new Vec2(0.0f, 0.0f);

        style.Colors[(int)ImGuiCol.Text] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vec4(0.2745098173618317f, 0.3176470696926117f, 0.4509803950786591f, 1.0f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vec4(0.09411764889955521f, 0.1019607856869698f, 0.1176470592617989f, 1.0f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.Border] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vec4(0.1137254908680916f, 0.125490203499794f, 0.1529411822557449f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vec4(0.09803921729326248f, 0.105882354080677f, 0.1215686276555061f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.CheckMark] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vec4(1.0f, 0.7960784435272217f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.Button] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vec4(0.1803921610116959f, 0.1882352977991104f, 0.196078434586525f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vec4(0.1529411822557449f, 0.1529411822557449f, 0.1529411822557449f, 1.0f);
        style.Colors[(int)ImGuiCol.Header] = new Vec4(0.1411764770746231f, 0.1647058874368668f, 0.2078431397676468f, 1.0f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vec4(0.105882354080677f, 0.105882354080677f, 0.105882354080677f, 1.0f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.Separator] = new Vec4(0.1294117718935013f, 0.1490196138620377f, 0.1921568661928177f, 1.0f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vec4(0.1568627506494522f, 0.1843137294054031f, 0.250980406999588f, 1.0f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vec4(0.1568627506494522f, 0.1843137294054031f, 0.250980406999588f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vec4(0.1450980454683304f, 0.1450980454683304f, 0.1450980454683304f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.Tab] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.TabSelected] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLines] = new Vec4(0.5215686559677124f, 0.6000000238418579f, 0.7019608020782471f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vec4(0.03921568766236305f, 0.9803921580314636f, 0.9803921580314636f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vec4(0.8823529481887817f, 0.7960784435272217f, 0.5607843399047852f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vec4(0.95686274766922f, 0.95686274766922f, 0.95686274766922f, 1.0f);
        style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderLight] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBg] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vec4(0.09803921729326248f, 0.105882354080677f, 0.1215686276555061f, 1.0f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vec4(0.9372549057006836f, 0.9372549057006836f, 0.9372549057006836f, 1.0f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new Vec4(0.4980392158031464f, 0.5137255191802979f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.NavHighlight] = new Vec4(0.2666666805744171f, 0.2901960909366608f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vec4(0.4980392158031464f, 0.5137255191802979f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vec4(0.196078434586525f, 0.1764705926179886f, 0.5450980663299561f, 0.501960813999176f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vec4(0.196078434586525f, 0.1764705926179886f, 0.5450980663299561f, 0.501960813999176f);

        //style.Colors[(int)ImGuiCol.ButtonActive] = new Vec4(0.30980393f, 0.30980393f, 0.30980393f, 1.0f);
        //style.Colors[(int)ImGuiCol.TextDisabled] = new Vec4(0.2745098173618317f, 0.3176470696926117f, 0.4509803950786591f, 1.0f);
        //style.Colors[(int)ImGuiCol.WindowBg] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        //style.Colors[(int)ImGuiCol.ChildBg] = new Vec4(0.09411764889955521f, 0.1019607856869698f, 0.1176470592617989f, 1.0f);
        //style.Colors[(int)ImGuiCol.PopupBg] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        //style.Colors[(int)ImGuiCol.Border] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        //style.Colors[(int)ImGuiCol.BorderShadow] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        //style.Colors[(int)ImGuiCol.FrameBg] = new Vec4(0f, 0f, 0f, 1.0f);
        //style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        //style.Colors[(int)ImGuiCol.FrameBgActive] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        //style.Colors[(int)ImGuiCol.TitleBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        //style.Colors[(int)ImGuiCol.TitleBgActive] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        //style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        //style.Colors[(int)ImGuiCol.MenuBarBg] = new Vec4(0.09803921729326248f, 0.105882354080677f, 0.1215686276555061f, 1.0f);
        //style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        //style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        //style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vec4(0.1568627506494522f, 0.168627455830574f, 0.1921568661928177f, 1.0f);
        //style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        //style.Colors[(int)ImGuiCol.CheckMark] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        //style.Colors[(int)ImGuiCol.SliderGrab] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        //style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vec4(1.0f, 0.7960784435272217f, 0.4980392158031464f, 1.0f);
        //style.Colors[(int)ImGuiCol.Button] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        //style.Colors[(int)ImGuiCol.ButtonHovered] = new Vec4(0.1803921610116959f, 0.1882352977991104f, 0.196078434586525f, 1.0f);
        //style.Colors[(int)ImGuiCol.ButtonActive] = new Vec4(0.1529411822557449f, 0.1529411822557449f, 0.1529411822557449f, 1.0f);
        //style.Colors[(int)ImGuiCol.Header] = new Vec4(0.1411764770746231f, 0.1647058874368668f, 0.2078431397676468f, 1.0f);
        //style.Colors[(int)ImGuiCol.HeaderHovered] = new Vec4(0.105882354080677f, 0.105882354080677f, 0.105882354080677f, 1.0f);
        //style.Colors[(int)ImGuiCol.HeaderActive] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        //style.Colors[(int)ImGuiCol.Separator] = new Vec4(0.1294117718935013f, 0.1490196138620377f, 0.1921568661928177f, 1.0f);
        //style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vec4(0.1568627506494522f, 0.1843137294054031f, 0.250980406999588f, 1.0f);
        //style.Colors[(int)ImGuiCol.SeparatorActive] = new Vec4(0.1568627506494522f, 0.1843137294054031f, 0.250980406999588f, 1.0f);
        //style.Colors[(int)ImGuiCol.ResizeGrip] = new Vec4(0.1450980454683304f, 0.1450980454683304f, 0.1450980454683304f, 1.0f);
        //style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vec4(0.9725490212440491f, 1.0f, 0.4980392158031464f, 1.0f);
        //style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f);
        //style.Colors[(int)ImGuiCol.Tab] = new Vec4(0.0784313753247261f, 0.08627451211214066f, 0.1019607856869698f, 1.0f);
        //style.Colors[(int)ImGuiCol.TabHovered] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        //style.Colors[(int)ImGuiCol.TabSelected] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        //style.Colors[(int)ImGuiCol.PlotLines] = new Vec4(0.5215686559677124f, 0.6000000238418579f, 0.7019608020782471f, 1.0f);
        //style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vec4(0.03921568766236305f, 0.9803921580314636f, 0.9803921580314636f, 1.0f);
        //style.Colors[(int)ImGuiCol.PlotHistogram] = new Vec4(0.8823529481887817f, 0.7960784435272217f, 0.5607843399047852f, 1.0f);
        //style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vec4(0.95686274766922f, 0.95686274766922f, 0.95686274766922f, 1.0f);
        //style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        //style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vec4(0.0470588244497776f, 0.05490196123719215f, 0.07058823853731155f, 1.0f);
        //style.Colors[(int)ImGuiCol.TableBorderLight] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f);
        //style.Colors[(int)ImGuiCol.TableRowBg] = new Vec4(0.1176470592617989f, 0.1333333402872086f, 0.1490196138620377f, 1.0f);
        //style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vec4(0.09803921729326248f, 0.105882354080677f, 0.1215686276555061f, 1.0f);
        //style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vec4(0.9372549057006836f, 0.9372549057006836f, 0.9372549057006836f, 1.0f);
        //style.Colors[(int)ImGuiCol.DragDropTarget] = new Vec4(0.4980392158031464f, 0.5137255191802979f, 1.0f, 1.0f);
        //style.Colors[(int)ImGuiCol.NavHighlight] = new Vec4(0.2666666805744171f, 0.2901960909366608f, 1.0f, 1.0f);
        //style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vec4(0.4980392158031464f, 0.5137255191802979f, 1.0f, 1.0f);
        //style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vec4(0.196078434586525f, 0.1764705926179886f, 0.5450980663299561f, 0.501960813999176f);
        //style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vec4(0.196078434586525f, 0.1764705926179886f, 0.5450980663299561f, 0.501960813999176f);

        //  High Contrast
        //style.Colors[(int)ImGuiCol.ButtonActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // ButtonActive - White
        //style.Colors[(int)ImGuiCol.TextDisabled] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // TextDisabled - Gray
        //style.Colors[(int)ImGuiCol.WindowBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // WindowBg - Black
        //style.Colors[(int)ImGuiCol.ChildBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // ChildBg - Black
        //style.Colors[(int)ImGuiCol.PopupBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // PopupBg - Black
        //style.Colors[(int)ImGuiCol.Border] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // Border - White
        //style.Colors[(int)ImGuiCol.BorderShadow] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // BorderShadow - Black
        //style.Colors[(int)ImGuiCol.FrameBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // FrameBg - Black
        //style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // FrameBgHovered - Gray
        //style.Colors[(int)ImGuiCol.FrameBgActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // FrameBgActive - White
        //style.Colors[(int)ImGuiCol.TitleBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // TitleBg - Black
        //style.Colors[(int)ImGuiCol.TitleBgActive] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // TitleBgActive - Black
        //style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // TitleBgCollapsed - Black
        //style.Colors[(int)ImGuiCol.MenuBarBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // MenuBarBg - Black
        //style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // ScrollbarBg - Black
        //style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // ScrollbarGrab - White
        //style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // ScrollbarGrabHovered - Gray
        //style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // ScrollbarGrabActive - White
        //style.Colors[(int)ImGuiCol.CheckMark] = new Vec4(1.0f, 1.0f, 0.0f, 1.0f); // CheckMark - Yellow
        //style.Colors[(int)ImGuiCol.SliderGrab] = new Vec4(1.0f, 1.0f, 0.0f, 1.0f); // SliderGrab - Yellow
        //style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vec4(1.0f, 0.5f, 0.0f, 1.0f); // SliderGrabActive - Orange
        //style.Colors[(int)ImGuiCol.Button] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // Button - Black
        //style.Colors[(int)ImGuiCol.ButtonHovered] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // ButtonHovered - Gray
        //style.Colors[(int)ImGuiCol.ButtonActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // ButtonActive - White
        //style.Colors[(int)ImGuiCol.Header] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // Header - Black
        //style.Colors[(int)ImGuiCol.HeaderHovered] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // HeaderHovered - Gray
        //style.Colors[(int)ImGuiCol.HeaderActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // HeaderActive - White
        //style.Colors[(int)ImGuiCol.Separator] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // Separator - White
        //style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // SeparatorHovered - Gray
        //style.Colors[(int)ImGuiCol.SeparatorActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // SeparatorActive - White
        //style.Colors[(int)ImGuiCol.ResizeGrip] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // ResizeGrip - Gray
        //style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vec4(1.0f, 1.0f, 0.0f, 1.0f); // ResizeGripHovered - Yellow
        //style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // ResizeGripActive - White
        //style.Colors[(int)ImGuiCol.Tab] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // Tab - Black
        //style.Colors[(int)ImGuiCol.TabHovered] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // TabHovered - Gray
        //style.Colors[(int)ImGuiCol.TabSelected] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // TabSelected - White
        //style.Colors[(int)ImGuiCol.PlotLines] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // PlotLines - White
        //style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vec4(0.0f, 1.0f, 1.0f, 1.0f); // PlotLinesHovered - Cyan
        //style.Colors[(int)ImGuiCol.PlotHistogram] = new Vec4(1.0f, 1.0f, 0.0f, 1.0f); // PlotHistogram - Yellow
        //style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // PlotHistogramHovered - White
        //style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // TableHeaderBg - Black
        //style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // TableBorderStrong - Black
        //style.Colors[(int)ImGuiCol.TableBorderLight] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // TableBorderLight - White
        //style.Colors[(int)ImGuiCol.TableRowBg] = new Vec4(0.0f, 0.0f, 0.0f, 1.0f); // TableRowBg - Black
        //style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // TableRowBgAlt - Gray
        //style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vec4(1.0f, 1.0f, 1.0f, 1.0f); // TextSelectedBg - White
        //style.Colors[(int)ImGuiCol.DragDropTarget] = new Vec4(1.0f, 1.0f, 0.0f, 1.0f); // DragDropTarget - Yellow
        //style.Colors[(int)ImGuiCol.NavHighlight] = new Vec4(0.0f, 0.0f, 1.0f, 1.0f); // NavHighlight - Blue
        //style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vec4(1.0f, 1.0f, 0.0f, 1.0f); // NavWindowingHighlight - Yellow
        //style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vec4(0.5f, 0.5f, 0.5f, 0.5f); // NavWindowingDimBg - Semi-transparent Gray
        //style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vec4(0.5f, 0.5f, 0.5f, 0.5f); // ModalWindowDimBg - Semi-transparent Gray

        //  Mcdonalds
        //style.Colors[(int)ImGuiCol.ButtonActive] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // ButtonActive - Hot Rod Red
        //style.Colors[(int)ImGuiCol.TextDisabled] = new Vec4(0.5f, 0.5f, 0.5f, 1.0f); // TextDisabled - Gray
        //style.Colors[(int)ImGuiCol.WindowBg] = new Vec4(0.1f, 0.1f, 0.1f, 1.0f); // WindowBg - Dark Gray
        //style.Colors[(int)ImGuiCol.ChildBg] = new Vec4(0.1f, 0.1f, 0.1f, 1.0f); // ChildBg - Dark Gray
        //style.Colors[(int)ImGuiCol.PopupBg] = new Vec4(0.1f, 0.1f, 0.1f, 1.0f); // PopupBg - Dark Gray
        //style.Colors[(int)ImGuiCol.Border] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // Border - Hot Rod Red
        //style.Colors[(int)ImGuiCol.BorderShadow] = new Vec4(0.1f, 0.1f, 0.1f, 1.0f); // BorderShadow - Dark Gray
        //style.Colors[(int)ImGuiCol.FrameBg] = new Vec4(0.1f, 0.1f, 0.1f, 1.0f); // FrameBg - Dark Gray
        //style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // FrameBgHovered - Hot Rod Red
        //style.Colors[(int)ImGuiCol.FrameBgActive] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // FrameBgActive - Hot Rod Red
        //style.Colors[(int)ImGuiCol.TitleBg] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // TitleBg - Hot Rod Red
        //style.Colors[(int)ImGuiCol.TitleBgActive] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // TitleBgActive - Hot Rod Red
        //style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vec4(0.1f, 0.1f, 0.1f, 1.0f); // TitleBgCollapsed - Dark Gray
        //style.Colors[(int)ImGuiCol.MenuBarBg] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // MenuBarBg - Hot Rod Red
        //style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vec4(0.1f, 0.1f, 0.1f, 1.0f); // ScrollbarBg - Dark Gray
        //style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // ScrollbarGrab - Gold
        //style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // ScrollbarGrabHovered - Gold
        //style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // ScrollbarGrabActive - Gold
        //style.Colors[(int)ImGuiCol.CheckMark] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // CheckMark - Gold
        //style.Colors[(int)ImGuiCol.SliderGrab] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // SliderGrab - Gold
        //style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // SliderGrabActive - Gold
        //style.Colors[(int)ImGuiCol.Button] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // Button - Hot Rod Red
        //style.Colors[(int)ImGuiCol.ButtonHovered] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // ButtonHovered - Gold
        //style.Colors[(int)ImGuiCol.ButtonActive] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // ButtonActive - Hot Rod Red
        //style.Colors[(int)ImGuiCol.Header] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // Header - Hot Rod Red
        //style.Colors[(int)ImGuiCol.HeaderHovered] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // HeaderHovered - Gold
        //style.Colors[(int)ImGuiCol.HeaderActive] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // HeaderActive - Hot Rod Red
        //style.Colors[(int)ImGuiCol.Separator] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // Separator - Gold
        //style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // SeparatorHovered - Gold
        //style.Colors[(int)ImGuiCol.SeparatorActive] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // SeparatorActive - Gold
        //style.Colors[(int)ImGuiCol.ResizeGrip] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // ResizeGrip - Gold
        //style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // ResizeGripHovered - Gold
        //style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // ResizeGripActive - Gold
        //style.Colors[(int)ImGuiCol.Tab] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // Tab - Hot Rod Red
        //style.Colors[(int)ImGuiCol.TabHovered] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // TabHovered - Gold
        //style.Colors[(int)ImGuiCol.TabSelected] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // TabSelected - Gold
        //style.Colors[(int)ImGuiCol.PlotLines] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // PlotLines - Gold
        //style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // PlotLinesHovered - Gold
        //style.Colors[(int)ImGuiCol.PlotHistogram] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // PlotHistogram - Gold
        //style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // PlotHistogramHovered - Gold
        //style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // TableHeaderBg - Hot Rod Red
        //style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // TableBorderStrong - Hot Rod Red
        //style.Colors[(int)ImGuiCol.TableBorderLight] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // TableBorderLight - Gold
        //style.Colors[(int)ImGuiCol.TableRowBg] = new Vec4(0.8f, 0.0f, 0.0f, 1.0f); // TableRowBg - Hot Rod Red
        //style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // TableRowBgAlt - Gold
        //style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // TextSelectedBg - Gold
        //style.Colors[(int)ImGuiCol.DragDropTarget] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // DragDropTarget - Gold
        //style.Colors[(int)ImGuiCol.NavHighlight] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // NavHighlight - Gold
        //style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vec4(1.0f, 0.84f, 0.0f, 1.0f); // NavWindowingHighlight - Gold
        //style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vec4(0.8f, 0.0f, 0.0f, 0.5f); // NavWindowingDimBg - Semi-transparent Hot Rod Red
        //style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vec4(0.8f, 0.0f, 0.0f, 0.5f); // ModalWindowDimBg - Semi-transparent Hot Rod Red




    }

    /// <summary>
    /// Asks ImGui for the generated geometry data and sends it to the graphics pipeline, should be called after the UI is drawn using ImGui.** calls
    /// </summary>
    public virtual void AfterLayout()
    {
        ImGui.Render();

        unsafe { RenderDrawData(ImGui.GetDrawData()); }
    }

    #endregion ImGuiRenderer

    #region Setup & Update

    /// <summary>
    /// Setup key input event handler.
    /// </summary>
    protected virtual void SetupInput()
    {
        var io = ImGui.GetIO();

        // MonoGame-specific //////////////////////
        _game.Window.TextInput += (s, a) =>
        {
            if (a.Character == '\t') return;
            io.AddInputCharacter(a.Character);
        };

        ///////////////////////////////////////////

        // FNA-specific ///////////////////////////
        //TextInputEXT.TextInput += c =>
        //{
        //    if (c == '\t') return;

        //    ImGui.GetIO().AddInputCharacter(c);
        //};
        ///////////////////////////////////////////
    }

    /// <summary>
    /// Updates the <see cref="Effect" /> to the current matrices and texture
    /// </summary>
    protected virtual Effect UpdateEffect(Texture2D texture)
    {
        _effect = _effect ?? new BasicEffect(_graphicsDevice);

        var io = ImGui.GetIO();

        _effect.World = Matrix.Identity;
        _effect.View = Matrix.Identity;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
        _effect.TextureEnabled = true;
        _effect.Texture = texture;
        _effect.VertexColorEnabled = true;

        return _effect;
    }

    /// <summary>
    /// Sends XNA input state to ImGui
    /// </summary>
    protected virtual void UpdateInput()
    {
        if (!_game.IsActive) return;

        var io = ImGui.GetIO();

        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed);
        io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed);

        io.AddMouseWheelEvent(
            (mouse.HorizontalScrollWheelValue - _horizontalScrollWheelValue) / WHEEL_DELTA,
            (mouse.ScrollWheelValue - _scrollWheelValue) / WHEEL_DELTA);
        _scrollWheelValue = mouse.ScrollWheelValue;
        _horizontalScrollWheelValue = mouse.HorizontalScrollWheelValue;

        foreach (var key in _allKeys)
        {
            if (TryMapKeys(key, out ImGuiKey imguikey))
            {
                io.AddKeyEvent(imguikey, keyboard.IsKeyDown(key));
            }
        }

        io.DisplaySize = new Vec2(_graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new Vec2(1f, 1f);
    }

    private bool TryMapKeys(Keys key, out ImGuiKey imguikey)
    {
        //Special case not handed in the switch...
        //If the actual key we put in is "None", return none and true. 
        //otherwise, return none and false.
        if (key == Keys.None)
        {
            imguikey = ImGuiKey.None;
            return true;
        }

        imguikey = key switch
        {
            Keys.Back => ImGuiKey.Backspace,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Enter => ImGuiKey.Enter,
            Keys.CapsLock => ImGuiKey.CapsLock,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Space => ImGuiKey.Space,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.End => ImGuiKey.End,
            Keys.Home => ImGuiKey.Home,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PrintScreen => ImGuiKey.PrintScreen,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            >= Keys.D0 and <= Keys.D9 => ImGuiKey._0 + (key - Keys.D0),
            >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
            >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
            Keys.Multiply => ImGuiKey.KeypadMultiply,
            Keys.Add => ImGuiKey.KeypadAdd,
            Keys.Subtract => ImGuiKey.KeypadSubtract,
            Keys.Decimal => ImGuiKey.KeypadDecimal,
            Keys.Divide => ImGuiKey.KeypadDivide,
            >= Keys.F1 and <= Keys.F24 => ImGuiKey.F1 + (key - Keys.F1),
            Keys.NumLock => ImGuiKey.NumLock,
            Keys.Scroll => ImGuiKey.ScrollLock,
            Keys.LeftShift => ImGuiKey.ModShift,
            Keys.LeftControl => ImGuiKey.ModCtrl,
            Keys.LeftAlt => ImGuiKey.ModAlt,
            Keys.OemSemicolon => ImGuiKey.Semicolon,
            Keys.OemPlus => ImGuiKey.Equal,
            Keys.OemComma => ImGuiKey.Comma,
            Keys.OemMinus => ImGuiKey.Minus,
            Keys.OemPeriod => ImGuiKey.Period,
            Keys.OemQuestion => ImGuiKey.Slash,
            Keys.OemTilde => ImGuiKey.GraveAccent,
            Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
            Keys.OemCloseBrackets => ImGuiKey.RightBracket,
            Keys.OemPipe => ImGuiKey.Backslash,
            Keys.OemQuotes => ImGuiKey.Apostrophe,
            Keys.BrowserBack => ImGuiKey.AppBack,
            Keys.BrowserForward => ImGuiKey.AppForward,
            _ => ImGuiKey.None,
        };

        return imguikey != ImGuiKey.None;
    }

    #endregion Setup & Update

    #region Internals

    /// <summary>
    /// Gets the geometry as set up by ImGui and sends it to the graphics device
    /// </summary>
    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
        var lastViewport = _graphicsDevice.Viewport;
        var lastScissorBox = _graphicsDevice.ScissorRectangle;
        var lastRasterizer = _graphicsDevice.RasterizerState;
        var lastDepthStencil = _graphicsDevice.DepthStencilState;
        var lastBlendFactor = _graphicsDevice.BlendFactor;
        var lastBlendState = _graphicsDevice.BlendState;
        var lastSamplerState = _graphicsDevice.SamplerStates[0];

        _graphicsDevice.BlendFactor = XnaColor.White;
        _graphicsDevice.BlendState = BlendState.NonPremultiplied;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointWrap;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        // Setup projection
        _graphicsDevice.Viewport = new Viewport(0, 0, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);

        UpdateBuffers(drawData);

        RenderCommandLists(drawData);

        // Restore modified state
        _graphicsDevice.Viewport = lastViewport;
        _graphicsDevice.ScissorRectangle = lastScissorBox;
        _graphicsDevice.RasterizerState = lastRasterizer;
        _graphicsDevice.DepthStencilState = lastDepthStencil;
        _graphicsDevice.BlendState = lastBlendState;
        _graphicsDevice.BlendFactor = lastBlendFactor;
        _graphicsDevice.SamplerStates[0] = lastSamplerState;
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        // Expand buffers if we need more room
        if (drawData.TotalVtxCount > _vertexBufferSize)
        {
            _vertexBuffer?.Dispose();

            _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
            _vertexBuffer = new VertexBuffer(_graphicsDevice, DrawVertDeclaration.Declaration, _vertexBufferSize, BufferUsage.None);
            _vertexData = new byte[_vertexBufferSize * DrawVertDeclaration.Size];
        }

        if (drawData.TotalIdxCount > _indexBufferSize)
        {
            _indexBuffer?.Dispose();

            _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
            _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
            _indexData = new byte[_indexBufferSize * sizeof(ushort)];
        }

        // Copy ImGui's vertices and indices to a set of managed byte arrays
        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            fixed (void* vtxDstPtr = &_vertexData[vtxOffset * DrawVertDeclaration.Size])
            fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
            {
                Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        // Copy the managed byte arrays to the gpu vertex- and index buffers
        _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
        _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
    }

    private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
    {
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                if (drawCmd.ElemCount == 0)
                {
                    continue;
                }

                if (!_loadedTextures.ContainsKey(drawCmd.TextureId))
                {
                    throw new InvalidOperationException($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");
                }

                _graphicsDevice.ScissorRectangle = new Rectangle(
                    (int)drawCmd.ClipRect.X,
                    (int)drawCmd.ClipRect.Y,
                    (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                    (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                var effect = UpdateEffect(_loadedTextures[drawCmd.TextureId]);

                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

#pragma warning disable CS0618 // // FNA does not expose an alternative method.
                    _graphicsDevice.DrawIndexedPrimitives(
                        primitiveType: PrimitiveType.TriangleList,
                        baseVertex: (int)drawCmd.VtxOffset + vtxOffset,
                        minVertexIndex: 0,
                        numVertices: cmdList.VtxBuffer.Size,
                        startIndex: (int)drawCmd.IdxOffset + idxOffset,
                        primitiveCount: (int)drawCmd.ElemCount / 3
                    );
#pragma warning restore CS0618
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    #endregion Internals
}
