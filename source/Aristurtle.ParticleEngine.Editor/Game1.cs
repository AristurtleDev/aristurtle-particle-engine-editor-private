// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Data;
using System.Text.Json;
using Aristurtle.ParticleEngine.Data;
using Aristurtle.ParticleEngine.Editor.Factories;
using Aristurtle.ParticleEngine.Editor.Graphics;
using Aristurtle.ParticleEngine.Editor.Gui;
using Aristurtle.ParticleEngine.Editor.IO;
using Aristurtle.ParticleEngine.Modifiers;
using Aristurtle.ParticleEngine.Modifiers.Containers;
using Aristurtle.ParticleEngine.Modifiers.Interpolators;
using Aristurtle.ParticleEngine.Profiles;
using Aristurtle.ParticleEngine.Serialization.Json;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Aristurtle.ParticleEngine.Editor
{
    public class Game1 : Game
    {
        private const string WINDOW_TITLE = "Turtle Particle Engine: Editor {0} | {1:F3} ms/Frame | {2:F1} FPS";
        private const string VERSION = "0.0.1";

        //-------------------------------------------------------------------------------------------------------------
        //  Project files
        //-------------------------------------------------------------------------------------------------------------
        private readonly string _defaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        private string _projectName = "ParticleEffectProject";
        private string _projectDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        private string _projectFile = string.Empty;

        //-------------------------------------------------------------------------------------------------------------
        //  Particle effect
        //-------------------------------------------------------------------------------------------------------------
        private ParticleEffect _particleEffect;
        private ParticleEmitter _emitter;
        private Modifier _modifier;
        private List<Interpolator> _interpolators;
        private Interpolator _interpolator;
        private readonly ParticleEffectRenderer _particleRenderer;
        private readonly string[] _particleValueKindNames;
        private readonly string[] _circleRadiationNames;
        private readonly string[] _renderingOrderNames;

        //-------------------------------------------------------------------------------------------------------------
        //  ImGui Stuff
        //-------------------------------------------------------------------------------------------------------------
        private ImGuiRenderer _imguiRenderer;

        //-------------------------------------------------------------------------------------------------------------
        //  Layout
        //-------------------------------------------------------------------------------------------------------------
        private Vec2 _mainMenuBarSize;
        private Rectangle _particleEffectWindowRect;
        private Rectangle _modifiersWindowRect;

        //-------------------------------------------------------------------------------------------------------------
        //  MonoGame Stuff
        //-------------------------------------------------------------------------------------------------------------
        private SpriteBatch _spriteBatch;
        private readonly GraphicsDeviceManager _graphics;
        private Texture2D _checkerBoardTexture;
        private Rectangle _checkerBoardRectangle;
        private bool _emitOnClick;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.DeviceCreated += OnGraphicsDeviceCreated;
            _graphics.DeviceReset += OnGraphicsDeviceReset;
            _graphics.ApplyChanges();


            Window.AllowUserResizing = false;
            Window.ClientSizeChanged += OnClientSizeChanged;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _particleValueKindNames = Enum.GetNames(typeof(ParticleValueKind));
            _circleRadiationNames = Enum.GetNames(typeof(CircleRadiation));
            _renderingOrderNames = Enum.GetNames(typeof(ParticleRenderingOrder));
            _particleRenderer = new ParticleEffectRenderer();
        }

        private void OnGraphicsDeviceReset(object sender, EventArgs e)
        {
            RecalculateCheckerboardDestination();
        }

        private void OnGraphicsDeviceCreated(object sender, EventArgs e)
        {
            RecalculateCheckerboardDestination();
        }

        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            if (Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0)
            {
                RecalculateCheckerboardDestination();
            }
        }

        private void RecalculateCheckerboardDestination()
        {
            if (_checkerBoardTexture is null) { return; }

            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;
            int textureWidth = _checkerBoardTexture.Width;
            float scaleFactor = Math.Max((float)screenWidth / textureWidth, (float)screenHeight / textureWidth);
            int scale = (int)(textureWidth * scaleFactor);
            _checkerBoardRectangle.Width = _checkerBoardRectangle.Height = scale;
        }

        protected override void Initialize()
        {
            base.Initialize();

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //  Create the checkerboard background
            int textureSize = 256;
            int squareSize = 8;
            XnaColor light = new XnaColor(115, 128, 141);
            XnaColor dark = new XnaColor(110, 122, 135);
            XnaColor[] colorData = new XnaColor[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    int index = y * textureSize + x;
                    bool alternateColor = (x / squareSize + y / squareSize) % 2 == 0;
                    colorData[index] = alternateColor ? dark : light;
                }
            }
            _checkerBoardTexture = new Texture2D(GraphicsDevice, textureSize, textureSize);
            _checkerBoardTexture.SetData(colorData);
            RecalculateCheckerboardDestination();

            _imguiRenderer = new ImGuiRenderer(this);
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        }

        private MouseState _previousMouse;
        private MouseState _currentMouse;

        protected override void Update(GameTime gameTime)
        {
            _previousMouse = _currentMouse;
            _currentMouse = Mouse.GetState();

            if (_particleEffect is not null)
            {
                _particleEffect.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            if (_emitOnClick && _currentMouse.LeftButton == ButtonState.Pressed)
            {
                if (!_particleEffectWindowRect.Contains(_currentMouse.Position) && !_modifiersWindowRect.Contains(_currentMouse.Position))
                {
                    Vec2 point1 = new Vec2(_previousMouse.Position.X, _previousMouse.Position.Y);
                    Vec2 point2 = new Vec2(_currentMouse.Position.X, _currentMouse.Position.Y);
                    LineSegment line = new LineSegment(point1, point2);
                    _particleEffect.Trigger(line, 0.0f);

                }
            }
        }

        protected override unsafe void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(XnaColor.Black);

            #region Draw Checkerboard Background

            _spriteBatch?.Begin(samplerState: SamplerState.PointWrap);
            _spriteBatch?.Draw(_checkerBoardTexture, _checkerBoardRectangle, XnaColor.White);
            _spriteBatch?.End();

            #endregion Draw Checkerboard Background

            #region Draw Particle Effect

            if (_particleEffect is not null)
            {
                _spriteBatch?.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);
                _particleRenderer.Draw(_spriteBatch, _particleEffect);
                _spriteBatch?.End();
            }

            #endregion Draw Particle Effect

            #region Draw ImGui Interface

            float frameRate;
            ImGuiStylePtr style;
            ImGuiIOPtr io;

            _imguiRenderer?.BeforeLayout(gameTime);
            style = ImGui.GetStyle();
            io = ImGui.GetIO();

            DrawMainMenuBar(style, io);

            if (_particleEffect is null)
            {
                DrawStartWindow(style, io);
            }
            else
            {
                DrawDockSpace(style, io);
                DrawEmitterList(style, io);
                DrawEmitterModifiers(style, io);
            }

            frameRate = io.Framerate;
            Window.Title = $"Turtle Particle Engine: Editor v0.0.1 | {1000f / ImGui.GetIO().Framerate:F3} ms/Frame | {ImGui.GetIO().Framerate:F1} FPS";
            _imguiRenderer?.AfterLayout();

            #endregion Draw ImGui Interface

            Window.Title = string.Format(WINDOW_TITLE, VERSION, 1000.0f / frameRate, frameRate);
        }


        #region ImGui Windows

        private void DrawDockSpace(ImGuiStylePtr style, ImGuiIOPtr io)
        {
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None;
            windowFlags |= ImGuiWindowFlags.NoTitleBar;
            windowFlags |= ImGuiWindowFlags.NoCollapse;
            windowFlags |= ImGuiWindowFlags.NoResize;
            windowFlags |= ImGuiWindowFlags.NoMove;
            windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus;
            windowFlags |= ImGuiWindowFlags.NoNavFocus;
            windowFlags |= ImGuiWindowFlags.NoBackground;

            Vec2 windowPos = Vec2.Zero;
            windowPos.Y = _mainMenuBarSize.Y;

            Vec2 windowSize = io.DisplaySize;
            windowSize.Y -= _mainMenuBarSize.Y;

            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(windowSize);
            ImGui.Begin("DockSpace", windowFlags);

            ImGuiDockNodeFlags dockNodeFlags = ImGuiDockNodeFlags.None;
            dockNodeFlags |= ImGuiDockNodeFlags.PassthruCentralNode;
            dockNodeFlags |= ImGuiDockNodeFlags.NoDockingOverCentralNode;

            uint id = ImGui.GetID("DockSpace");
            ImGui.DockSpace(id, Vec2.Zero, dockNodeFlags);

            ImGui.End();

        }

        private void DrawMainMenuBar(ImGuiStylePtr style, ImGuiIOPtr io)
        {
            if (ImGui.BeginMainMenuBar())
            {
                _mainMenuBarSize = ImGui.GetWindowSize();

                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New")) { CreateNewProject(); }
                    if (ImGui.MenuItem("Open...")) { OpenExistingProject(); }
                    if (ImGui.MenuItem("Save")) { SaveProject(); }
                    if (ImGui.MenuItem("Exit")) { Exit(); }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }
        }

        private unsafe void DrawStartWindow(ImGuiStylePtr style, ImGuiIOPtr io)
        {
            Vec2 windowPos = Vec2.Zero;
            windowPos.Y += _mainMenuBarSize.Y;

            Vec2 windowSize = io.DisplaySize;
            windowSize.Y -= _mainMenuBarSize.Y;

            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(windowSize);

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None;
            windowFlags |= ImGuiWindowFlags.NoCollapse;
            windowFlags |= ImGuiWindowFlags.NoResize;
            windowFlags |= ImGuiWindowFlags.NoMove;
            windowFlags |= ImGuiWindowFlags.NoScrollbar;
            windowFlags |= ImGuiWindowFlags.NoTitleBar;

            ImGui.Begin("##StartWindow", windowFlags);
            ImGui.PushFont(Fonts.TitleFont);
            ImGui.Text("Turtle Particle Engine");
            ImGui.PopFont();


            Vec2 buttonSize = new Vec2(400, 100);
            Vec2 topLeft;
            topLeft.X = io.DisplaySize.X * 0.5f - buttonSize.X * 0.5f;
            topLeft.Y = io.DisplaySize.Y * 0.5f - (buttonSize.Y * 2 + style.ItemSpacing.Y) * 0.5f;
            ImGui.PushFont(Fonts.HeadingFont);
            ImGui.SetCursorPos(topLeft);
            if (ImGui.Button("Create New Project##Button", buttonSize)) { CreateNewProject(); }
            ImGui.SetCursorPosX(topLeft.X);
            if (ImGui.Button("Open Existing Project##Button", buttonSize)) { OpenExistingProject(); }
            ImGui.PopFont();

            ImGui.End();
        }

        private unsafe void DrawEmitterList(ImGuiStylePtr style, ImGuiIOPtr io)
        {
            Vec2 contentSize = Vec2.Zero;
            Vec2 buttonSize = Vec2.Zero;
            Vec2 listBoxSize = Vec2.Zero;

            ImGui.SetNextWindowPos(new Vec2(0, _mainMenuBarSize.Y), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vec2(600, 1056), ImGuiCond.FirstUseEver);
            ImGui.Begin("Particle Emitters");

            #region Emitter List

            contentSize = ImGui.GetContentRegionAvail();
            buttonSize.X = contentSize.X * 0.5f - style.ItemSpacing.X * 0.5f;
            if (ImGui.Button("Add Emitter", buttonSize)) { AddNewEmitter(); }
            ImGui.SameLine();
            ImGui.BeginDisabled(_particleEffect?.Emitters.Count == 0);
            if (ImGui.Button("Remove Emitter", buttonSize)) { RemoveSelectedEmitter(); }
            ImGui.EndDisabled();
            listBoxSize = ImGui.GetContentRegionAvail();
            listBoxSize.Y = ImGui.CalcTextSize("Y").Y * 5 + style.ItemSpacing.Y * 5;
            ImGui.BeginDisabled(_particleEffect.Emitters.Count == 0);
            if (ImGui.BeginListBox("##Particle Emitters ListBox", listBoxSize))
            {
                for (int emitterIndex = 0; emitterIndex < _particleEffect.Emitters.Count; emitterIndex++)
                {
                    var emitter = _particleEffect.Emitters[emitterIndex];
                    var isEmitterSelected = _emitter == emitter;

                    if (ImGui.Selectable($"{emitter.Name}##{emitterIndex}", isEmitterSelected))
                    {
                        _emitter = emitter;
                    }
                }

                ImGui.EndListBox();
            }
            ImGui.EndDisabled();

            #endregion Emitter List


            if (_emitter is null)
            {
                ImGui.TextDisabled("Create or Select a Particle Emitter to view its properties!");
            }
            else
            {

                #region Emitter Properties

                ImGui.BeginTable("##EmitterPropertiesTable", 2, ImGuiTableFlags.SizingStretchSame);

                //  Emitter Name
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Name");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##Name##EmitterProperty", ref _emitter.Name, 256, ImGuiInputTextFlags.EnterReturnsTrue);
                OutlinePreviousItemIfActive();

                //  Emitter Texture
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Texture");
                ImGui.TableNextColumn();

                string preview = Path.GetFileNameWithoutExtension(_emitter.TextureKey);
                ImGui.SetNextItemWidth(-1);
                if (ImGui.BeginCombo("##Texture##EmitterProperty", preview))
                {
                    if (ImGui.Selectable("<New Texture>", _emitter.TextureKey == string.Empty))
                    {
                        ChooseTextureForEmitter();
                    }

                    ImGui.Separator();

                    foreach (string texture in _particleRenderer.Textures.Keys)
                    {
                        string label = Path.GetFileNameWithoutExtension(texture);
                        if (ImGui.Selectable(label, _emitter.TextureKey == texture))
                        {
                            _emitter.TextureKey = texture;
                        }
                    }
                    ImGui.EndCombo();
                }


                //  Auto Trigger
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Auto Trigger");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.Checkbox("##AutoTrigger##EmitterProperties", ref _emitter.AutoTrigger);
                OutlinePreviousItemIfActive();

                //  Auto Trigger Frequency
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Auto Trigger Frequency");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputFloat("##AutoTriggerFrequency##EmitterProperties", ref _emitter.AutoTriggerFrequency, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                OutlinePreviousItemIfActive();

                //  Capacity
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Capacity");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int capacity = _emitter.Capacity;
                if (ImGui.InputInt("##Capacity##EmitterProperties", ref capacity, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    _emitter.ChangeCapacity(capacity);
                }
                OutlinePreviousItemIfActive();

                //  Lifespan
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Lifespan");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputFloat("##Lifespan##EmitterProperties", ref _emitter.LifeSpan, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                OutlinePreviousItemIfActive();

                //  Rendering Order
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Rendering Order");
                ImGui.TableNextColumn();
                int renderOrder = (int)_emitter.RenderingOrder;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo($"##{nameof(ParticleEmitter)}RenderingOrder", ref renderOrder, _renderingOrderNames, _renderingOrderNames.Length))
                {
                    _emitter.RenderingOrder = (ParticleRenderingOrder)renderOrder;
                }
                OutlinePreviousItemIfActive();


                ImGui.EndTable();

                #endregion Emitter Properties

                #region Emitter Parameters

                ImGui.BeginTable("##EmitterParametersTable", 3, ImGuiTableFlags.SizingStretchSame);

                int kind = 0;

                //  Quantity
                ref ParticleInt32Parameter quantity = ref _emitter.Parameters.Quantity;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Quantity");
                ImGui.TableNextColumn();
                kind = (int)quantity.Kind;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##QuantityKind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
                {
                    quantity.Kind = (ParticleValueKind)kind;
                }
                OutlinePreviousItemIfActive();
                if (quantity.Kind == ParticleValueKind.Constant)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputInt("##QuantityStatic", ref quantity.Constant, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
                    OutlinePreviousItemIfActive();
                }
                else
                {
                    int[] range = new int[] { quantity.RandomMin, quantity.RandomMax };
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt2("##QuantityRandom", ref range[0], ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        quantity.RandomMin = range[0];
                        quantity.RandomMax = range[0];
                    }
                    OutlinePreviousItemIfActive();
                }

                //  Speed
                ref ParticleFloatParameter speed = ref _emitter.Parameters.Speed;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Speed");
                ImGui.TableNextColumn();
                kind = (int)speed.Kind;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##SpeedKind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
                {
                    speed.Kind = (ParticleValueKind)kind;
                }
                OutlinePreviousItemIfActive();
                if (speed.Kind == ParticleValueKind.Constant)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputFloat("##SpeedStatic", ref speed.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                    OutlinePreviousItemIfActive();
                }
                else
                {
                    Vec2 range = new Vec2(speed.RandomMin, speed.RandomMax);
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputFloat2("##SpeedRandom", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        speed.RandomMin = range.X;
                        speed.RandomMax = range.Y;
                    }
                    OutlinePreviousItemIfActive();
                }

                //  Color
                ref ParticleColorParameter color = ref _emitter.Parameters.Color;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Color");
                ImGui.TableNextColumn();
                kind = (int)color.Kind;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##ColorKind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
                {
                    color.Kind = (ParticleValueKind)kind;
                }
                OutlinePreviousItemIfActive();

                if (color.Kind == ParticleValueKind.Constant)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    var (r, g, b) = ColorUtilities.HslToRgb(color.Constant);
                    Vec3 rgb = new Vec3(r, g, b) / 255.0f;
                    if (ImGui.ColorEdit3($"##ColorStatic", ref rgb, ImGuiColorEditFlags.InputRGB))
                    {
                        var (h, s, l) = ColorUtilities.RgbToHsl(rgb);
                        color.Constant = new Vec3(h, s, l);
                    }
                    OutlinePreviousItemIfActive();
                }
                else
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    var (rMin, gMin, bMin) = ColorUtilities.HslToRgb(color.RandomMin);
                    Vec3 colorMin = new Vec3(rMin, gMin, bMin) / 255.0f;
                    if (ImGui.ColorEdit3($"##ColorRandomMin", ref colorMin, ImGuiColorEditFlags.InputRGB))
                    {
                        var (hMin, sMin, lMin) = ColorUtilities.RgbToHsl(colorMin);
                        color.RandomMin = new Vec3(hMin, sMin, lMin);
                    }
                    OutlinePreviousItemIfActive();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    var (rMax, gMax, bMax) = ColorUtilities.HslToRgb(color.RandomMax);
                    Vec3 colorMax = new Vec3(rMax, gMax, bMax) / 255.0f;
                    if (ImGui.ColorEdit3($"##ColorRandomMax", ref colorMax, ImGuiColorEditFlags.InputRGB))
                    {
                        var (hMax, sMax, lMax) = ColorUtilities.RgbToHsl(colorMax);
                        color.RandomMax = new Vec3(hMax, sMax, lMax);
                    }
                }

                //  Opacity
                ref ParticleFloatParameter opacity = ref _emitter.Parameters.Opacity;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Opacity");
                ImGui.TableNextColumn();
                kind = (int)opacity.Kind;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##OpacityKind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
                {
                    opacity.Kind = (ParticleValueKind)kind;
                }
                OutlinePreviousItemIfActive();
                if (opacity.Kind == ParticleValueKind.Constant)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputFloat("##OpacityStatic", ref opacity.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                    OutlinePreviousItemIfActive();
                }
                else
                {
                    Vec2 range = new Vec2(opacity.RandomMin, opacity.RandomMax);
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputFloat2("##OpacityRandom", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        opacity.RandomMin = range.X;
                        opacity.RandomMax = range.Y;
                    }
                }

                //  Scale
                ref ParticleFloatParameter scale = ref _emitter.Parameters.Scale;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Scale");
                ImGui.TableNextColumn();
                kind = (int)scale.Kind;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##ScaleKind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
                {
                    scale.Kind = (ParticleValueKind)kind;
                }
                OutlinePreviousItemIfActive();
                if (scale.Kind == ParticleValueKind.Constant)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputFloat("##ScaleStatic", ref scale.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                    OutlinePreviousItemIfActive();
                }
                else
                {
                    Vec2 range = new Vec2(scale.RandomMin, scale.RandomMax);
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputFloat2("##ScaleRandom", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        scale.RandomMin = range.X;
                        scale.RandomMax = range.Y;
                    }
                }

                //  Rotation
                ref ParticleFloatParameter rotation = ref _emitter.Parameters.Rotation;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Rotation");
                ImGui.TableNextColumn();
                kind = (int)rotation.Kind;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##RotationKind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
                {
                    rotation.Kind = (ParticleValueKind)kind;
                }
                OutlinePreviousItemIfActive();
                if (rotation.Kind == ParticleValueKind.Constant)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputFloat("##RotationStatic", ref rotation.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                    OutlinePreviousItemIfActive();
                }
                else
                {
                    Vec2 range = new Vec2(rotation.RandomMin, rotation.RandomMax);
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputFloat2("##RotationRandom", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        rotation.RandomMin = range.X;
                        rotation.RandomMax = range.Y;
                    }
                }

                //  Mass
                ref ParticleFloatParameter mass = ref _emitter.Parameters.Mass;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Mass");
                ImGui.TableNextColumn();
                kind = (int)mass.Kind;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo("##MassKind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
                {
                    mass.Kind = (ParticleValueKind)kind;
                }
                OutlinePreviousItemIfActive();
                if (mass.Kind == ParticleValueKind.Constant)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputFloat("##MassStatic", ref mass.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                    OutlinePreviousItemIfActive();
                }
                else
                {
                    Vec2 range = new Vec2(mass.RandomMin, mass.RandomMax);
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputFloat2("##MassRandom", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        mass.RandomMin = range.X;
                        mass.RandomMax = range.Y;
                    }
                }

                ImGui.EndTable();

                #endregion Emitter Parameters

                #region Emitter Profile

                ImGui.BeginTable("##EmitterProfileTable", 3, ImGuiTableFlags.SizingStretchSame);

                //  Profile Selection
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Profile Type");
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                int index = Array.IndexOf(ParticleProfileFactory.ProfileTypes, _emitter.Profile.GetType());
                if (ImGui.Combo("##ProfileTypeCombo", ref index, ParticleProfileFactory.ProfileTypeNames, ParticleProfileFactory.ProfileTypeNames.Length))
                {
                    Type newType = ParticleProfileFactory.ProfileTypes[index];
                    if (_emitter.Profile.GetType() != newType)
                    {
                        _emitter.Profile = ParticleProfileFactory.CreateProfile(newType);
                    }
                }

                switch (_emitter.Profile)
                {
                    case BoxFillProfile boxFillProfile:
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Width");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(BoxFillProfile)}Width", ref boxFillProfile.Width, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Height");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(BoxFillProfile)}Height", ref boxFillProfile.Height, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        break;

                    case BoxProfile boxProfile:
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Width");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(BoxProfile)}Width", ref boxProfile.Width, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Height");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(BoxProfile)}Height", ref boxProfile.Height, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        break;

                    case BoxUniformProfile boxUniformProfile:
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Width");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(BoxUniformProfile)}Width", ref boxUniformProfile.Width, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Height");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(BoxUniformProfile)}Height", ref boxUniformProfile.Height, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        break;

                    case CircleProfile circleProfile:
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Radiate");
                        ImGui.TableNextColumn();
                        int radiate = (int)circleProfile.Radiate;
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.Combo($"##{nameof(CircleProfile)}Radiate", ref radiate, _circleRadiationNames, _circleRadiationNames.Length))
                        {
                            circleProfile.Radiate = (CircleRadiation)radiate;
                        }
                        OutlinePreviousItemIfActive();
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Radius");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(CircleProfile)}Radius", ref circleProfile.Radius, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        break;

                    case LineProfile lineProfile:
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Axis");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat2($"##{nameof(LineProfile)}Axis", ref lineProfile.Axis, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Length");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(LineProfile)}Length", ref lineProfile.Length, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        break;

                    case RingProfile ringProfile:
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Radiate");
                        ImGui.TableNextColumn();
                        int ringRadiate = (int)ringProfile.Radiate;
                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.Combo($"##{nameof(RingProfile)}Radiate", ref ringRadiate, _circleRadiationNames, _circleRadiationNames.Length))
                        {
                            ringProfile.Radiate = (CircleRadiation)ringRadiate;
                        }
                        OutlinePreviousItemIfActive();
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Radius");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(RingProfile)}Radius", ref ringProfile.Radius, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        break;

                    case SprayProfile spriteProfile:
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Direction");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat2($"##{nameof(LineProfile)}Direction", ref spriteProfile.Direction, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TableNextColumn();
                        ImGui.Text("Spread");
                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(-1);
                        ImGui.InputFloat($"##{nameof(LineProfile)}Spread", ref spriteProfile.Spread, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                        OutlinePreviousItemIfActive();
                        break;
                }

                ImGui.EndTable();

                #endregion Emitter Profile
            }

            Vec2 windowSize = ImGui.GetWindowSize();
            Vec2 windowPos = ImGui.GetWindowPos();
            _particleEffectWindowRect = new Rectangle((int)windowPos.X, (int)windowPos.Y, (int)windowSize.X, (int)windowSize.Y);

            ImGui.End();
        }

        private void DrawEmitterModifiers(ImGuiStylePtr style, ImGuiIOPtr io)
        {
            ImGui.SetNextWindowPos(new Vec2(1500, _mainMenuBarSize.Y), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vec2(420, 1056), ImGuiCond.FirstUseEver);

            ImGui.Begin("Modifiers");

            if (_emitter is null)
            {
                ImGui.TextDisabled("Create or Select a Particle Emitter to view its modifiers!");
            }
            else
            {
                #region Modifier List

                Vec2 contentSize = Vec2.Zero;
                Vec2 buttonSize = Vec2.Zero;

                contentSize = ImGui.GetContentRegionAvail();
                buttonSize.X = contentSize.X * 0.5f - style.ItemSpacing.X * 0.5f;
                if (ImGui.Button("Add Modifier", buttonSize))
                {
                    ImGui.OpenPopup("Choose Modifier Modal");
                }
                ImGui.SameLine();
                ImGui.BeginDisabled(_emitter.Modifiers.Count == 0);
                if (ImGui.Button("Remove Modifier", buttonSize)) { RemoveSelectedModifier(); }
                ImGui.EndDisabled();
                Vec2 listBoxSize = ImGui.GetContentRegionAvail();
                listBoxSize.Y = ImGui.CalcTextSize("Y").Y * 5 + style.ItemSpacing.Y * 5;
                ImGui.BeginDisabled(_emitter.Modifiers.Count == 0);
                if (ImGui.BeginListBox("##Particle Modifier ListBox", listBoxSize))
                {
                    int modifierIndex = 0;

                    foreach (Modifier modifier in _emitter.Modifiers)
                    {
                        if (ImGui.Selectable($"{modifier.Name}##{modifierIndex}", _modifier == modifier))
                        {
                            SelectModifier(modifier);
                        }
                        modifierIndex++;
                    }

                    ImGui.EndListBox();
                }
                ImGui.EndDisabled();

                #endregion Modifier List

                #region Choose Modifier Modal

                bool isModifierModalOpen = true;
                if (ImGui.BeginPopupModal("Choose Modifier Modal", ref isModifierModalOpen, ImGuiWindowFlags.AlwaysAutoResize))
                {
                    for (int i = 0; i < ModifierFactory.ModifierTypeNames.Length; i++)
                    {
                        if (ImGui.Selectable(ModifierFactory.ModifierTypeNames[i]))
                        {
                            Type modifierType = ModifierFactory.ModifierTypes[i];
                            AddModifier(modifierType);
                            ImGui.CloseCurrentPopup();
                        }
                    }

                    Vec2 size = ImGui.GetWindowSize();
                    Vec2 pos = ImGui.GetMainViewport().GetCenter() - size * 0.5f;
                    ImGui.SetWindowPos(pos);
                    ImGui.EndPopup();
                }
                _emitOnClick = !isModifierModalOpen;

                #endregion Choose Modifier Modal

                if (_modifier is null)
                {
                    ImGui.TextDisabled("Create or Select a Modifier to view its properties!");
                }
                else
                {

                    #region Modifier Properties

                    ImGui.BeginTable($"##ModifierPropertiesTable", 3, ImGuiTableFlags.SizingStretchSame);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Name");
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputText("##ModifierName", ref _modifier.Name, 256, ImGuiInputTextFlags.EnterReturnsTrue);
                    OutlinePreviousItemIfActive();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Frequency");
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputFloat("##ModifierFrequency", ref _modifier.Frequency, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                    OutlinePreviousItemIfActive();

                    switch (_modifier)
                    {
                        case AgeModifier ageModifier:
                            break;

                        case CircleContainerModifier circleContainerModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Inside");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.Checkbox("##CircleContainerModifier##Inside", ref circleContainerModifier.Inside);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Radius");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##CircleContainerModifier##Radius", ref circleContainerModifier.Radius, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Restitution Coefficient");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##CircleContainerModifier##RestitutionCoefficient", ref circleContainerModifier.RestitutionCoefficient, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;

                        case DragModifier dragModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Density");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##DragModifier##Density", ref dragModifier.Density);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Drag Coefficient");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##DragModifier##DragCoefficient", ref dragModifier.DragCoefficient, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;

                        case LinearGravityModifier linearGravityModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Direction");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat2("##LinearGravityModifier##Direction", ref linearGravityModifier.Direction, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Strength");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##LinearGravityModifier##Strength", ref linearGravityModifier.Strength, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;

                        case OpacityFastFadeModifier opacityFastFadeModifier:
                            break;

                        case RectangleContainerModifier rectangleContainerModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Width");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputInt("##RectangleContainerModifier##Width", ref rectangleContainerModifier.Width, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Height");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputInt("##RectangleContainerModifier##Height", ref rectangleContainerModifier.Height, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Restitution Coefficient");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##RectangleContainerModifier##RestitutionCoefficient", ref rectangleContainerModifier.RestitutionCoefficient, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;

                        case RectangleLoopContainerModifier rectangleLoopContainerModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Width");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputInt("##RectangleLoopContainerModifier##Width", ref rectangleLoopContainerModifier.Width, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Height");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputInt("##RectangleLoopContainerModifier##Height", ref rectangleLoopContainerModifier.Height, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;

                        case RotationModifier rotationModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Rotation Rate");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##RotationModifier##RotationRate", ref rotationModifier.RotationRate, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;

                        case VelocityColorModifier velocityColorModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Stationary Color");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.ColorEdit3("##VelocityColorModifier##StationaryColor", ref velocityColorModifier.StationaryColor, ImGuiColorEditFlags.InputRGB);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Velocity Color");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.ColorEdit3("##VelocityColorModifier##VelocityColor", ref velocityColorModifier.VelocityColor, ImGuiColorEditFlags.InputRGB);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Velocity Threshold");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##VelocityColorModifier##VelocityThreshold", ref velocityColorModifier.VelocityThreshold, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;

                        case VelocityModifier velocityModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Velocity Threshold");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##VelocityModifier##VelocityThreshold", ref velocityModifier.VelocityThreshold, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;

                        case VortexModifier vortexModifier:
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Position");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat2("##VortexModifier##Position", ref vortexModifier.Position, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Mass");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("##VortexModifier##Mass", ref vortexModifier.Mass, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();

                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Max Speed");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputFloat("#VortexModifier##MaxSpeed", ref vortexModifier.MaxSpeed, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();
                            break;
                    }

                    ImGui.EndTable();

                    #endregion Modifier Properties

                    if (_modifier is AgeModifier || _modifier is VelocityModifier)
                    {
                        #region Interpolator List

                        contentSize = ImGui.GetContentRegionAvail();
                        buttonSize = Vec2.Zero;
                        buttonSize.X = contentSize.X * 0.5f - style.ItemSpacing.X * 0.5f;

                        if (ImGui.Button("Add Interpolator", buttonSize))
                        {
                            ImGui.OpenPopup("Choose Interpolator Modal");
                        }
                        ImGui.SameLine();
                        ImGui.BeginDisabled(_interpolators?.Count == 0);
                        if (ImGui.Button("Remove Interpolator", buttonSize)) { RemoveSelectedInterpolator(); }
                        ImGui.EndDisabled();

                        listBoxSize = ImGui.GetContentRegionAvail();
                        listBoxSize.Y = ImGui.CalcTextSize("Y").Y * 5 + style.ItemSpacing.Y * 5;
                        ImGui.BeginDisabled(_interpolators.Count == 0);
                        if (ImGui.BeginListBox("##Modifier Interpolator ListBox", listBoxSize))
                        {
                            int interpolatorIndex = 0;

                            foreach (Interpolator interpolator in _interpolators)
                            {
                                if (ImGui.Selectable($"{interpolator.Name}##{interpolatorIndex}", _interpolator == interpolator))
                                {
                                    SelectInterpolator(interpolator);
                                }
                                interpolatorIndex++;
                            }

                            ImGui.EndListBox();
                        }
                        ImGui.EndDisabled();

                        #endregion Interpolator List

                        #region Choose Interpolator Modal

                        bool isInterpolatorModalOpen = true;
                        if (ImGui.BeginPopupModal("Choose Interpolator Modal", ref isInterpolatorModalOpen, ImGuiWindowFlags.AlwaysAutoResize))
                        {
                            for (int i = 0; i < InterpolatorFactory.InterpolatorTypeNames.Length; i++)
                            {
                                if (ImGui.Selectable(InterpolatorFactory.InterpolatorTypeNames[i]))
                                {
                                    Type interpolatorType = InterpolatorFactory.InterpolatorTypes[i];
                                    AddInterpolator(interpolatorType);
                                    ImGui.CloseCurrentPopup();
                                }
                            }

                            Vec2 size = ImGui.GetWindowSize();
                            Vec2 pos = ImGui.GetMainViewport().GetCenter() - size * 0.5f;
                            ImGui.SetWindowPos(pos);
                            ImGui.EndPopup();
                        }
                        _emitOnClick = !isInterpolatorModalOpen;

                        #endregion Choose Interpolator Modal


                        if (_interpolator is not null)
                        {
                            #region Interpolator Properties

                            ImGui.BeginTable($"##ModifierPropertiesTable", 3, ImGuiTableFlags.SizingStretchSame);
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Name");
                            ImGui.TableNextColumn();
                            ImGui.TableNextColumn();
                            ImGui.SetNextItemWidth(-1);
                            ImGui.InputText($"##Interpolator##Name", ref _interpolator.Name, 256, ImGuiInputTextFlags.EnterReturnsTrue);
                            OutlinePreviousItemIfActive();

                            switch (_interpolator)
                            {
                                case ColorInterpolator colorInterpolator:
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Start Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.ColorEdit3("##ColorInterpolator##StartValue", ref colorInterpolator.StartValue, ImGuiColorEditFlags.InputRGB);
                                    OutlinePreviousItemIfActive();
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("End Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.ColorEdit3("##ColorInterpolator###EndValue", ref colorInterpolator.EndValue, ImGuiColorEditFlags.InputRGB);
                                    OutlinePreviousItemIfActive();
                                    break;

                                case HueInterpolator hueInterpolator:
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Start Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat("##HueInterpolator##StartValue", ref hueInterpolator.StartValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("End Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat("##HueInterpolator##EndValue", ref hueInterpolator.EndValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    break;

                                case OpacityInterpolator opacityInterpolator:
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Start Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat("##OpacityInterpolator##StartValue", ref opacityInterpolator.StartValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("End Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat("##OpacityInterpolatorEndValue", ref opacityInterpolator.EndValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    break;

                                case RotationInterpolator rotationInterpolator:
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Start Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat("##RotationInterpolator##StartValue", ref rotationInterpolator.StartValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("End Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat("##RotationInterpolator##EndValue", ref rotationInterpolator.EndValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    break;

                                case ScaleInterpolator scaleInterpolator:
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Start Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat("##ScaleInterpolator##StartValue", ref scaleInterpolator.StartValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("End Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat("##ScaleInterpolator##EndValue", ref scaleInterpolator.EndValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    break;

                                case VelocityInterpolator velocityInterpolator:
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("Start Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat2("##VelocityInterpolator##StartValue", ref velocityInterpolator.StartValue, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.Text("End Value");
                                    ImGui.TableNextColumn();
                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(-1);
                                    ImGui.InputFloat2("##VelocityInterpolator##EndValue", ref velocityInterpolator.EndValue, null, ImGuiInputTextFlags.EnterReturnsTrue);
                                    OutlinePreviousItemIfActive();
                                    break;
                            }
                            ImGui.EndTable();

                            #endregion Interpolator Properties

                        }
                    }

                }
            }

            Vec2 windowSize = ImGui.GetWindowSize();
            Vec2 windowPos = ImGui.GetWindowPos();
            _modifiersWindowRect = new Rectangle((int)windowPos.X, (int)windowPos.Y, (int)windowSize.X, (int)windowSize.Y);
            ImGui.End();
        }

        #endregion ImGui Windows

        #region Helper Methods

        private void CreateNewProject()
        {
            string result = TinyFileDialog.SaveFile("Create New Project", _defaultDirectory + "\\", "*.particles", "Particle Effect");

            if (string.IsNullOrEmpty(result)) { return; }

            _projectName = Path.GetFileNameWithoutExtension(result);
            _projectFile = result;
            _projectDirectory = Path.GetDirectoryName(result);

            _particleEffect?.Dispose();
            _particleEffect = new ParticleEffect(_projectName);
            _particleEffect.Position = ImGui.GetIO().DisplaySize * 0.5f;

            AddNewEmitter();
            SaveProject();
            _emitOnClick = true;
        }

        private void OpenExistingProject()
        {
            string result = TinyFileDialog.OpenFile("Open Existing Project", _defaultDirectory + "\\", "*.particles", "Particle Effect", false);

            if (string.IsNullOrEmpty(result)) { return; }

            _projectName = Path.GetFileNameWithoutExtension(result);
            _projectFile = result;
            _projectDirectory = Path.GetDirectoryName(result);

            _particleEffect?.Dispose();

            JsonSerializerOptions options = ParticleEffectJsonSerializerOptionsProvider.Default;
            string json = File.ReadAllText(result);
            _particleEffect = JsonSerializer.Deserialize<ParticleEffect>(json, options);
            _emitter = _particleEffect.Emitters.FirstOrDefault();
            _modifier = _emitter?.Modifiers.FirstOrDefault();

            switch (_modifier)
            {
                case AgeModifier age:
                    _interpolator = age.Interpolators.FirstOrDefault();
                    break;
                case VelocityModifier velocity:
                    _interpolator = velocity.Interpolators.FirstOrDefault();
                    break;
            }

            foreach (Texture2D texture in _particleRenderer.Textures.Values)
            {
                texture.Dispose();
            }

            _particleRenderer.Textures.Clear();

            foreach (ParticleEmitter emitter in _particleEffect.Emitters)
            {
                string path = Path.Combine(_projectDirectory, emitter.TextureKey);
                Texture2D texture = Texture2D.FromFile(GraphicsDevice, path);
                texture.Name = emitter.TextureKey;
                _particleRenderer.Textures.Add(texture.Name, texture);
            }

            _particleEffect.Position = ImGui.GetIO().DisplaySize * 0.5f;
            _emitOnClick = true;
        }

        private void SaveProject()
        {
            JsonSerializerOptions options = ParticleEffectJsonSerializerOptionsProvider.Default;
            string json = JsonSerializer.Serialize<ParticleEffect>(_particleEffect, options);
            File.WriteAllText(_projectFile, json);
        }

        private void AddNewEmitter()
        {
            _emitter = ParticleEmitterFactory.CreateParticleEmitter();
            _particleEffect?.Emitters.Add(_emitter);
        }

        private void RemoveSelectedEmitter()
        {
            if (_particleEffect is null || _emitter is null) { return; }

            var index = _particleEffect.Emitters.IndexOf(_emitter);
            _particleEffect.Emitters.RemoveAt(index);
            index = Math.Max(0, index - 1);
            _emitter = _particleEffect.Emitters.ElementAtOrDefault(index);
        }

        private void ChooseTextureForEmitter()
        {
            if (_emitter is null) { return; }

            //  Ask user to choose image file to open
            string path = TinyFileDialog.OpenFile("Choose Texture", _defaultDirectory + "\\", "*.png,*.jgp,*.png,*.tif", "Image Files", false);

            //  If they hit cancel, then path will be null
            if (string.IsNullOrEmpty(path)) { return; }

            string textureKey = Path.GetFileName(path);

            //  If the image file they choose is already in the project directory
            //  check to see if it's already loaded. If it is, just set the emitter
            //  to that texture, otherwise, load it and then set the emitter
            if (Path.GetDirectoryName(path) == _projectDirectory)
            {
                if (!_particleRenderer.Textures.ContainsKey(textureKey))
                {
                    Texture2D texture = Texture2D.FromFile(GraphicsDevice, path);
                    texture.Name = textureKey;
                    _particleRenderer.Textures.Add(textureKey, texture);
                }

                _emitter.TextureKey = textureKey;
                return;
            }

            //  Otherwise, the image file they choose is not in the project directory
            //  which means we'll need to copy it to the project directory.  However,
            //  if a file already exists in the project directory with that name,
            //  we'll need to prompt for approval to overwrite it
            string existing = Path.Combine(_projectDirectory, textureKey);
            if (File.Exists(existing))
            {
                string choice = TinyFileDialog.MessageBox("Overwrite Existing?", $"{existing} already exists.\nDo you want to replace it?", "yesno", "warning", 0);
                if (choice == "no")
                {
                    return;
                }

                _particleRenderer.Textures[textureKey].Dispose();
                _particleRenderer.Textures.Remove(textureKey);
            }

            File.Copy(path, existing, true);
            Texture2D newTexture = Texture2D.FromFile(GraphicsDevice, existing);
            _particleRenderer.Textures.Add(textureKey, newTexture);

            _emitter.TextureKey = textureKey;
        }

        private void AddModifier(Type type)
        {
            if (_emitter is null) { return; }

            var modifier = ModifierFactory.CreateModifier(type);
            _emitter.Modifiers.Add(modifier);
            SelectModifier(modifier);
        }

        private void RemoveSelectedModifier()
        {
            if (_emitter is null || _modifier is null) { return; }

            var index = _emitter.Modifiers.IndexOf(_modifier);
            _emitter.Modifiers.RemoveAt(index);
            index = Math.Max(0, index - 1);
            SelectModifier(index);
        }

        private void SelectModifier(int index)
        {
            var modifier = _emitter?.Modifiers.ElementAtOrDefault(index);
            SelectModifier(modifier);
        }

        private void SelectModifier(Modifier modifier)
        {
            _modifier = modifier;
            ChangeInterpolatorCollection();
        }

        private void AddInterpolator(Type type)
        {
            if (_interpolators is null) { return; }

            var interpolator = InterpolatorFactory.CreateInterpolator(type);
            _interpolators.Add(interpolator);
            SelectInterpolator(interpolator);
        }

        private void RemoveSelectedInterpolator()
        {
            if (_interpolators is null) { return; }

            int index = _interpolators.IndexOf(_interpolator);
            _interpolators.RemoveAt(index);
            index = Math.Max(0, index - 1);
            SelectInterpolator(index);
            _interpolator = _interpolators.ElementAtOrDefault(index);
        }

        private void ChangeInterpolatorCollection()
        {
            if (_modifier is AgeModifier ageModifier)
            {
                _interpolators = ageModifier.Interpolators;
            }
            else if (_modifier is VelocityModifier velocityModifier)
            {
                _interpolators = velocityModifier.Interpolators;
            }
            else
            {
                _interpolators = null;
            }

            _interpolator = _interpolators?.FirstOrDefault();
        }

        private void SelectInterpolator(int index)
        {
            var interpolator = _interpolators?.ElementAtOrDefault(index);
            SelectInterpolator(interpolator);
        }

        private void SelectInterpolator(Interpolator interpoaltor)
        {
            _interpolator = interpoaltor;
        }

        private void OutlinePreviousItemIfActive()
        {
            if (ImGui.IsItemActive())
            {
                Vec2 min = ImGui.GetItemRectMin();
                Vec2 max = ImGui.GetItemRectMax();
                uint col = ImGui.GetColorU32(new Vec4(1, 1, 1, 1));
                ImDrawFlags flags = ImDrawFlags.None;
                ImGui.GetWindowDrawList().AddRect(min, max, col, 0.0f, flags, 1.0f);
            }
        }

        #endregion Helper Methods
    }
}
