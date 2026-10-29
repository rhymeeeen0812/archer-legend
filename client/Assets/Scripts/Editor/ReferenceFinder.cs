using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace SampleEditor
{
    public class ReferenceFinder : EditorWindow
    {
        private List<GameObject> _referencedObjects = new List<GameObject>();
        private string componentName = string.Empty;
        private GUIStyle _labelStyle;
        private Vector2 _scrollPosition;
        private string[] componentNames = new string[]
        {
            "UnityEngine.UI.Image, UnityEngine.UI",
            "UnityEngine.UI.RawImage, UnityEngine.UI",
            "UnityEngine.RectTransform, UnityEngine.CoreModule"
        };
        private int selectedComponentIndex = 0;

        [MenuItem("Tools/Find References")]
        private static void ShowWindow()
        {
            GetWindow<ReferenceFinder>("Find References");
        }

        private void OnGUI()
        {
            SetLabelStyle();

            DrawLine();

            GUILayout.Label("Prefab検索", _labelStyle);
            selectedComponentIndex = EditorGUILayout.Popup(selectedComponentIndex, componentNames);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Prefab"))
            {
                FindPrefabReferences(componentNames[selectedComponentIndex]);
            }

            if (GUILayout.Button("Clear"))
            {
                componentName = string.Empty;
                _referencedObjects.Clear();
            }
            GUILayout.EndHorizontal();

            DrawLine();

            if (_referencedObjects.Count > 0)
            {
                GUILayout.Label("Referenced Prefabs", _labelStyle);
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                foreach (GameObject obj in _referencedObjects)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(obj.name);
                    if (GUILayout.Button("Select"))
                    {
                        Selection.activeObject = obj;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
        }

        private void FindPrefabReferences(string componentName)
        {
            _referencedObjects.Clear();

            // 検索するパスを指定
            string[] searchPaths = new string[] { "Assets/Prefabs", "Assets/Scenes" };

            foreach (string path in searchPaths)
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab == null) continue;

                    // Assembly クラスを使って型を取得
                    Type componentType = null;
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        componentType = assembly.GetType(componentName);
                        if (componentType != null)
                        {
                            break;
                        }
                    }

                    if (componentType == null)
                    {
                        Debug.LogError("Component type not found: " + componentName);
                        continue;
                    }

                    Debug.Log("Checking prefab: " + assetPath + " for component: " + componentType);
                    if (ContainsComponent(prefab.transform, componentType))
                    {
                        _referencedObjects.Add(prefab);
                        Debug.Log("Found prefab: " + prefab.name);
                    }
                }
            }

            Debug.Log("Total prefabs found: " + _referencedObjects.Count);
        }

        private void SetLabelStyle()
        {
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 20;
            _labelStyle.fontStyle = FontStyle.Bold;
            _labelStyle.alignment = TextAnchor.MiddleCenter;
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 1));
        }

        private void DrawLine()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }


        /// <summary>
        /// 指定されたコンポーネントが指定された親オブジェクト以下に存在するかどうかを返す
        /// </summary>
        private bool ContainsComponent(Transform parent, Type componentType)
        {
            bool componentFound = parent.GetComponent(componentType) != null;

            foreach (Transform child in parent)
            {
                componentFound |= ContainsComponent(child, componentType);
            }

            return componentFound;
        }
    }
}
