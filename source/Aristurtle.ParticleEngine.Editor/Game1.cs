// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Aristurtle.ParticleEngine.Editor.Graphics;
using Aristurtle.ParticleEngine.Editor.Gui;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Aristurtle.ParticleEngine.Editor
{
    public class Game1 : Game
    {
        private const string VERSION = "0.0.1";

        private static readonly CompositeFormat s_windowTitle = CompositeFormat.Parse("Turtle Particle Engine: Editor {0} | {1:F3} ms/Frame | {2:F1} FPS");

        private readonly GraphicsDeviceManager _graphics;
        private MouseState _previousMouse;
        private MouseState _currentMouse;

        private ImGuiRenderer _imguiRenderer;
        private XnaRect _particleEffectWindowRect;
        private XnaRect _modifiersWindowRect;
        private SpriteBatch _spriteBatch;
        private Texture2D _checkerBoardTexture;
        private XnaRect _checkerBoardRectangle;
        private bool _emitOnClick;
        private float _frameRate;

        public new static GraphicsDevice GraphicsDevice;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.DeviceCreated += OnGraphicsDeviceCreated;
            _graphics.DeviceReset += OnGraphicsDeviceReset;
            _graphics.ApplyChanges();


            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnClientSizeChanged;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
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
            GraphicsDevice = base.GraphicsDevice;

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

        protected override void Update(GameTime gameTime)
        {
            //  Update input states
            _previousMouse = _currentMouse;
            _currentMouse = Mouse.GetState();

            //  Only continue if there is a particle effect
            if (Project.ParticleEffect is null) { return; }

            //  Update the particle effect
            Project.ParticleEffect.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            //  If there is a modal open within the gui, don't continue
            if (ParticleEmitterModifierWindow.IsModalOpen) { return; }

            //  Trigger an emission of the particle effect at the current mouse position if the left mouse button was
            //  clicked and the position is not within a gui window.
            if (_currentMouse.LeftButton == ButtonState.Pressed)
            {
                bool emit = ParticleEmittersWindow.Bounds.Contains(_currentMouse.Position) == false &&
                            ParticleEmitterModifierWindow.Bounds.Contains(_currentMouse.Position) == false;

                if (emit)
                {
                    SysVec2 point1 = new SysVec2(_previousMouse.Position.X, _previousMouse.Position.Y);
                    SysVec2 point2 = new SysVec2(_currentMouse.Position.X, _currentMouse.Position.Y);
                    LineSegment line = new LineSegment(point1, point2);
                    Project.ParticleEffect.Trigger(line, 0.0f);
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(XnaColor.Black);

            DrawBackground();
            DrawParticleEffect();
            DrawGui(gameTime);

            Window.Title = string.Format(CultureInfo.InvariantCulture, s_windowTitle, VERSION, 1000.0f / _frameRate, _frameRate);
        }

        private void DrawBackground()
        {
            _spriteBatch?.Begin(samplerState: SamplerState.PointWrap);
            _spriteBatch?.Draw(_checkerBoardTexture, _checkerBoardRectangle, XnaColor.White);
            _spriteBatch?.End();
        }

        private void DrawParticleEffect()
        {
            if (Project.ParticleEffect is null) { return; }

            _spriteBatch?.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);
            ParticleEffectRenderer.Draw(_spriteBatch, Project.ParticleEffect);
            _spriteBatch?.End();
        }

        private void DrawGui(GameTime gameTime)
        {
            _imguiRenderer?.BeforeLayout(gameTime);
            MainMenuWindow.Draw();

            if (Project.ParticleEffect is null)
            {
                StartWindow.Draw();
            }
            else
            {
                DockSpaceWindow.Draw();
                ParticleEmittersWindow.Draw();
                ParticleEmitterModifierWindow.Draw();
            }

            _frameRate = ImGui.GetIO().Framerate;
            _imguiRenderer?.AfterLayout();
        }
    }
}
