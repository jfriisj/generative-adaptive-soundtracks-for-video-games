#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Uee.SpriteExtractor
{
    public static class SpriteExtractor
    {
        #region HELPER METHODS

        private static ISpriteEditorDataProvider GetSpriteDateProvider(Object selectedObject)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var spriteDataProvider = factory.GetSpriteEditorDataProviderFromObject(selectedObject);
            spriteDataProvider.InitSpriteEditorDataProvider();
            return spriteDataProvider;
        }

        #endregion

        #region METHODS

        [MenuItem("Assets/Extract/Sprites/Extract All Sprites", priority = 10)]
        public static void ExtractAllSprites()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject is not Texture2D texture2D) return;

            var assetPath = AssetDatabase.GetAssetPath(selectedObject);
            var path = Path.GetDirectoryName(assetPath);
            var format = Path.GetExtension(assetPath);

            var spriteDataProvider = GetSpriteDateProvider(selectedObject);
            var spriteRects = spriteDataProvider.GetSpriteRects();
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (!textureImporter!.isReadable)
            {
                textureImporter.isReadable = true;
                textureImporter.SaveAndReimport();
            }

            foreach (var spriteRect in spriteRects)
            {
                var newTexture = texture2D.GetTextureFromBounds(
                    (int)spriteRect.rect.x,
                    (int)spriteRect.rect.y,
                    (int)spriteRect.rect.width,
                    (int)spriteRect.rect.height);
                newTexture.filterMode = textureImporter.filterMode;
                newTexture.Apply();
                var pngTexture = newTexture.EncodeTexture2D(format);
                File.WriteAllBytes($"{Path.Combine(path!, spriteRect.name)}{format}", pngTexture);
            }

            textureImporter.isReadable = false;
            textureImporter.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Extract/Sprites/Extract Selected Sprites", priority = 10)]
        public static void ExtractSprites()
        {
            var selectedObjects = Selection.objects;
            if (selectedObjects.Length == 0) return;

            var assetPath = AssetDatabase.GetAssetPath(selectedObjects[0]);
            var path = Path.GetDirectoryName(assetPath);
            var format = Path.GetExtension(assetPath);

            var spriteDataProvider = GetSpriteDateProvider(selectedObjects[0]);
            var spriteRects = spriteDataProvider.GetSpriteRects().ToList();
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (!textureImporter!.isReadable)
            {
                textureImporter.isReadable = true;
                textureImporter.SaveAndReimport();
            }

            foreach (var selectedObject in selectedObjects)
            {
                if (selectedObject is not Sprite sprite) continue;

                var spriteRect =
                    spriteRects.Find(spriteRect => string.CompareOrdinal(spriteRect.name, sprite.name) == 0);
                var newTexture = sprite.texture.GetTextureFromBounds(
                    (int)spriteRect.rect.x,
                    (int)spriteRect.rect.y,
                    (int)spriteRect.rect.width,
                    (int)spriteRect.rect.height);
                newTexture.filterMode = textureImporter.filterMode;
                newTexture.Apply();
                var pngTexture = newTexture.EncodeTexture2D(format);
                File.WriteAllBytes($"{Path.Combine(path!, spriteRect.name)}{format}", pngTexture);
            }

            textureImporter.isReadable = false;
            textureImporter.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        #endregion
    }
}
#endif