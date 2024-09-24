// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Aristurtle.ParticleEngine.Modifiers.Interpolators;

namespace Aristurtle.ParticleEngine.Editor.Factories;

public static class InterpolatorFactory
{
    public static Type[] InterpolatorTypes;
    public static string[] InterpolatorTypeNames;

    static InterpolatorFactory()
    {
        InterpolatorTypes = typeof(Interpolator).Assembly.ExportedTypes.Where(t => t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(Interpolator<>))
                                                                       .OrderBy(t => t.Name)
                                                                       .Select(t => t)
                                                                       .ToArray();

        InterpolatorTypeNames = InterpolatorTypes.Select(i => i.Name).ToArray();
    }

    public static Interpolator CreateInterpolator(Type interpolatorType)
    {
        return interpolatorType switch
        {
            Type t when t == typeof(ColorInterpolator) => ColorInterpolator(),
            Type t when t == typeof(HueInterpolator) => HueInterpolator(),
            Type t when t == typeof(OpacityInterpolator) => OpacityInterpolator(),
            Type t when t == typeof(RotationInterpolator) => RotationInterpolator(),
            Type t when t == typeof(ScaleInterpolator) => ScaleInterpolator(),
            _ => Activator.CreateInstance(interpolatorType) as Interpolator
        };
    }

    private static ColorInterpolator ColorInterpolator() => new ColorInterpolator() { StartValue = new Vec3(0.0f, 0.0f, 0.0f), EndValue = new Vec3(0.0f, 0.0f, 1.0f) };
    private static HueInterpolator HueInterpolator() => new HueInterpolator() { StartValue = 0.0f, EndValue = 1.0f };
    private static OpacityInterpolator OpacityInterpolator() => new OpacityInterpolator() { StartValue = 0.0f, EndValue = 1.0f };
    private static RotationInterpolator RotationInterpolator() => new RotationInterpolator() { StartValue = 0.0f, EndValue = MathF.PI / 2.0f };
    private static ScaleInterpolator ScaleInterpolator() => new ScaleInterpolator() { StartValue = 1.0f, EndValue = 0.0f };
}
