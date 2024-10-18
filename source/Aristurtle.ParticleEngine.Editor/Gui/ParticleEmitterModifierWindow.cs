// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using Aristurtle.ParticleEngine.Editor.Factories;
using Aristurtle.ParticleEngine.Modifiers;
using Aristurtle.ParticleEngine.Modifiers.Containers;
using Aristurtle.ParticleEngine.Modifiers.Interpolators;
using ImGuiNET;

namespace Aristurtle.ParticleEngine.Editor.Gui;

public static class ParticleEmitterModifierWindow
{
    public static SysVec2 WindowSize = SysVec2.Zero;
    public static SysVec2 WindowPos = SysVec2.Zero;

    public static void Draw()
    {
        SysVec2 pos = new SysVec2(1500, MainMenuWindow.Size.Y);

        SysVec2 size = new SysVec2(420, 1056);

        ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
        ImGui.Begin("Modifiers");
        if (Project.SelectedEmitter is null)
        {
            ImGui.TextDisabled("Create or Select a Particle Emitter to view its modifiers!");
        }
        else
        {
            DrawModifierList();
            DrawChooseModifierModal();
            DrawSelectedModifierInputs();

            if (Project.SelectedModifier is AgeModifier || Project.SelectedModifier is VelocityModifier)
            {
                DrawInterpolatorList();
                DrawChooseInterpolatorModal();
                DrawSelectedInterpolatorInputs();
            }
        }

        WindowSize = ImGui.GetWindowSize();
        WindowPos = ImGui.GetWindowPos();
        ImGui.End();
    }

    private static void DrawModifierList()
    {
        ImGuiStylePtr style = ImGui.GetStyle();

        SysVec2 contentSize = ImGui.GetContentRegionAvail();

        SysVec2 buttonSize;
        buttonSize.X = contentSize.X * 0.5f - style.ItemSpacing.X * 0.5f;
        buttonSize.Y = 0;

        SysVec2 listBoxSize;
        listBoxSize.X = contentSize.X;
        listBoxSize.Y = ImGui.CalcTextSize("Y").Y * 5 + style.ItemSpacing.Y * 5;

        if (ImGui.Button("Add Modifier", buttonSize)) { ImGui.OpenPopup("Choose Modifier Modal"); }
        ImGui.SameLine();
        ImGui.BeginDisabled(Project.SelectedEmitter is null || Project.SelectedEmitter.Modifiers.Count == 0);
        if (ImGui.Button("Remove Modifier", buttonSize)) { Project.RemoveSelectedModifier(); }
        ImGui.EndDisabled();

        ImGui.BeginDisabled(Project.SelectedEmitter is null || Project.SelectedEmitter.Modifiers.Count == 0);
        if (ImGui.BeginListBox("##Selected Particle Emitter Modifier ListBox", listBoxSize))
        {
            for (int i = 0; i < Project.SelectedEmitter.Modifiers.Count; i++)
            {
                Modifier modifier = Project.SelectedEmitter.Modifiers[i];
                bool isModifierSelected = Project.SelectedModifier == modifier;

                if (ImGui.Selectable($"{modifier.Name}##{i}", isModifierSelected))
                {
                    Project.SelectedModifier = modifier;
                }
            }

            ImGui.EndListBox();
        }
        ImGui.EndDisabled();
    }

