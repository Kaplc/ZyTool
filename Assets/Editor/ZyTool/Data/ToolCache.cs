using System;
using UnityEditor;
using UnityEngine;

namespace ZyTool.Data
{
    [Serializable]
    public class ToolCache
    {
        [SerializeField] private string outsideFolderPath;
        [SerializeField] private string unityFolderPath;
        [SerializeField] private string artFolderPath;
        [SerializeField] private string prefabsFolderPath;
        [SerializeField] private string emptySprPath;
        [SerializeField] private string atlasPath;

        public string OutsideFolderPath
        {
            get => outsideFolderPath;
            set => outsideFolderPath = value;
        }

        public string ArtFolderPath
        {
            get => artFolderPath;
            set => artFolderPath = value;
        }

        public string PrefabsFolderPath
        {
            get => prefabsFolderPath;
            set => prefabsFolderPath = value;
        }

        public string UnityFolderPath
        {
            get => unityFolderPath;
            set => unityFolderPath = value;
        }

        public string EmptySprPath
        {
            get => emptySprPath;
            set => emptySprPath = value;
        }
        
        public string AtlasPath
        {
            get => atlasPath;
            set => atlasPath = value;
        }

        public void Save(string path)
        {
            // json保存
            string json = JsonUtility.ToJson(this);
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }
        
        public static ToolCache Load(string path)
        {
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                return JsonUtility.FromJson<ToolCache>(json);
            }
            else
            {
                return new ToolCache();
            }
        }
    }
}