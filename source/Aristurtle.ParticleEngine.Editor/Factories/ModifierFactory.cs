// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Aristurtle.ParticleEngine.Modifiers;
using Aristurtle.ParticleEngine.Modifiers.Containers;
using Aristurtle.ParticleEngine.Modifiers.Interpolators;

namespace Aristurtle.ParticleEngine.Editor.Factories;

public static class ModifierFactory
{
    public static Type[] ModifierTypes;
    public static string[] ModifierTypeNames;

    static ModifierFactory()
    {
        ModifierTypes = typeof(Modifier).Assembly.ExportedTypes.Where(t => t.BaseType == typeof(Modifier))
                                                               .OrderBy(t => t.Name)
                                                               .Select(t => t)
                                                               .ToArray();

        ModifierTypeNames = ModifierTypes.Select(m => m.Name).ToArray();
    }

    public static Modifier CreateModifier(Type modifierType)
    {
        return modifierType switch
        {
            Type t when t == typeof(RectangleLoopContainerModifier) => RectangleLoopContainerModifier(),
            Type t when t == typeof(RectangleContainerModifier) => RectangleContainerModifier(),
            Type t when t == typeof(LinearGravityModifier) => LinearGravityModifier(),
            Type t when t == typeof(VortexModifier) => VortexModifier(),
            Type t when t == typeof(OpacityFastFadeModifier) => OpacityFastFadeModifier(),
            Type t when t == typeof(AgeModifier) => AgeModifier(),
            Type t when t == typeof(CircleContainerModifier) => CircleContainerModifier(),
            Type t when t == typeof(DragModifier) => DragModifier(),
            Type t when t == typeof(RotationModifier) => RotationModifier(),
            Type t when t == typeof(VelocityColorModifier) => VelocityColorModifier(),
            Type t when t == typeof(VelocityModifier) => VelocityModifier(),
            _ => Activator.CreateInstance(modifierType) as Modifier
        };
    }

    private static RectangleLoopContainerModifier RectangleLoopContainerModifier() => new RectangleLoopContainerModifier() { Width = 100, Height = 100 };
    private static RectangleContainerModifier RectangleContainerModifier() => new RectangleContainerModifier { Width = 100, Height = 100 };
    private static LinearGravityModifier LinearGravityModifier() => new LinearGravityModifier() { Direction = Vec2.UnitY, Strength = 100.0f };
    private static VortexModifier VortexModifier() => new VortexModifier() { Mass = 100.0f, Position = new Vec2(100.0f, 100.0f), MaxSpeed = 100.0f };
    private static OpacityFastFadeModifier OpacityFastFadeModifier() => new OpacityFastFadeModifier();
    private static AgeModifier AgeModifier() => new AgeModifier() { Interpolators = new List<Interpolator>() { new ScaleInterpolator() { StartValue = 0.0f, EndValue = 1.0f } } };
    private static CircleContainerModifier CircleContainerModifier() => new CircleContainerModifier() { Radius = 100.0f };
    private static DragModifier DragModifier() => new DragModifier();
    private static RotationModifier RotationModifier() => new RotationModifier() { RotationRate = MathF.PI / 4.0f };
    private static VelocityColorModifier VelocityColorModifier() => new VelocityColorModifier() { VelocityThreshold = 100.0f, StationaryColor = new Vec3(0, 0, 1.0f), VelocityColor = new Vec3(0, 1.0f, 0.5f) };
    private static VelocityModifier VelocityModifier() => new VelocityModifier() { VelocityThreshold = 100.0f, Interpolators = new List<Interpolator>() { new ScaleInterpolator() { StartValue = 0.0f, EndValue = 1.0f } } };
}
