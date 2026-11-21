using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace RoyalVault.EditorTools
{
    // Packs the reel symbols into atlases so the reels batch: one for still symbols, one for their motion blur
    public static class SpriteAtlases
    {
        private const string PublicAtlases = "Assets/RoyalVault/Art/Atlases";
        private const string PrivateArt = "Assets/RoyalVault/Art.Private";
        private const string SymbolArt = "Assets/RoyalVault/Art/Symbols";
        private const int Padding = 8;
        private const int PageSize = 2048;

        [MenuItem("Royal Vault/Pack Sprite Atlases")]
        public static void Pack()
        {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            SetMeshType(SymbolArt, SpriteMeshType.FullRect);
            Create(PublicAtlases, "Symbols", Sprites(SymbolArt));
            Create(PublicAtlases, "SymbolsBlur", Sprites(SymbolArt + "/Blur"));
            // baked animation frames stay plain rectangles: tight outlines would rebuild thousands of vertices each frame
            foreach (string folder in AnimationFolders())
            {
                SetMeshType(folder, SpriteMeshType.FullRect);
            }
        }

        // Every folder of baked frames: the characters and the animated symbols
        private static string[] AnimationFolders()
        {
            var folders = new List<string>();
            foreach (string parent in new[] { PrivateArt + "/Characters", PrivateArt + "/Symbols" })
            {
                if (AssetDatabase.IsValidFolder(parent))
                {
                    folders.AddRange(AssetDatabase.GetSubFolders(parent));
                }
            }

            return folders.ToArray();
        }

        // Plain rectangles keep vertex counts low; 504 px symbols with 8 px padding fill a 2048 page exactly
        private static void SetMeshType(string folder, SpriteMeshType type)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType == type)
                {
                    continue;
                }

                settings.spriteMeshType = type;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        // Still symbols and their motion-blur copies get separate atlases: reels show one set or the other
        private static Object[] Sprites(string folder)
        {
            var sprites = new List<Object>();
            foreach (string path in Directory.GetFiles(folder, "*.png"))
            {
                sprites.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\', '/')));
            }

            return sprites.ToArray();
        }

        private static void Create(string atlasFolder, string name, Object[] packables)
        {
            Directory.CreateDirectory(atlasFolder);
            AssetDatabase.Refresh();
            string path = atlasFolder + "/" + name + ".spriteatlasv2";
            if (AssetImporter.GetAtPath(path) == null)
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(packables);
                SpriteAtlasAsset.Save(atlas, path);
                AssetDatabase.ImportAsset(path);
            }

            Configure((SpriteAtlasImporter)AssetImporter.GetAtPath(path));
        }

        private static void Configure(SpriteAtlasImporter importer)
        {
            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                padding = Padding,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                generateMipMaps = false,
                readable = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            };
            importer.SetPlatformSettings(Platform("Standalone", TextureImporterFormat.BC7));
            importer.SetPlatformSettings(Platform("Android", TextureImporterFormat.ASTC_4x4));
            importer.SetPlatformSettings(Platform("iPhone", TextureImporterFormat.ASTC_4x4));
            importer.SaveAndReimport();
        }

        private static TextureImporterPlatformSettings Platform(string name, TextureImporterFormat format)
        {
            return new TextureImporterPlatformSettings
            {
                name = name,
                overridden = true,
                maxTextureSize = PageSize,
                format = format,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100,
            };
        }
    }
}
