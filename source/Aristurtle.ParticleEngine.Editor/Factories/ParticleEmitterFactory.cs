// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Aristurtle.ParticleEngine.Data;

namespace Aristurtle.ParticleEngine.Editor.Factories;

public static class ParticleEmitterFactory
{
    private static int _id;

    public static ParticleEmitter CreateParticleEmitter()
    {
        _id++;
        ParticleEmitter emitter = new ParticleEmitter();
        emitter.Name = $"emitter{_id:00}";

        emitter.Parameters.Color = new ParticleColorParameter()
        {
            Constant = new Vec3(0.0f, 0.0f, 1.0f),
            RandomMin = new Vec3(0.0f, 0.0f, 0.0f),
            RandomMax = new Vec3(0.0f, 0.0f, 1.0f),
            Kind = ParticleValueKind.Constant
        };

        emitter.Parameters.Opacity = new ParticleFloatParameter()
        {
            Constant = 1.0f,
            RandomMin = 0.0f,
            RandomMax = 1.0f,
            Kind = ParticleValueKind.Constant
        };

        emitter.Parameters.Rotation = new ParticleFloatParameter()
        {
            Constant = MathF.PI / 2.0f,
            RandomMin = -MathF.PI / 2.0f,
            RandomMax = MathF.PI / 2.0f,
            Kind = ParticleValueKind.Constant
        };

        emitter.Parameters.Speed = new ParticleFloatParameter()
        {
            Constant = 50.0f,
            RandomMin = 50.0f,
            RandomMax = 100.0f,
            Kind = ParticleValueKind.Constant
        };

        emitter.Parameters.Scale = new ParticleFloatParameter()
        {
            Constant = 1.0f,
            RandomMin = 0.0f,
            RandomMax = 1.0f,
            Kind = ParticleValueKind.Constant
        };

        emitter.Parameters.Mass = new ParticleFloatParameter()
        {
            Constant = 1.0f,
            RandomMin = 0.0f,
            RandomMax = 1.0f,
            Kind = ParticleValueKind.Constant
        };

        emitter.Parameters.Quantity = new ParticleInt32Parameter()
        {
            Constant = 5,
            RandomMin = 5,
            RandomMax = 10,
            Kind = ParticleValueKind.Constant
        };

        return emitter;
    }
}
