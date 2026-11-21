using UnityEditor;
using UnityEngine;

namespace RoyalVault.EditorTools
{
    /// <summary>
    /// First-import settings for the game's art: UI sprites at full quality without mipmaps, 9-slice borders for the
    /// pieces that stretch (reel frame, pills, popup, level bar), and the character pivoted on its feet. Only images
    /// without import settings are touched, so later changes in the Inspector are kept.
    /// </summary>
    public sealed class ArtImporter : AssetPostprocessor
    {
        private const string Art = "Assets/RoyalVault/Art";

        /// <summary>Licensed audio: music streams from disk, effects load decompressed so they start instantly.</summary>
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Art + ".Private/Audio") || !assetImporter.importSettingsMissing) return;
            var importer = (AudioImporter)assetImporter;
            bool music = System.IO.Path.GetFileName(assetPath).StartsWith("Music_", System.StringComparison.OrdinalIgnoreCase);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = music ? 0.6f : 0.8f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = music;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Art) || !assetImporter.importSettingsMissing) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = assetPath.Contains("/Characters/") ? (int)SpriteAlignment.BottomCenter : (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);

            // 9-slice borders: left, bottom, right, top (in the art's pixels)
            if (assetPath.EndsWith("Machine/reel_frame.png")) importer.spriteBorder = new Vector4(137, 130, 139, 136);
            else if (assetPath.EndsWith("UI/popup_panel.png")) importer.spriteBorder = new Vector4(170, 170, 170, 170);
            else if (assetPath.EndsWith("UI/level_bar.png")) importer.spriteBorder = new Vector4(65, 0, 65, 0);
            else if (assetPath.EndsWith("Generated/pill.png")) importer.spriteBorder = new Vector4(62, 62, 62, 62);
        }
    }
}
