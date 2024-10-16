// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Aristurtle.ParticleEngine.Profiles;

namespace Aristurtle.ParticleEngine.Editor.Factories;

public static class ParticleProfileFactory
{

    public static Type[] ProfileTypes;
    public static string[] ProfileTypeNames;

    static ParticleProfileFactory()
    {
        ProfileTypes = typeof(Profile).Assembly.ExportedTypes.Where(t => t.BaseType == typeof(Profile))
                                                             .OrderBy(t => t.Name)
                                                             .Select(t => t)
                                                             .ToArray();

        ProfileTypeNames = ProfileTypes.Select(p => p.Name).ToArray();
    }

    public static Profile CreateProfile(Type profileType)
    {
        return profileType switch
        {
            Type t when t == typeof(CircleProfile) => CircleProfile(),
            Type t when t == typeof(BoxProfile) => BoxProfile(),
            Type t when t == typeof(BoxFillProfile) => BoxFillProfile(),
            Type t when t == typeof(BoxUniformProfile) => BoxUniformProfile(),
            Type t when t == typeof(LineProfile) => LineProfile(),
            Type t when t == typeof(RingProfile) => RingProfile(),
            Type t when t == typeof(SprayProfile) => SprayProfile(),
            _ => Activator.CreateInstance(profileType) as Profile
        };
    }

    private static CircleProfile CircleProfile() => new CircleProfile() { Radius = 100.0f };
    private static BoxProfile BoxProfile() => new BoxProfile() { Width = 100.0f, Height = 100.0f };
    private static BoxFillProfile BoxFillProfile() => new BoxFillProfile() { Width = 100.0f, Height = 100.0f };
    private static BoxUniformProfile BoxUniformProfile() => new BoxUniformProfile() { Width = 100.0f, Height = 100.0f };
    private static LineProfile LineProfile() => new LineProfile() { Axis = SysVec2.UnitX, Length = 100.0f };
    private static RingProfile RingProfile() => new RingProfile() { Radius = 100.0f };
    private static SprayProfile SprayProfile() => new SprayProfile() { Direction = -SysVec2.UnitY, Spread = MathF.PI / 2.0f };
}
