using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace satania.benritool
{
    public class BenriTool : EditorWindow
    {
        #region 定義系

        //エディタのタイトル
        public static string EditorTitle = "衣装作成者用 便利ツール";

        private bool _isModelAutoSetter = false;
        private bool _isTextureAutoSetter = false;

        private GameObject _ref_prefab;
        private Dictionary<string, Material[]> _meshRenderers = new Dictionary<string, Material[]>();
        private Dictionary<string, Material[]> _skinRenderers = new Dictionary<string, Material[]>();

        public GameObject[] _prefabs;

        private string savePath = "Assets/さたにあしょっぴんぐ/BenriTool/Generate/";
        #endregion

        #region 設定用
        private string s_IsModelAutoSetter = "IsModelAutoSetter";
        private string s_IsTextureAutoSetter = "IsTextureAutoSetter";
        #endregion

        #region 関数
        private bool GetSettingValue(string name)
        {
            string value = EditorUserSettings.GetConfigValue(s_IsModelAutoSetter);
            return !string.IsNullOrEmpty(value) && value.Equals("True");
        }

        private void SetSettingValue(string name, bool value)
        {
            EditorUserSettings.SetConfigValue(name, value.ToString());
        }

        private void Initialize()
        {
            _isModelAutoSetter = GetSettingValue(s_IsModelAutoSetter);
            _isTextureAutoSetter = GetSettingValue(s_IsTextureAutoSetter);
        }

        private void drawSizeLabel(string msg, int size, bool isBold = true)
        {
            GUIStyle RichText = new GUIStyle(EditorStyles.label);
            RichText.richText = true;

            if (isBold)
                GUILayout.Label($"<size={size}><b>{msg}</b></size>", RichText);
            else
                GUILayout.Label($"<size={size}>{msg}</size>", RichText);
        }

        public static bool IsExists(GameObject gameObject)
        {
            return gameObject.scene.IsValid();
        }

        /// <summary>
        /// 引用 : https://qiita.com/Milcia/items/ff7d9e1dffa28004efb7
        /// </summary>
        /// <param name="targetObj"></param>
        /// <returns></returns>
        private static string GetHierarchyPath(GameObject targetObj)
        {
            List<GameObject> objPath = new List<GameObject>();
            objPath.Add(targetObj);
            for (int i = 0; objPath[i].transform.parent != null; i++)
                objPath.Add(objPath[i].transform.parent.gameObject);
            string path = objPath[objPath.Count - 2].gameObject.name; //今回の場合avatar(先頭のオブジェクトが不要)なのでCount - 2にする。必要な場合は - 1 に変更
            for (int i = objPath.Count - 3; i >= 0; i--) //こっちもCount - 3にする。必要な場合は - 2にする
                path += "/" + objPath[i].gameObject.name;

            return path;
        }

        private void SetMaterials(GameObject to)
        {
            if (to == null)
                return;

            foreach (var mesh in _meshRenderers)
            {
                Transform findObject = to.transform.Find(mesh.Key);
                if (findObject == null)
                    continue;

                MeshRenderer meshRenderer = findObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sharedMaterials = mesh.Value;
                }
            }

            foreach (var skin in _skinRenderers)
            {
                Transform findObject = to.transform.Find(skin.Key);
                if (findObject == null)
                    continue;

                SkinnedMeshRenderer skinRenderer = findObject.GetComponent<SkinnedMeshRenderer>();
                if (skinRenderer != null)
                {
                    skinRenderer.sharedMaterials = skin.Value;
                }
            }

            EditorUtility.SetDirty(to);
        }
        #endregion

        #region Capture関数

        readonly int[,] prestSize = new int[,] { { 720, 480 }, { 1280, 720 }, { 1920, 1080 }, { 2560, 1440 }, { 3840, 2160 }, { 2048, 1080 }, { 4096, 2160 }, { 8192, 4320 } };
        private enum resPreset
        {
            [InspectorName("SD (720x480)")] SD,
            [InspectorName("HD (1280x720)")] HD,
            [InspectorName("FHD (1920x1080)")] FHD,
            [InspectorName("QHD (2560x1440)")] QHD,
            [InspectorName("UHD (3840x2160)")] UHD,
            [InspectorName("2k (2048x1080)")] _2k,
            [InspectorName("4k (4096x2160)")] _4k,
            [InspectorName("8k (8192x4320)")] _8k,
            [InspectorName("Custom")] Custom
        }

        private resPreset res = resPreset.FHD;

        private enum camOrientationPreset { Horizon, Vertical }
        private camOrientationPreset camOrientaion = camOrientationPreset.Horizon;

        private Camera camera;

        private enum BGType { Default, Skybox, Color, Transparent }
        private BGType bgType = BGType.Default;
        private Color bgColor = Color.white;

        Vector2 captureSize = new Vector2(1920, 1080);

        string ssSaveDir = "Assets/さたにあしょっぴんぐ/BenriTool/Capture/";

        private string doCapture()
        {
            Camera captureCamera = Instantiate(camera);

            int captureWidth = (int)Mathf.Round(captureSize.x);
            int captureHeight = (int)Mathf.Round(captureSize.y);

            string path = ssSaveDir;
            DirectoryInfo dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                Directory.CreateDirectory(path);
            }

            string time = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss (" + captureSize.x + "x" + captureSize.y + ")") + ".png";

            string name;
            name = path + time;

            TextureFormat format = (bgType == BGType.Transparent) ? TextureFormat.ARGB32 : TextureFormat.RGB24;
            Texture2D screenShot = new Texture2D(captureWidth, captureHeight, format, false);
            RenderTexture rt = new RenderTexture(captureWidth, captureHeight, 24);

            RenderTexture.active = rt;
            captureCamera.targetTexture = rt;
            if (bgType == BGType.Skybox)
            {
                captureCamera.clearFlags = CameraClearFlags.Skybox;
                captureCamera.backgroundColor = Color.white;
            }
            else if (bgType == BGType.Color)
            {
                captureCamera.clearFlags = CameraClearFlags.SolidColor;
                captureCamera.backgroundColor = bgColor;
            }
            else if (bgType == BGType.Transparent)
            {
                captureCamera.clearFlags = CameraClearFlags.SolidColor;
                captureCamera.backgroundColor = Color.clear;
            }
            captureCamera.Render();

            screenShot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            screenShot.Apply();

            byte[] bytes = screenShot.EncodeToPNG();
            File.WriteAllBytes(name, bytes);

            AssetDatabase.Refresh();

            DestroyImmediate(captureCamera.gameObject);

            return name;
        }
        #endregion

        #region エディタ描画系
        [MenuItem("さたにあ/便利ツール設定")]
        private static void Init()
        {
            //ウィンドウのインスタンスを生成
            BenriTool window = GetWindow<BenriTool>();

            //ウィンドウサイズを固定
            window.maxSize = window.minSize = new Vector2(512, 512);

            //タイトルを変更
            window.titleContent = new GUIContent(EditorTitle);
        }

        private void drawArray(string name)
        {
            ScriptableObject target = this;

            SerializedObject so = new SerializedObject(target);

            SerializedProperty stringsProperty = so.FindProperty("_prefabs");

            EditorGUILayout.PropertyField(stringsProperty, new GUIContent("コピーしたいプレハブたち"), true);

            so.ApplyModifiedProperties();
        }

        private void ShowGUI()
        {
            drawSizeLabel("インポート設定", 20, true);

            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                _isModelAutoSetter = EditorGUILayout.ToggleLeft("自動でFBXを最適化", _isModelAutoSetter);
                if (EditorGUI.EndChangeCheck())
                {
                    SetSettingValue(s_IsModelAutoSetter, _isModelAutoSetter);
                }

                EditorGUI.BeginChangeCheck();
                _isTextureAutoSetter = EditorGUILayout.ToggleLeft("自動でPNGを最適化", _isTextureAutoSetter);
                if (EditorGUI.EndChangeCheck())
                {
                    SetSettingValue(s_IsTextureAutoSetter, _isTextureAutoSetter);
                }
            }
            GUILayout.Space(15);

            drawSizeLabel("マテリアルコピー", 20, true);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                drawSizeLabel("マテリアルを参照するプレハブ", 13, true);
                EditorGUI.BeginChangeCheck();
                _ref_prefab = EditorGUILayout.ObjectField("元のプレハブ", _ref_prefab, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                {
                    if (_ref_prefab == null)
                    {
                        _meshRenderers.Clear();
                        _skinRenderers.Clear();
                        return;
                    }

                    foreach (var mesh in _ref_prefab.GetComponentsInChildren<MeshRenderer>())
                    {
                        string meshpath = GetHierarchyPath(mesh.gameObject);
                        _meshRenderers.Add(meshpath, mesh.sharedMaterials);
                    }

                    foreach (var skin in _ref_prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        string skinpath = GetHierarchyPath(skin.gameObject);
                        _skinRenderers.Add(skinpath, skin.sharedMaterials);
                    }
                }

                GUILayout.Space(10);

                //入れてもらう
                drawArray("_prefabs");

                GUILayout.Space(10);

                drawSizeLabel("保存先", 13, true);
                using (new GUILayout.HorizontalScope())
                {
                    savePath = EditorGUILayout.TextField(savePath);
                    if (GUILayout.Button("参照"))
                    {
                        string panelPath = EditorUtility.SaveFolderPanel("プレハブを保存する場所", savePath, "");
                        if (!string.IsNullOrEmpty(panelPath))
                            savePath = panelPath;
                    }
                }

                if (GUILayout.Button("コピー開始"))
                {
                    if (!EditorUtility.DisplayDialog(EditorTitle, "処理を開始してよろしいですか？", "はい", "いいえ"))
                        return;

                    if (!Directory.Exists(savePath))
                        Directory.CreateDirectory(savePath);

                    char result = savePath[savePath.Length - 1];
                    if (result != '/')
                        savePath += "/";

                    foreach (var prefab in _prefabs)
                    {
                        GameObject _pref = prefab;

                        if (!IsExists(_pref))
                        {
                            _pref = Instantiate(_pref);
                            _pref.name = prefab.name;
                        }

                        SetMaterials(_pref);

                        //ファイル名に使用できない文字を取得
                        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();

                        string filename = _pref.name;
                        if (filename.Length > 50)
                            filename = filename.Substring(0, 50);

                        for (int i = 0; i < invalidChars.Length; i++)
                        {
                            filename = filename.Replace(invalidChars[i].ToString(), "");
                        }

                        // Prefabを作成or上書きして紐づける
                        PrefabUtility.SaveAsPrefabAssetAndConnect(_pref, savePath + filename + ".prefab", InteractionMode.AutomatedAction);

#if UNITY_EDITOR
                        DestroyImmediate(_pref);
#else
                        Destroy(_pref);
#endif

                    }

                    EditorUtility.DisplayDialog(EditorTitle, "処理が完了しました！", "はい");
                }
            }

            GUILayout.Space(15);

            drawSizeLabel("Unity キャプチャー", 20, true);
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                camera = (Camera)EditorGUILayout.ObjectField("カメラ", camera, typeof(Camera), true);
                GUILayout.Space(5);

                // 해상도 설정.
                res = (resPreset)EditorGUILayout.EnumPopup("解像度", res);
                if (res != resPreset.Custom)
                {
                    camOrientaion = (camOrientationPreset)EditorGUILayout.EnumPopup(" ", camOrientaion);

                    // 가로, 세로 설정.
                    if (camOrientaion == camOrientationPreset.Horizon)
                    {
                        captureSize.x = prestSize[(int)res, 0];
                        captureSize.y = prestSize[(int)res, 1];
                    }
                    else
                    {
                        captureSize.x = prestSize[(int)res, 1];
                        captureSize.y = prestSize[(int)res, 0];
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Size");
                    captureSize = EditorGUILayout.Vector2Field("", captureSize, GUILayout.Width(EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth));
                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(5);

                // 배경.
                bgType = (BGType)EditorGUILayout.EnumPopup("背景", bgType);

                // 배경 색.
                if (bgType == BGType.Color)
                    bgColor = EditorGUILayout.ColorField("色", bgColor);

                GUILayout.Space(10);

                drawSizeLabel("保存先", 13, true);
                using (new GUILayout.HorizontalScope())
                {
                    ssSaveDir = EditorGUILayout.TextField(ssSaveDir);
                    if (GUILayout.Button("参照"))
                    {
                        string panelPath = EditorUtility.SaveFolderPanel("プレハブを保存する場所", ssSaveDir, "");
                        if (!string.IsNullOrEmpty(panelPath))
                            ssSaveDir = panelPath;
                    }
                }

                GUILayout.Space(10);

                if (GUILayout.Button("撮影"))
                {
                    string name = doCapture();
                    EditorUtility.DisplayDialog(EditorTitle, $"{name} で保存しました！", "OK");
                }
            }
        }

        /// <summary>
        /// GUI描画用
        /// </summary>
        public void OnGUI()
        {
            ShowGUI();
        }
#endregion

        #region UnityFunc
        private void OnEnable()
        {
            Initialize();

            _ref_prefab = null;
            camera = Camera.main;
        }
        #endregion
    }
}