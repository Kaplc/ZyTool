using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ZyTool
{
    public class CheckTool
    {
        private ZyTool rootTool;

        public Object checkFolder;
        public Object checkFolder2;
        private bool open;

        public bool Open
        {
            get => open;
            set
            {
                if (value)
                {
                    rootTool.CloseAllTool();
                }

                open = value;
            }
        }

        public CheckTool(ZyTool rootTool)
        {
            this.rootTool = rootTool;
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("检查目标文件夹是否有资源被使用", GUILayout.Width(200));
                checkFolder = EditorGUILayout.ObjectField(checkFolder, typeof(Object), false, GUILayout.Width(150));
                if (GUILayout.Button("检查", GUILayout.Width(50)))
                {
                    if (checkFolder)
                    {
                        if (CheckUsedResForFolder() == false)
                        {
                            EditorUtility.DisplayDialog("检查结果", "含有目标文件夹资源被使用", "确定");
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("检查目标文件夹是否有资源没有被使用", GUILayout.Width("检查目标文件夹是否有资源没有被使用".Length * 15));
                checkFolder2 = EditorGUILayout.ObjectField(checkFolder2, typeof(Object), false, GUILayout.Width(150));
                if (GUILayout.Button("检查", GUILayout.Width(50)))
                {
                    if (checkFolder2)
                    {
                        if (CheckUnusedResForFolder() == false)
                        {
                            EditorUtility.DisplayDialog("检查结果", "有文件未被使用", "确定");
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool CheckUsedResForFolder()
        {
            // 获取当前预制体根
            var root = rootTool.GetCurrentPrefabRoot();

            Image[] images = root.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image.sprite != null && image.sprite.name.Contains("示意图") == false)
                {
                    string path = AssetDatabase.GetAssetPath(image.sprite);
                    string cekPath = AssetDatabase.GetAssetPath(checkFolder);

                    if (path.Contains(cekPath))
                    {
                        EditorGUIUtility.PingObject(image);
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CheckUnusedResForFolder()
        {
            // 获取当前预制体根
            var root = rootTool.GetCurrentPrefabRoot();
            // 所有sprite的路径
            List<string> spritePaths = new List<string>();
            Image[] images = root.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image.sprite != null && image.sprite.name.Contains("示意图") == false)
                {
                    spritePaths.Add(AssetDatabase.GetAssetPath(image.sprite));
                }
            }
            
            // 所有图片的路径
            List<string> spritePath = new List<string>(rootTool.GetAllFileNamesForFolder(checkFolder2));

            foreach (var path in spritePath)
            {
                if (spritePaths.Contains(path) == false)
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
                    return false;
                }
            }

            return true;
        }

        // --------------------------------------------------------------------------------
    }
}