using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>
    /// Applies consistent import settings to everything under Assets/_Project/Art,
    /// so new art never needs manual Inspector setup.
    /// - Tiles and Obstacles: Sprite, 96 pixels per unit (keeps the Kenney tiles' relative sizes), no mipmaps.
    /// - UI:        Sprite, 100 PPU, 9-slice borders for panels/buttons so they stretch cleanly.
    /// - Particles: Sprite, max size 256 (source files are 512px, far bigger than needed).
    /// To change a value, edit it here and run Tools > Match3 > Reapply Art Import Settings.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_Project/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            if (assetPath.StartsWith(ArtRoot + "Tiles/") || assetPath.StartsWith(ArtRoot + "Obstacles/"))
            {
                importer.spritePixelsPerUnit = 96f;
                importer.maxTextureSize = 256;
            }
            else if (assetPath.StartsWith(ArtRoot + "Particles/"))
            {
                importer.spritePixelsPerUnit = 100f;
                importer.maxTextureSize = 256;
            }
            else if (assetPath.StartsWith(ArtRoot + "UI/"))
            {
                importer.spritePixelsPerUnit = 100f;
                importer.maxTextureSize = 512;
                ApplyUiSpriteSettings(importer, GetUiBorder(assetPath));
            }
        }

        // Border order: left, bottom, right, top (in pixels). Bottom is larger on "depth" sprites
        // because their shadow edge sits at the bottom.
        private static Vector4 GetUiBorder(string path)
        {
            if (path.EndsWith("ui_panel.png")) return new Vector4(24, 32, 24, 24);
            if (path.EndsWith("ui_slot.png")) return new Vector4(20, 20, 20, 20);
            if (path.Contains("ui_button_")) return new Vector4(24, 32, 24, 24);
            return Vector4.zero; // icons are not sliced
        }

        // Full Rect mesh: a SpriteRenderer drawing a 9-slice (Sliced) sprite needs it, or Unity logs a warning.
        private static void ApplyUiSpriteSettings(TextureImporter importer, Vector4 border)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = border;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        // Changing the version makes Unity re-import every texture with the new rules above, once.
        public override uint GetVersion()
        {
            return 2;
        }

        [MenuItem("Tools/Match3/Reapply Art Import Settings")]
        private static void ReapplyAll()
        {
            AssetDatabase.ImportAsset("Assets/_Project/Art",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("Match3: art import settings reapplied.");
        }
    }
}
