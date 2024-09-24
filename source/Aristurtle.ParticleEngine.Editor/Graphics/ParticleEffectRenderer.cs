﻿// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using System.Runtime.InteropServices;
using Aristurtle.ParticleEngine.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aristurtle.ParticleEngine.Editor.Graphics;

public sealed class ParticleEffectRenderer : IDisposable
{
    public Dictionary<string, Texture2D> Textures;
    public bool IsDisposed { get; private set; }

    public ParticleEffectRenderer()
    {
        Textures = new Dictionary<string, Texture2D>();
    }

    ~ParticleEffectRenderer() => Dispose(false);

    public void Draw(SpriteBatch spriteBatch, ParticleEffect particleEffect)
    {
        ArgumentNullException.ThrowIfNull(particleEffect);
        ObjectDisposedException.ThrowIf(particleEffect.IsDisposed, particleEffect);

        ReadOnlySpan<ParticleEmitter> emitters = CollectionsMarshal.AsSpan(particleEffect.Emitters);
        for (int i = 0; i < emitters.Length; i++)
        {
            Draw(spriteBatch, emitters[i]);
        }
    }

    public void Draw(SpriteBatch spriteBatch, ParticleEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        UnsafeDraw(spriteBatch, emitter);
    }

    private unsafe void UnsafeDraw(SpriteBatch spriteBatch, ParticleEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        if (string.IsNullOrEmpty(emitter.TextureKey))
        {
            return;
        }

        if (!Textures.TryGetValue(emitter.TextureKey, out Texture2D texture))
        {
            throw new InvalidOperationException($"{nameof(ParticleEffectRenderer)} does not contain a texture named '{emitter.TextureKey}'.  Did you forget to add it?");
        }

        Rectangle sourceRect;
        sourceRect.X = emitter.SourceRectangle?.X ?? texture.Bounds.X;
        sourceRect.Y = emitter.SourceRectangle?.Y ?? texture.Bounds.Y;
        sourceRect.Width = emitter.SourceRectangle?.Width ?? texture.Bounds.Width;
        sourceRect.Height = emitter.SourceRectangle?.Height ?? texture.Bounds.Height;

        Vector2 origin = sourceRect.Center.ToVector2();
        int count = emitter.ActiveParticles;

        IntPtr buffer = Marshal.AllocHGlobal(emitter.Buffer.ActiveSizeInBytes);


        try
        {
            if(emitter.RenderingOrder == ParticleRenderingOrder.FrontToBack)
            {
                emitter.Buffer.CopyToReverse(buffer);
            }
            else
            {
                emitter.Buffer.CopyTo(buffer);
            }

            Particle* particle = (Particle*)buffer;

            while (count-- > 0)
            {
                var (r, g, b) = ColorUtilities.HslToRgb(particle->Color);
                Color color = new Color(r, g, b);

                if (spriteBatch.GraphicsDevice.BlendState == BlendState.AlphaBlend)
                {
                    color *= particle->Opacity;
                }
                else
                {
                    color.A = (byte)(particle->Opacity * 255);
                }

                Vector2 position = new Vector2(particle->Position[0], particle->Position[1]);
                Vector2 scale = new Vector2(particle->Scale);
                color.A = (byte)MathHelper.Clamp(particle->Opacity * 255, 0, 255);
                float rotation = particle->Rotation;
                float layerDepth = particle->LayerDepth;

                spriteBatch.Draw(texture, position, sourceRect, color, rotation, origin, scale, SpriteEffects.None, layerDepth);

                particle++;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        if (disposing)
        {
            Textures.Clear();
        }

        IsDisposed = true;
    }
}
