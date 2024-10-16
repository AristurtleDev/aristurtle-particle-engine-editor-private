// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Aristurtle.ParticleEngine.Data;
using Aristurtle.ParticleEngine.Editor.Graphics;
using ImGuiNET;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class ParticleEmittersWindow
{
    private static readonly string[] _renderingOrderNames;
    private static readonly string[] _particleValueKindNames;

    private static SysVec2 _contentSize = SysVec2.Zero;
    private static SysVec2 _buttonSize = SysVec2.Zero;
    private static SysVec2 _listBoxSize = SysVec2.Zero;


    static ParticleEmittersWindow()
    {
        _renderingOrderNames = Enum.GetNames(typeof(ParticleRenderingOrder));
        _particleValueKindNames = Enum.GetNames(typeof(ParticleValueKind));
    }


    public static void Draw(ImGuiStylePtr style, ImGuiIOPtr io)
    {
        SysVec2 pos = new SysVec2(0, MainMenuWindow.Size.Y);

        SysVec2 size = new SysVec2(600, 1056);

        ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
        ImGui.Begin("Particle Emitters");

        DrawEmitterList(style, io);
        DrawSelectedEmitterInputs();

        //  TODO: Selected Emitter Profile

        ImGui.End();
    }

    private static void DrawEmitterList(ImGuiStylePtr style, ImGuiIOPtr io)
    {
        _contentSize = ImGui.GetContentRegionAvail();
        _buttonSize.X = _contentSize.X * 0.5f - style.ItemSpacing.X * 0.5f;

        if (ImGui.Button("Add Emitter", _buttonSize)) { Project.AddNewEmitter(); }
        ImGui.SameLine();
        ImGui.BeginDisabled(Project.ParticleEffect is null || Project.ParticleEffect.Emitters.Count == 0);
        if (ImGui.Button("Remove Emitter", _buttonSize)) { Project.RemoveSelectedEmitter(); }
        ImGui.EndDisabled();

        _listBoxSize = ImGui.GetContentRegionAvail();
        _listBoxSize.Y = ImGui.CalcTextSize("Y").Y * 5 + style.ItemSpacing.Y * 5;

        ImGui.BeginDisabled(Project.ParticleEffect is null || Project.ParticleEffect.Emitters.Count == 0);
        if (ImGui.BeginListBox("##Particle Emitters ListBox", _listBoxSize))
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
            if(ImGui.ColorEdit3("##Selected Emitter Color Random Max Input", ref rgbMax, ImGuiColorEditFlags.InputRGB))
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

}
