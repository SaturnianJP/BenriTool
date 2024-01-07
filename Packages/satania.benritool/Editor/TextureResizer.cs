#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace satania.benritool
{
    public static class ExtensionClass
    {
        #region [File Save Utility]
        public static UnityEngine.Object SaveFile(this UnityEngine.Object asset, string filepath, string ex)
        {
            Type type = asset.GetType();

            string AssetPath = AssetDatabase.GetAssetPath(asset);
            if (AssetPath == null || string.IsNullOrEmpty(AssetPath))
                AssetDatabase.CreateAsset(asset, filepath + $".{ex}");
            else
                AssetDatabase.CopyAsset(AssetPath, filepath + $".{ex}");

            if (!File.Exists(filepath + $".{ex}"))
                return default;

            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath(filepath + $".{ex}", type);
        }
        #endregion

        public static Texture2D GetResized(this Texture2D texture, int width, int height)
        {
            // リサイズ後のサイズを持つRenderTextureを作成して書き込む
            var rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(texture, rt);

            // リサイズ後のサイズを持つTexture2Dを作成してRenderTextureから書き込む
            var preRT = RenderTexture.active;
            RenderTexture.active = rt;
            var ret = new Texture2D(width, height);
            ret.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            ret.Apply();
            RenderTexture.active = preRT;

            RenderTexture.ReleaseTemporary(rt);
            return ret;
        }
    }

    public class TextureResizer : EditorWindow
    {
        [MenuItem("Assets/便利ツール/画像変換/x512", true, 1120)]
        [MenuItem("Assets/便利ツール/画像変換/x1024", true, 1120)]
        [MenuItem("Assets/便利ツール/画像変換/x2048", true, 1120)]
        [MenuItem("Assets/便利ツール/画像変換/x4096", true, 1120)]
        private static bool CheckType()
        {
            if (Selection.activeObject == null) return false;

            return Selection.activeObject.GetType() == typeof(Texture2D);
        }

        [MenuItem("Assets/便利ツール/画像変換/x512", false, 1120)]
        private static void convert_512()
        {
            ResizeTex(512);
        }

        [MenuItem("Assets/便利ツール/画像変換/x1024", false, 1120)]
        private static void convert_1024()
        {
            ResizeTex(1024);
        }

        [MenuItem("Assets/便利ツール/画像変換/x2048", false, 1120)]
        private static void convert_2048()
        {
            ResizeTex(2048);
        }

        [MenuItem("Assets/便利ツール/画像変換/x4096", false, 1120)]
        private static void convert_4096()
        {
            ResizeTex(4096);
        }

        private static void ResizeTex(int resolution)
        {
            if (Selection.objects.Length == 0) return;

            for (int i = 0; i < Selection.objects.Length; i++)
            {
                var obj = Selection.objects[i];
                if (obj == null || obj.GetType() != typeof(Texture2D))
                    continue;

                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                bool isNormalMap = false;

                string filename = Path.GetFileNameWithoutExtension(path);
                string dir = Path.GetDirectoryName(path);
                string copiedPath = $"{dir}/{filename}_{resolution}_{resolution}.png";

                //Texture2Dでキャスト
                Texture2D tex = obj as Texture2D;

                #region 元の画像からピクセル情報を読み込めるようにする
                TextureImporter texImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                if (!texImporter.isReadable)
                    texImporter.isReadable = true;
                texImporter.textureCompression = TextureImporterCompression.Uncompressed;

                isNormalMap = texImporter.textureType == TextureImporterType.NormalMap;
                if (isNormalMap)
                {
                    texImporter.textureType = TextureImporterType.Default;
                }

                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                #endregion

                //リサイズ
                tex = tex.GetResized(resolution, resolution);

                #region リサイズした画像を保存
                byte[] pngData = tex.EncodeToPNG();
                File.WriteAllBytes(copiedPath, pngData);
                AssetDatabase.Refresh();
                #endregion

                #region 複製後のテクスチャのMaxSizeを変更
                var copiedtexImporter = AssetImporter.GetAtPath(copiedPath) as TextureImporter;
                EditorUtility.CopySerialized(texImporter, copiedtexImporter);

                copiedtexImporter.maxTextureSize = resolution;
                if (isNormalMap)
                {
                    copiedtexImporter.textureType = TextureImporterType.NormalMap;
                }
                
                AssetDatabase.WriteImportSettingsIfDirty(copiedPath);
                AssetDatabase.ImportAsset(copiedPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
                #endregion

                if (isNormalMap)
                {
                    texImporter.textureType = TextureImporterType.NormalMap;
                }

                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }
        }
    }
}

#endif