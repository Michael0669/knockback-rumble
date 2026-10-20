using UnityEditor;
using UnityEngine;

namespace KnockbackRumble.EditorTools
{
    /// <summary>
    /// Import settings for the menu art in Art/Resources/UI: crisp, uncompressed, no mipmaps,
    /// transparent edges handled properly. The stripes texture tiles.
    /// </summary>
    public class UiTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("KnockbackRumble/Art/Resources/UI/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.wrapMode = assetPath.EndsWith("stripes.png") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        }
    }
}
