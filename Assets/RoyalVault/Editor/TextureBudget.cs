using UnityEditor;
using UnityEngine;

namespace RoyalVault.EditorTools
{
    // Explicit, high-quality compression for every game texture and sound, so nothing is left uncompressed by default
    public static class TextureBudget
    {
        private const string Art = "Assets/RoyalVault/Art";
        private const string PrivateArt = "Assets/RoyalVault/Art.Private";
        private const float ShortSoundSeconds = 1.5f;

        private static readonly string[] Paintings = { "Art/Machine/bg", "Art/Story/scene_" };

        [MenuItem("Royal Vault/Apply Texture and Audio Budget")]
        public static void Apply()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", Folders()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && ApplyTexture(importer, path))
                {
                    changed++;
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", Folders()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is AudioImporter importer && ApplyAudio(importer, path))
                {
                    changed++;
                }
            }

            Debug.Log("Royal Vault: compression settings updated on " + changed + " assets");
        }

        private static string[] Folders()
        {
            return AssetDatabase.IsValidFolder(PrivateArt) ? new[] { Art, PrivateArt } : new[] { Art };
        }

        private static bool ApplyTexture(TextureImporter importer, string path)
        {
            bool painting = IsPainting(path);
            bool changed = false;
            if (importer.mipmapEnabled || importer.isReadable)
            {
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                changed = true;
            }

            changed |= Platform(importer, "Standalone", TextureImporterFormat.BC7);
            changed |= Platform(importer, "Android", painting ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4);
            changed |= Platform(importer, "iPhone", painting ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4);
            if (changed)
            {
                importer.SaveAndReimport();
            }

            return changed;
        }

        private static bool Platform(TextureImporter importer, string platform, TextureImporterFormat format)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            bool same = settings.overridden && settings.format == format && settings.maxTextureSize == 4096;
            if (same)
            {
                return false;
            }

            settings.overridden = true;
            settings.format = format;
            settings.maxTextureSize = 4096;
            settings.compressionQuality = 100;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        // Large opaque paintings compress with bigger blocks; their soft detail hides it
        private static bool IsPainting(string path)
        {
            foreach (string painting in Paintings)
            {
                if (path.Contains(painting))
                {
                    return true;
                }
            }

            return false;
        }

        // Music streams; short effects load decompressed so they start at once; longer effects stay compressed in memory
        private static bool ApplyAudio(AudioImporter importer, string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                return false;
            }

            bool music = System.IO.Path.GetFileName(path).StartsWith("Music_", System.StringComparison.OrdinalIgnoreCase);
            AudioClipLoadType load = music ? AudioClipLoadType.Streaming
                : clip.length <= ShortSoundSeconds ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType == load && settings.compressionFormat == AudioCompressionFormat.Vorbis)
            {
                return false;
            }

            settings.loadType = load;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = music;
            importer.SaveAndReimport();
            return true;
        }
    }
}
