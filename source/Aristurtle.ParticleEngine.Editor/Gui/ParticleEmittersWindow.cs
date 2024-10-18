// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using Aristurtle.ParticleEngine.Data;
using Aristurtle.ParticleEngine.Editor.Factories;
using Aristurtle.ParticleEngine.Editor.Graphics;
using Aristurtle.ParticleEngine.Profiles;
using ImGuiNET;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class ParticleEmittersWindow
{
    private static readonly string[] _renderingOrderNames = Enum.GetNames(typeof(ParticleRenderingOrder));
    private static readonly string[] _particleValueKindNames = Enum.GetNames(typeof(ParticleValueKind));
    private static readonly string[] _circleRadiationNames = Enum.GetNames(typeof(CircleRadiation));

    public static SysVec2 WindowSize = SysVec2.Zero;
    public static SysVec2 WindowPos = SysVec2.Zero;

    public static void Draw()
    {
        SysVec2 pos = new SysVec2(0, MainMenuWindow.Size.Y);

        SysVec2 size = new SysVec2(600, 1056);

        ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
        ImGui.Begin("Particle Emitters");
        DrawEmitterList();
        DrawSelectedEmitterInputs();

        WindowSize = ImGui.GetWindowSize();
        WindowPos = ImGui.GetWindowPos();

        ImGui.End();
    }

    private static void DrawEmitterList()
    {
        ImGuiStylePtr style = ImGui.GetStyle();

        SysVec2 contentSize = ImGui.GetContentRegionAvail();

        SysVec2 buttonSize;
        buttonSize.X = contentSize.X * 0.5f - style.ItemSpacing.X * 0.5f;
        buttonSize.Y = 0;

        SysVec2 listBoxSize;
        listBoxSize.X = contentSize.X;
        listBoxSize.Y = ImGui.CalcTextSize("Y").Y * 5 + style.ItemSpacing.Y * 5;


        if (ImGui.Button("Add Emitter", buttonSize)) { Project.AddNewEmitter(); }
        ImGui.SameLine();
        ImGui.BeginDisabled(Project.ParticleEffect is null || Project.ParticleEffect.Emitters.Count == 0);
        if (ImGui.Button("Remove Emitter", buttonSize)) { Project.RemoveSelectedEmitter(); }
        ImGui.EndDisabled();

        ImGui.BeginDisabled(Project.ParticleEffect is null || Project.ParticleEffect.Emitters.Count == 0);
        if (ImGui.BeginListBox("##Particle Emitters ListBox", listBoxSize))
        {
            for (int i = 0; i < Project.ParticleEffect.Emitters.Count; i++)
            {
                ParticleEmitter emitter = Project.ParticleEffect.Emitters[i];
                bool isEmitterSelected = Project.SelectedEmitter == emitter;

                if (ImGui.Selectable($"{emitter.Name}##{i}", isEmitterSelected))
                {
                    Project.SelectedEmitter = emitter;
                }
            }

            ImGui.EndListBox();
        }
        ImGui.EndDisabled();
    }

    private static void DrawSelectedEmitterInputs()
    {
        if (Project.SelectedEmitter is null)
        {
            ImGui.TextDisabled("Create or Select a Particle Emitter to view its properties!");
            return;
        }

        DrawSelectedEmitterProperties();
        DrawSelectedEmitterParameters();
        DrawSelectedEmitterProfile();
    }

    #region Selected Emitter Properties

    private static void DrawSelectedEmitterProperties()
    {
        ImGui.BeginTable("##Selected Emitter Properties Table", 2, ImGuiTableFlags.SizingStretchSame);
        DrawSelectedEmitterNameInput();
        DrawSelectedEmitterTextureInput();
        DrawSelectedEmitterAutoTriggerInput();
        DrawSelectedEmitterAutoTriggerFrequencyInput();
        DrawSelectedEmitterCapacityInput();
        DrawSelectedEmitterLifespanInput();
        DrawSelectedEmitterRenderingOrderInput();
        ImGui.EndTable();

    }

    private static void DrawSelectedEmitterNameInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Name");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##Selected Emitter Name Input", ref Project.SelectedEmitter.Name, 256, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterTextureInput()
    {
        string preview = Path.GetFileNameWithoutExtension(Project.SelectedEmitter.TextureKey);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Texture");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##Selected Emitter Texture Input", preview))
        {
            if (ImGui.Selectable("<New Texture>", string.IsNullOrEmpty(Project.SelectedEmitter.TextureKey)))
            {
                Project.AddNewTexture();
            }

            ImGui.Separator();

            foreach (string texture in ParticleEffectRenderer.Textures.Keys)
            {
                string label = Path.GetFileNameWithoutExtension(texture);
                if (ImGui.Selectable(label, Project.SelectedEmitter.TextureKey == texture))
                {
                    Project.SelectedEmitter.TextureKey = texture;
                }
            }

            ImGui.EndCombo();
        }
    }

    private static void DrawSelectedEmitterAutoTriggerInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Auto Trigger");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.Checkbox("##Selected Emitter Auto Trigger Input", ref Project.SelectedEmitter.AutoTrigger);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterAutoTriggerFrequencyInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Auto Trigger Frequency");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Auto Trigger Frequency Input", ref Project.SelectedEmitter.AutoTriggerFrequency);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterCapacityInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Capacity");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        int capacity = Project.SelectedEmitter.Capacity;
        if (ImGui.InputInt("##Selected Emitter Capacity Input", ref capacity, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            Project.SelectedEmitter.ChangeCapacity(capacity);
        }
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterLifespanInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Lifespan");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Lifespan Input", ref Project.SelectedEmitter.LifeSpan);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterRenderingOrderInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Rendering Order");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        int renderingOrder = (int)Project.SelectedEmitter.RenderingOrder;
        if (ImGui.Combo("##Selected Emitter Rendering Order Input", ref renderingOrder, _renderingOrderNames, _renderingOrderNames.Length))
        {
            Project.SelectedEmitter.RenderingOrder = (ParticleRenderingOrder)renderingOrder;
        }
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    #endregion Selected Emitter Properties

    #region Selected Emitter Parameters

    private static void DrawSelectedEmitterParameters()
    {
        ImGui.BeginTable("##Selected Emitter Parameters Table", 3, ImGuiTableFlags.SizingStretchSame);
        DrawSelectedEmitterQuantityInput();
        DrawSelectedEmitterSpeed();
        DrawSelectedEmitterColorInput();
        DrawSelectedEmitterOpacityInput();
        DrawSelectedEmitterScaleInput();
        DrawSelectedEmitterRotationInput();
        DrawSelectedEmitterMassInput();
        ImGui.EndTable();
    }

    private static void DrawSelectedEmitterQuantityInput()
    {
        ref ParticleInt32Parameter value = ref Project.SelectedEmitter.Parameters.Quantity;
        int kind = (int)value.Kind;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Quantity");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Quantity Kind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
        {
            value.Kind = (ParticleValueKind)kind;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        if (value.Kind is ParticleValueKind.Constant)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputInt("##Selected Emitter Quantity Constant Input", ref value.Constant, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGuiEx.OutlinePreviousItemIfActive();
        }

        if (value.Kind is ParticleValueKind.Random)
        {
            int[] range = new int[] { value.RandomMin, value.RandomMax };
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputInt2("##Selected Emitter Quantity Random Input", ref range[0], ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value.RandomMin = range[0];
                value.RandomMax = range[1];
            }
            ImGuiEx.OutlinePreviousItemIfActive();
        }
    }

    private static void DrawSelectedEmitterSpeed()
    {
        ref ParticleFloatParameter value = ref Project.SelectedEmitter.Parameters.Speed;
        int kind = (int)value.Kind;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Speed");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Speed Kind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
        {
            value.Kind = (ParticleValueKind)kind;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        if (value.Kind is ParticleValueKind.Constant)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputFloat("##Selected Emitter Speed Constant Input", ref value.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGuiEx.OutlinePreviousItemIfActive();
        }

        if (value.Kind is ParticleValueKind.Random)
        {
            SysVec2 range = new SysVec2(value.RandomMin, value.RandomMax);
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat2("##Selected Emitter Speed Random Input", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value.RandomMin = range.X;
                value.RandomMax = range.Y;
            }
            ImGuiEx.OutlinePreviousItemIfActive();
        }
    }

    private static void DrawSelectedEmitterColorInput()
    {
        ref ParticleColorParameter value = ref Project.SelectedEmitter.Parameters.Color;
        int kind = (int)value.Kind;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Color");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Color Kind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
        {
            value.Kind = (ParticleValueKind)kind;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        if (value.Kind is ParticleValueKind.Constant)
        {
            var (r, g, b) = ColorUtilities.HslToRgb(value.Constant);
            SysVec3 rgb = new SysVec3(r, g, b) / 255.0f;

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.ColorEdit3("##Selected Emitter Color Constant Input", ref rgb, ImGuiColorEditFlags.InputRGB))
            {
                var (h, s, l) = ColorUtilities.RgbToHsl(rgb);
                value.Constant = new SysVec3(h, s, l);
            }
            ImGuiEx.OutlinePreviousItemIfActive();
        }

        if (value.Kind is ParticleValueKind.Random)
        {
            var (rMin, gMin, bMin) = ColorUtilities.HslToRgb(value.RandomMin);
            var (rMax, gMax, bMax) = ColorUtilities.HslToRgb(value.RandomMax);
            SysVec3 rgbMin = new SysVec3(rMin, gMin, bMin) / 255.0f;
            SysVec3 rgbMax = new SysVec3(rMax, gMax, bMax) / 255.0f;

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.ColorEdit3("##Selected Emitter Color Random Min Input", ref rgbMin, ImGuiColorEditFlags.InputRGB))
            {
                var (h, s, l) = ColorUtilities.RgbToHsl(rgbMin);
                value.RandomMin = new SysVec3(h, s, l);
            }
            ImGuiEx.OutlinePreviousItemIfActive();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.ColorEdit3("##Selected Emitter Color Random Max Input", ref rgbMax, ImGuiColorEditFlags.InputRGB))
            {
                var (h, s, l) = ColorUtilities.RgbToHsl(rgbMax);
                value.RandomMax = new SysVec3(h, s, l);
            }
            ImGuiEx.OutlinePreviousItemIfActive();
        }
    }

    private static void DrawSelectedEmitterOpacityInput()
    {
        ref ParticleFloatParameter value = ref Project.SelectedEmitter.Parameters.Opacity;
        int kind = (int)value.Kind;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Opacity");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Opacity Kind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
        {
            value.Kind = (ParticleValueKind)kind;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        if (value.Kind is ParticleValueKind.Constant)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputFloat("##Selected Emitter Opacity Constant Input", ref value.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGuiEx.OutlinePreviousItemIfActive();
        }

        if (value.Kind is ParticleValueKind.Random)
        {
            SysVec2 range = new SysVec2(value.RandomMin, value.RandomMax);
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat2("##Selected Emitter Opacity Random Input", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value.RandomMin = range.X;
                value.RandomMax = range.Y;
            }
            ImGuiEx.OutlinePreviousItemIfActive();
        }
    }

    private static void DrawSelectedEmitterScaleInput()
    {
        ref ParticleFloatParameter value = ref Project.SelectedEmitter.Parameters.Scale;
        int kind = (int)value.Kind;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Scale");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Scale Kind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
        {
            value.Kind = (ParticleValueKind)kind;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        if (value.Kind is ParticleValueKind.Constant)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputFloat("##Selected Emitter Scale Constant Input", ref value.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGuiEx.OutlinePreviousItemIfActive();
        }

        if (value.Kind is ParticleValueKind.Random)
        {
            SysVec2 range = new SysVec2(value.RandomMin, value.RandomMax);
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat2("##Selected Emitter Scale Random Input", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value.RandomMin = range.X;
                value.RandomMax = range.Y;
            }
            ImGuiEx.OutlinePreviousItemIfActive();
        }
    }

    private static void DrawSelectedEmitterRotationInput()
    {
        ref ParticleFloatParameter value = ref Project.SelectedEmitter.Parameters.Rotation;
        int kind = (int)value.Kind;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Rotation");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Rotation Kind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
        {
            value.Kind = (ParticleValueKind)kind;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        if (value.Kind is ParticleValueKind.Constant)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputFloat("##Selected Emitter Rotation Constant Input", ref value.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGuiEx.OutlinePreviousItemIfActive();
        }

        if (value.Kind is ParticleValueKind.Random)
        {
            SysVec2 range = new SysVec2(value.RandomMin, value.RandomMax);
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat2("##Selected Emitter Rotation Random Input", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value.RandomMin = range.X;
                value.RandomMax = range.Y;
            }
            ImGuiEx.OutlinePreviousItemIfActive();
        }
    }

    private static void DrawSelectedEmitterMassInput()
    {
        ref ParticleFloatParameter value = ref Project.SelectedEmitter.Parameters.Mass;
        int kind = (int)value.Kind;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Mass");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Mass Kind", ref kind, _particleValueKindNames, _particleValueKindNames.Length))
        {
            value.Kind = (ParticleValueKind)kind;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        if (value.Kind is ParticleValueKind.Constant)
        {
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputFloat("##Selected Emitter Mass Constant Input", ref value.Constant, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGuiEx.OutlinePreviousItemIfActive();
        }

        if (value.Kind is ParticleValueKind.Random)
        {
            SysVec2 range = new SysVec2(value.RandomMin, value.RandomMax);
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputFloat2("##Selected Emitter Mass Random Input", ref range, null, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value.RandomMin = range.X;
                value.RandomMax = range.Y;
            }
            ImGuiEx.OutlinePreviousItemIfActive();
        }
    }

    #endregion Selected Emitter Parameters

    #region Selected Emitter Profile

    private static void DrawSelectedEmitterProfile()
    {
        ImGui.BeginTable("##Selected Emitter Profile Table", 3, ImGuiTableFlags.SizingStretchSame);
        DrawSelectedEmitterProfileTypeInput();

        switch (Project.SelectedEmitter.Profile)
        {
            case BoxFillProfile boxFillProfile:
                DrawSelectedEmitterProfileInputs(boxFillProfile);
                break;

            case BoxProfile boxProfile:
                DrawSelectedEmitterProfileInputs(boxProfile);
                break;

            case BoxUniformProfile boxUniformProfile:
                DrawSelectedEmitterProfileInputs(boxUniformProfile);
                break;

            case CircleProfile circleProfile:
                DrawSelectedEmitterProfileInputs(circleProfile);
                break;

            case LineProfile lineProfile:
                DrawSelectedEmitterProfileInputs(lineProfile);
                break;

            case RingProfile ringProfile:
                DrawSelectedEmitterProfileInputs(ringProfile);
                break;

            case SprayProfile sprayProfile:
                DrawSelectedEmitterProfileInputs(sprayProfile);
                break;


        }

        ImGui.EndTable();
    }

    private static void DrawSelectedEmitterProfileTypeInput()
    {
        int index = Array.IndexOf(ParticleProfileFactory.ProfileTypes, Project.SelectedEmitter.Profile.GetType());

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Profile Type");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Profile Type Input", ref index, ParticleProfileFactory.ProfileTypeNames, ParticleProfileFactory.ProfileTypeNames.Length))
        {
            Type newType = ParticleProfileFactory.ProfileTypes[index];
            if (Project.SelectedEmitter.Profile.GetType() != newType)
            {
                Project.SelectedEmitter.Profile = ParticleProfileFactory.CreateProfile(newType);
            }
        }
    }

    private static void DrawSelectedEmitterProfileInputs(BoxFillProfile profile)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Width");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Box Fill Profile Width Input", ref profile.Width, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Height");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Box Fill Profile Height Input", ref profile.Height, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterProfileInputs(BoxProfile profile)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Width");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Box Profile Width Input", ref profile.Width, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Height");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Box Profile Height Input", ref profile.Height, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterProfileInputs(BoxUniformProfile profile)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Width");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Box Uniform Profile Width Input", ref profile.Width, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Height");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Box Uniform Profile Height Input", ref profile.Height, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterProfileInputs(CircleProfile profile)
    {
        int radiate = (int)profile.Radiate;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Radiate");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Circle Profile Radiate Input", ref radiate, _circleRadiationNames, _circleRadiationNames.Length))
        {
            profile.Radiate = (CircleRadiation)radiate;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Radius");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Circle Profile Radius Input", ref profile.Radius, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterProfileInputs(LineProfile profile)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Axis");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat2("##Selected Emitter Line Profile Axis Input", ref profile.Axis, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Length");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Line Profile Length Input", ref profile.Length, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterProfileInputs(RingProfile profile)
    {
        int radiate = (int)profile.Radiate;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Radiate");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##Selected Emitter Ring Profile Radiate Input", ref radiate, _circleRadiationNames, _circleRadiationNames.Length))
        {
            profile.Radiate = (CircleRadiation)radiate;
        }
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Radius");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Ring Profile Radius Input", ref profile.Radius, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedEmitterProfileInputs(SprayProfile profile)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Direction");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat2("##Selected Emitter Spray Profile Axis Input", ref profile.Direction, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.Text("Spread");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Emitter Spray Profile Spread Input", ref profile.Spread, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    #endregion Selected Emitter Profile
}
