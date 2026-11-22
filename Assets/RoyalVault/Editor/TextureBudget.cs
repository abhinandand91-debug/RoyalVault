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

        private const int MaxSize = 4096;
        // Phones cannot show more detail than this on a full-screen background
        private const int MobileBackgroundSize = 2048;

        private static readonly string[] Paintings = { "Art/Machine/bg", "Art/Story/scene_" };
        // Animated characters and symbols: many large frames, soft painted detail
        private static readonly string[] Frames = { "Art.Private/Characters/", "Art.Private/Symbols/" };
        private static readonly string[] Backgrounds = { "Art/Machine/bg" };

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
            bool lighter = Matches(path, Paintings) || Matches(path, Frames);
            TextureImporterFormat mobile = lighter ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
            int mobileSize = Matches(path, Backgrounds) ? MobileBackgroundSize : MaxSize;
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

            changed |= Platform(importer, "Standalone", TextureImporterFormat.BC7, MaxSize);
            changed |= Platform(importer, "Android", mobile, mobileSize);
            changed |= Platform(importer, "iPhone", mobile, mobileSize);
            if (changed)
            {
                importer.SaveAndReimport();
            }

            return changed;
        }

        private static bool Platform(TextureImporter importer, string platform, TextureImporterFormat format, int maxSize)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            bool same = settings.overridden && settings.format == format && settings.maxTextureSize == maxSize;
            if (same)
            {
                return false;
            }

            settings.overridden = true;
            settings.format = format;
            settings.maxTextureSize = maxSize;
            settings.compressionQuality = 100;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        // Large paintings and animation frames compress with bigger blocks on mobile; their soft detail hides it
        private static bool Matches(string path, string[] folders)
        {
            foreach (string folder in folders)
            {
                if (path.Contains(folder))
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
