// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aristurtle.ParticleEngine.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aristurtle.ParticleEngine.Editor.Graphics;

public static class ParticleEffectRenderer
{
    public static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
    private static XnaRect _rect = XnaRect.Empty;

    public static void Unload()
    {
        foreach(Texture2D texture in Textures.Values)
        {
            texture.Dispose();
        }

        Textures.Clear();
    }

    public static void Draw(SpriteBatch spriteBatch, ParticleEffect particleEffect)
    {
        Debug.Assert(particleEffect is not null);
        Debug.Assert(particleEffect.IsDisposed is false);

        ReadOnlySpan<ParticleEmitter> emitters = CollectionsMarshal.AsSpan(particleEffect.Emitters);
        for(int i = 0; i < emitters.Length; i++)
        {
            Draw(spriteBatch, emitters[i]);
        }
    }

    public static void Draw(SpriteBatch spriteBatch, ParticleEmitter emitter)
    {
        Debug.Assert(emitter is not null);
        Debug.Assert(emitter.IsDisposed is false);
        UnsafeDraw(spriteBatch, emitter);
    }

    private static unsafe void UnsafeDraw(SpriteBatch spriteBatch, ParticleEmitter emitter)
    {
        Debug.Assert(spriteBatch is not null);

        if (string.IsNullOrEmpty(emitter.TextureKey)) { return; }

        if(!Textures.TryGetValue(emitter.TextureKey, out Texture2D texture))
        {
            Debug.Fail($"{nameof(ParticleEffectRenderer)} does not contain a texture named '{emitter.TextureKey}'.  Did you forget to add it?");
        }

        _rect.X = emitter.SourceRectangle?.X ?? texture.Bounds.X;
        _rect.Y = emitter.SourceRectangle?.Y ?? texture.Bounds.Y;
        _rect.Width = emitter.SourceRectangle?.Width ?? texture.Width;
        _rect.Height = emitter.SourceRectangle?.Height ?? texture.Height;

        XnaVec2 origin = _rect.Center.ToVector2();
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

            while(count-- > 0)
            {
                var (r, g, b) = ColorUtilities.HslToRgb(particle->Color);
                XnaColor color = new XnaColor(r, g, b);

                if(spriteBatch.GraphicsDevice.BlendState == BlendState.AlphaBlend)
                {
                    color *= particle->Opacity;
                }
                else
                {
                    color.A = (byte)(particle->Opacity * 255);
                }

                XnaVec2 position = new XnaVec2(particle->Position[0], particle->Position[1]);
                XnaVec2 scale = new XnaVec2(particle->Scale);
                color.A = (byte)MathHelper.Clamp(particle->Opacity * 255, 0, 255);
                float rotation = particle->Rotation;
                float layerDepth = particle->LayerDepth;

                spriteBatch.Draw(texture, position, _rect, color, rotation, origin, scale, SpriteEffects.None, layerDepth);

                particle++;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

    }
}
