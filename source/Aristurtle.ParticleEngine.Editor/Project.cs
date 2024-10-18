// Released under The Unlicense.
// See LICENSE file in the project root for full license information.
// License information can also be found at https://unlicense.org/.

using System.Text.Json;
using Aristurtle.ParticleEngine.Editor.Factories;
using Aristurtle.ParticleEngine.Editor.Graphics;
using Aristurtle.ParticleEngine.Editor.IO;
using Aristurtle.ParticleEngine.Modifiers;
using Aristurtle.ParticleEngine.Modifiers.Interpolators;
using Aristurtle.ParticleEngine.Serialization.Json;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;

namespace Aristurtle.ParticleEngine.Editor;

public static class Project
{
    public const string VERSION = "0.0.1";
    public static readonly string DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\";
    public static string ProjectName = "ParticleEffectProject";
    public static string ProjectDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\";
    public static string ProjectFilePath = string.Empty;


    public static ParticleEffect ParticleEffect;
    public static ParticleEmitter SelectedEmitter;
    public static Modifier SelectedModifier;
    public static Interpolator SelectedInterpolator;

    public static List<Interpolator> CurrentInterpolators = new List<Interpolator>();

    public static void CreateNew()
    {
        string result = TinyFileDialog.SaveFile("Create New Project", DefaultDirectory, "*.particles", "Particle Effect");

        if (string.IsNullOrEmpty(result)) { return; }

        ProjectName = Path.GetFileNameWithoutExtension(result);
        ProjectFilePath = result;
        ProjectDirectory = Path.GetDirectoryName(result);

        if (ParticleEffect is not null)
        {
            ParticleEffect.Dispose();
        }

        ParticleEffect = new ParticleEffect(ProjectName);
        ParticleEffect.Position = ImGui.GetIO().DisplaySize * 0.5f;


    }

    public static void OpenExisting()
    {
        string result = TinyFileDialog.OpenFile("Open Existing Project", DefaultDirectory, "*.particles", "Particle Effect", false);

        if (string.IsNullOrEmpty(result)) { return; }

        ProjectName = Path.GetFileNameWithoutExtension(result);
        ProjectFilePath = result;
        ProjectDirectory = Path.GetDirectoryName(result);

        if (ParticleEffect is not null)
        {
            ParticleEffect.Dispose();
        }

        JsonSerializerOptions options = ParticleEffectJsonSerializerOptionsProvider.Default;
        string json = File.ReadAllText(result);
        ParticleEffect = JsonSerializer.Deserialize<ParticleEffect>(json, options);

        SelectedEmitter = ParticleEffect.Emitters.FirstOrDefault();

        if (SelectedEmitter is not null)
        {
            SelectedModifier = SelectedEmitter.Modifiers.FirstOrDefault();
        }

        switch (SelectedModifier)
        {
            case AgeModifier age:
                SelectedInterpolator = age.Interpolators.FirstOrDefault();
                break;
            case VelocityModifier velocity:
                SelectedInterpolator = velocity.Interpolators.FirstOrDefault();
                break;
        }

        ParticleEffectRenderer.Unload();

        foreach (ParticleEmitter emitter in ParticleEffect.Emitters)
        {
            string path = Path.Combine(ProjectDirectory, emitter.TextureKey);
            Texture2D texture = Texture2D.FromFile(Game1.GraphicsDevice, path);
            texture.Name = emitter.TextureKey;
            ParticleEffectRenderer.Textures.Add(texture.Name, texture);
        }

        ParticleEffect.Position = ImGui.GetIO().DisplaySize * 0.5f;
    }

    public static void Save()
    {
        JsonSerializerOptions options = ParticleEffectJsonSerializerOptionsProvider.Default;
        string json = JsonSerializer.Serialize<ParticleEffect>(ParticleEffect, options);
        File.WriteAllText(ProjectFilePath, json);
    }

    public static void Exit()
    {

    }

    public static void AddNewEmitter()
    {
        if (ParticleEffect is null) { return; }

        SelectedEmitter = ParticleEmitterFactory.CreateParticleEmitter();
        ParticleEffect.Emitters.Add(SelectedEmitter);
    }

