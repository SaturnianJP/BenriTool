#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace satania.benritool
{
    public class BenriToolImporter : AssetPostprocessor
    {
        #region 設定用
        private static string s_IsModelAutoSetter = "IsModelAutoSetter";
        private static string s_IsTextureAutoSetter = "IsTextureAutoSetter";

        private static List<string> SkipList = new List<string>();
        #endregion[

        #region 関数
        private static bool GetSettingValue(string name)
        {
            string value = EditorUserSettings.GetConfigValue(name);
            return !string.IsNullOrEmpty(value) && value.Equals("True");
        }

        private static void SetSettingValue(string name, bool value)
        {
            EditorUserSettings.SetConfigValue(name, value.ToString());
        }

        #endregion

        #region FBX 関数
        private static bool isModelValidSetting(ModelImporter mdlimporter)
        {
            if (mdlimporter == null)
                return false;

            if (mdlimporter.importCameras || mdlimporter.importLights)
                return false;

            if (!mdlimporter.isReadable)
                return false;

            if (mdlimporter.importBlendShapeNormals != ModelImporterNormals.None)
                return false;

            return true;
        }

        private static void SetUpFBX(string asset)
        {
            ModelImporter _modelImporter = AssetImporter.GetAtPath(asset) as ModelImporter;
            if (isModelValidSetting(_modelImporter))
                return;

            _modelImporter.importCameras = false;
            _modelImporter.importLights = false;
            _modelImporter.isReadable = true;
            _modelImporter.importBlendShapeNormals = ModelImporterNormals.None;

            _modelImporter.SaveAndReimport();
        }
        #endregion

        #region PNG 関数
        private static bool isTransparencyTexture(Texture2D tex)
        {
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    Color col = tex.GetPixel(x, y);
                    if (col.a < 1)
                        return true;
                }
            }

            return false;
        }

        private static bool isTextureValidSetting(TextureImporter texImporter)
        {
            if (texImporter == null)
                return false;

            if (!texImporter.streamingMipmaps || !texImporter.crunchedCompression)
                return false;

            if (texImporter.textureCompression != TextureImporterCompression.CompressedHQ)
                return false;

            if (texImporter.compressionQuality != 100)
                return false;

            return true;
        }

        private static void SetUpPNG(string asset)
        {
            TextureImporter _texImporter = AssetImporter.GetAtPath(asset) as TextureImporter;
            if (isTextureValidSetting(_texImporter))
                return;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
            _texImporter.alphaIsTransparency = isTransparencyTexture(texture);

            _texImporter.streamingMipmaps = true;
            _texImporter.crunchedCompression = true;
            _texImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            _texImporter.compressionQuality = 100;

            _texImporter.SaveAndReimport();
        }
        #endregion

        void OnPreprocessAsset()
        {
            if (!assetImporter.importSettingsMissing)
            {
                SkipList.Add(assetPath);
            }
        }

        // 全てのアセットのインポートが完了した後に呼び出される
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                if (SkipList.Contains(asset))
                    continue;

                string extension = Path.GetExtension(asset);

                if (extension == ".fbx" || extension == ".FBX")
                {
                    if (GetSettingValue(s_IsModelAutoSetter))
                        SetUpFBX(asset);
                }

                if (extension == ".png" || extension == ".PNG")
                {
                    if (GetSettingValue(s_IsTextureAutoSetter))
                        SetUpPNG(asset);
                }
            }

            SkipList.Clear();
        }

        void OnPreprocessTexture()
        {
            if (GetSettingValue(s_IsTextureAutoSetter))
            {
                // AssetImporterをTextureImporterにキャスト
                var importer = (TextureImporter)assetImporter;

                importer.isReadable = true;
            }
        }
    }
}
#endif