    private static void DrawChooseModifierModal()
    {
        bool isOpen = true;

        if (ImGui.BeginPopupModal("Choose Modifier Modal", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            for (int i = 0; i < ModifierFactory.ModifierTypeNames.Length; i++)
            {
                if (ImGui.Selectable(ModifierFactory.ModifierTypeNames[i]))
                {
                    Type modifierType = ModifierFactory.ModifierTypes[i];
                    Project.AddModifier(modifierType);
                    ImGui.CloseCurrentPopup();
                }
            }

            SysVec2 size = ImGui.GetWindowSize();
            SysVec2 pos = ImGui.GetMainViewport().GetCenter() - size * 0.5f;
            ImGui.SetWindowPos(pos);
            ImGui.EndPopup();
        }
    }

    private static void DrawSelectedModifierInputs()
    {
        if (Project.SelectedModifier is null)
        {
            ImGui.TextDisabled("Create or Select a Modifier to view its properties!");
            return;
        }

        DrawSelectedModifierProperties();
    }

    #region Selected Modifier Properties

    private static void DrawSelectedModifierProperties()
    {
        ImGui.BeginTable("##Selected Modifier Properties Table", 3, ImGuiTableFlags.SizingStretchSame);
        DrawSelectedModifierNameInput();
        DrawSelectedModifierFrequencyInput();

        switch (Project.SelectedModifier)
        {
            case CircleContainerModifier circleContainer:
                DrawSelectedModifierInputs(circleContainer);
                break;

            case DragModifier dragModifier:
                DrawSelectedModifierInputs(dragModifier);
                break;

            case LinearGravityModifier linearGravity:
                DrawSelectedModifierInputs(linearGravity);
                break;

            case RectangleContainerModifier rectangleContainer:
                DrawSelectedModifierInputs(rectangleContainer);
                break;

            case RectangleLoopContainerModifier rectangleLoopContainer:
                DrawSelectedModifierInputs(rectangleLoopContainer);
                break;

            case RotationModifier rotationModifier:
                DrawSelectedModifierInputs(rotationModifier);
                break;

            case VelocityColorModifier velocityColor:
                DrawSelectedModifierInputs(velocityColor);
                break;

            case VelocityModifier velocity:
                DrawSelectedModifierInputs(velocity);
                break;

            case VortexModifier vortex:
                DrawSelectedModifierInputs(vortex);
                break;
        }

        ImGui.EndTable();
    }

    private static void DrawSelectedModifierNameInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Name");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##Selected Modifier Name Input", ref Project.SelectedModifier.Name, 256, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierFrequencyInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Frequency");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Frequency Input", ref Project.SelectedModifier.Frequency, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(CircleContainerModifier modifier)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Inside");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.Checkbox("##Selected Modifier Circle Container Inside Input", ref modifier.Inside);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Radius");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Circle Container Radius Input", ref modifier.Radius, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Restitution Coefficient");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Circle Container Restitution Coefficient Input", ref modifier.RestitutionCoefficient, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(DragModifier modifier)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Density");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Drag Density Input", ref modifier.Density);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Drag Coefficient");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Drag Drag Coefficient Input", ref modifier.DragCoefficient, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(LinearGravityModifier modifier)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Direction");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat2("##Selected Modifier Linear Gravity Direction Input", ref modifier.Direction, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Strength");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Linear Gravity Strength Input", ref modifier.Strength, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(RectangleContainerModifier modifier)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Width");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputInt("##Selected Modifier Rectangle Container Width Input", ref modifier.Width, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Height");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputInt("##Selected Modifier Rectangle Container Height Input", ref modifier.Height, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Restitution Coefficient");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Rectangle Container Restitution Coefficient Input", ref modifier.RestitutionCoefficient, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(RectangleLoopContainerModifier modifier)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Width");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputInt("##Selected Modifier Rectangle Loop Container Width Input", ref modifier.Width, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Height");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputInt("##Selected Modifier Rectangle Loop Container Height Input", ref modifier.Height, 0, 0, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(RotationModifier modifier)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Rotation Rate");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Rotation Rotation Rate INput", ref modifier.RotationRate, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(VelocityColorModifier modifier)
    {
        ImGui.TableNextColumn();
        ImGui.Text("Stationary Color");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.ColorEdit3("##Selected Modifier Velocity Color Stationary Color Input", ref modifier.StationaryColor, ImGuiColorEditFlags.InputRGB);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Velocity Color");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.ColorEdit3("##Selected Modifier Velocity Color Velocity Color Input", ref modifier.VelocityColor, ImGuiColorEditFlags.InputRGB);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Velocity Threshold");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Velocity Color Velocity Threshold Input", ref modifier.VelocityThreshold, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(VelocityModifier modifier)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Velocity Threshold");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Velocity Velocity Threshold Input", ref modifier.VelocityThreshold, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedModifierInputs(VortexModifier modifier)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Position");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat2("##Selected Modifier Vortex Position Input", ref modifier.Position, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Mass");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Modifier Vortex Mass Input", ref modifier.Mass, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Max Speed");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("#Selected Modifier Vortex Max Speed Input", ref modifier.MaxSpeed, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    #endregion Selected Modifier Properties

    private static void DrawInterpolatorList()
    {
        ImGuiStylePtr style = ImGui.GetStyle();

        SysVec2 contentSize = ImGui.GetContentRegionAvail();

        SysVec2 buttonSize;
        buttonSize.X = contentSize.X * 0.5f - style.ItemSpacing.X * 0.5f;
        buttonSize.Y = 0;

        SysVec2 listBoxSize;
        listBoxSize.X = contentSize.X;
        listBoxSize.Y = ImGui.CalcTextSize("Y").Y * 5 + style.ItemSpacing.Y * 5;

        if (ImGui.Button("Add Interpolator", buttonSize)) { ImGui.OpenPopup("Choose Interpolator Modal"); }
        ImGui.SameLine();
        ImGui.BeginDisabled(Project.CurrentInterpolators.Count == 0);
        if (ImGui.Button("Remove Interpolator", buttonSize)) { Project.RemoveSelectedInterpolator(); }
        ImGui.EndDisabled();

        ImGui.BeginDisabled(Project.CurrentInterpolators.Count == 0);
        if (ImGui.BeginListBox("##Selected Modifier Interpolator ListBox", listBoxSize))
        {
            for (int i = 0; i < Project.CurrentInterpolators.Count; i++)
            {
                Interpolator interpolator = Project.CurrentInterpolators[i];
                bool isInterpolatorSelected = Project.SelectedInterpolator == interpolator;

                if (ImGui.Selectable($"{interpolator.Name}##{i}", isInterpolatorSelected))
                {
                    Project.SelectedInterpolator = interpolator;
                }
            }

            ImGui.EndListBox();
        }
        ImGui.EndDisabled();
    }

    private static void DrawChooseInterpolatorModal()
    {
        bool isOpen = true;

        if (ImGui.BeginPopupModal("Choose Interpolator Modal", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            for (int i = 0; i < InterpolatorFactory.InterpolatorTypeNames.Length; i++)
            {
                if (ImGui.Selectable(InterpolatorFactory.InterpolatorTypeNames[i]))
                {
                    Type interpolatorType = InterpolatorFactory.InterpolatorTypes[i];
                    Project.AddInterpolator(interpolatorType);
                    ImGui.CloseCurrentPopup();
                }
            }

            SysVec2 size = ImGui.GetWindowSize();
            SysVec2 pos = ImGui.GetMainViewport().GetCenter() - size * 0.5f;
            ImGui.SetWindowPos(pos);
            ImGui.EndPopup();
        }
    }

    private static void DrawSelectedInterpolatorInputs()
    {
        if (Project.SelectedInterpolator is null)
        {
            ImGui.TextDisabled("Create or Select an Interpolator to view its properties!");
            return;
        }

        DrawSelectedInterpolatorProperties();
    }

    #region Selected Interpolator Properties

    private static void DrawSelectedInterpolatorProperties()
    {
        ImGui.BeginTable("##Selected Interpolator Properties Table", 3, ImGuiTableFlags.SizingStretchSame);
        DrawSelectedInterpolatorNameInput();

        switch (Project.SelectedInterpolator)
        {
            case ColorInterpolator color:
                DrawSelectedInterpolatorInput(color);
                break;

            case HueInterpolator hue:
                DrawSelectedInterpolatorInput(hue);
                break;

            case OpacityInterpolator opacity:
                DrawSelectedInterpolatorInput(opacity);
                break;

            case RotationInterpolator rotation:
                DrawSelectedInterpolatorInput(rotation);
                break;

            case ScaleInterpolator scale:
                DrawSelectedInterpolatorInput(scale);
                break;

            case VelocityInterpolator velocity:
                DrawSelectedInterpolatorInput(velocity);
                break;
        }

        ImGui.EndTable();
    }

    private static void DrawSelectedInterpolatorNameInput()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Name");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##Selected Interpolator Name Input", ref Project.SelectedInterpolator.Name, 256, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedInterpolatorInput(ColorInterpolator interpolator)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Start Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.ColorEdit3("##Selected Interpolator Color Start Value", ref interpolator.StartValue, ImGuiColorEditFlags.InputRGB);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("End Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.ColorEdit3("##Selected Interpolator Color End Value", ref interpolator.EndValue, ImGuiColorEditFlags.InputRGB);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedInterpolatorInput(HueInterpolator interpolator)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Start Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Interpolator Hue Start Value", ref interpolator.StartValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("End Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Interpolator Hue End Value", ref interpolator.EndValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedInterpolatorInput(OpacityInterpolator interpolator)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Start Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Interpolator Opacity Start Value", ref interpolator.StartValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("End Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Interpolator Opacity End Value", ref interpolator.EndValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedInterpolatorInput(RotationInterpolator interpolator)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Start Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Interpolator Rotation Start Value", ref interpolator.StartValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("End Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Interpolator Rotation End Value", ref interpolator.EndValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedInterpolatorInput(ScaleInterpolator interpolator)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Start Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Interpolator Scale Start Value", ref interpolator.StartValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("End Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat("##Selected Interpolator Scale End Value", ref interpolator.EndValue, 0, 0, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    private static void DrawSelectedInterpolatorInput(VelocityInterpolator interpolator)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Start Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat2("##Selected Interpolator Velocity Start Value", ref interpolator.StartValue, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("End Value");
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputFloat2("##Selected Interpolator Velocity End Value", ref interpolator.EndValue, null, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGuiEx.OutlinePreviousItemIfActive();
    }

    #endregion  Selected Interpolator Properties
}