    public static void RemoveSelectedEmitter()
    {
        if (ParticleEffect is null || SelectedEmitter is null) { return; }

        int index = ParticleEffect.Emitters.IndexOf(SelectedEmitter);
        ParticleEffect.Emitters.RemoveAt(index);
        index = Math.Max(0, index - 1);
        SelectedEmitter = ParticleEffect.Emitters.ElementAtOrDefault(index);
    }

    public static void AddModifier(Type type)
    {
        if (SelectedEmitter is null) { return; }

        SelectedModifier = ModifierFactory.CreateModifier(type);
        SelectedEmitter.Modifiers.Add(SelectedModifier);
        ChangeInterpolatorCollection();
    }

    public static void RemoveSelectedModifier()
    {
        if (SelectedEmitter is null || SelectedModifier is null) { return; }

        int index = SelectedEmitter.Modifiers.IndexOf(SelectedModifier);
        SelectedEmitter.Modifiers.RemoveAt(index);
        index = Math.Max(0, index - 1);
        SelectedModifier = SelectedEmitter.Modifiers.ElementAtOrDefault(index);
        ChangeInterpolatorCollection();
    }

    private static void ChangeInterpolatorCollection()
    {
        switch (SelectedModifier)
        {
            case AgeModifier age:
                CurrentInterpolators = age.Interpolators;
                break;
            case VelocityModifier velocity:
                CurrentInterpolators = velocity.Interpolators;
                break;
            default:
                CurrentInterpolators = null;
                break;
        }

        if (CurrentInterpolators is not null)
        {
            SelectedInterpolator = CurrentInterpolators.FirstOrDefault();
        }
    }

    public static void AddInterpolator(Type type)
    {
        if (CurrentInterpolators is null) { return; }

        SelectedInterpolator = InterpolatorFactory.CreateInterpolator(type);
        CurrentInterpolators.Add(SelectedInterpolator);
    }

    public static void RemoveSelectedInterpolator()
    {
        if (CurrentInterpolators is null || SelectedInterpolator is null) { return; }

        int index = CurrentInterpolators.IndexOf(SelectedInterpolator);
        CurrentInterpolators.RemoveAt(index);
        index = Math.Max(0, index - 1);
        SelectedInterpolator = CurrentInterpolators.ElementAtOrDefault(index);
    }

    public static void AddNewTexture()
    {
        if (SelectedEmitter is null) { return; }

        string result = TinyFileDialog.OpenFile("Choose Texture", DefaultDirectory, "*.png,*.jpg,*.tif", "Image Files", false);

        if (string.IsNullOrEmpty(result)) { return; }

        string textureKey = Path.GetFileName(result);

        //  If the image file they choose is already in the project directory, check to see if it's already loaded.
        //  If not already loaded, load it first, then set the texture key of the selected emitter to that texture.
        if(Path.GetDirectoryName(result).Equals(ProjectDirectory, StringComparison.OrdinalIgnoreCase))
        {
            if(!ParticleEffectRenderer.Textures.ContainsKey(textureKey))
            {
                Texture2D texture = Texture2D.FromFile(Game1.GraphicsDevice, result);
                texture.Name = textureKey;
                ParticleEffectRenderer.Textures.Add(textureKey, texture);
            }

            SelectedEmitter.TextureKey = textureKey;
            return;
        }

        //  If they image file they choose is not in the project directory, this means we'll need to copy it to the
        //  project directory.  However, if a file already exists in the project directory with that name, we'll need
        //  to prompt for approval to overwrite it
        string existing = Path.Combine(ProjectDirectory, textureKey);
        if(File.Exists(existing))
        {
            string choice = TinyFileDialog.MessageBox("Overwrite Existing?", $"{existing} already exists.\nDo you want to replace it?", "yesno", "warning", 0);
            if (choice == "no") { return; }
            ParticleEffectRenderer.Textures[textureKey].Dispose();
            ParticleEffectRenderer.Textures.Remove(textureKey);
        }

        File.Copy(result, existing, true);
        Texture2D newTexture = Texture2D.FromFile(Game1.GraphicsDevice, existing);
        ParticleEffectRenderer.Textures.Add(textureKey, newTexture);
        SelectedEmitter.TextureKey = textureKey;
    }
}
