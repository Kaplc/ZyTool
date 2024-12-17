using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZyTool.Data
{
    [Serializable]
    public class ToolCache
    {
        private string[] keysArr = new string[]
        {
            "outsideFolderPath",
            "unityFolderPath",
            "artFolderPath", 
            "prefabsFolderPath",
            "scriptsFolderPath",
            "emptySprPath", 
            "atlasPath",
            "checkFolderPath",
            "checkFolder2Path",
            "workspaceFilePath"
        };

        private Dictionary<string, string> pathDic = new Dictionary<string, string>();

        public ToolCache()
        {
            RegisterKey();
        }

        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<string> values = new List<string>();

        private void RegisterKey()
        {
            foreach (var key in keysArr)
            {
                if (pathDic.ContainsKey(key) == false)
                {
                    pathDic.Add(key, "");
                }
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            for (int i = 0; i < keys.Count; i++)
            {
                dict[keys[i]] = values[i];
            }

            return dict;
        }

        public void FromDictionary(Dictionary<string, string> dict)
        {
            keys.Clear();
            values.Clear();

            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void Add(string key, string value)
        {
            pathDic[key] = value;
        }

        public string Get(string key)
        {
            if (pathDic.TryGetValue(key, out var value))
            {
                return value;
            }

            Debug.LogError("该key不存在" + key);
            return "";
        }

        public void Save(string path)
        {
            FromDictionary(pathDic);
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
                var cache = JsonUtility.FromJson<ToolCache>(json);
                cache.pathDic = cache.ToDictionary();
                cache.RegisterKey();
                return cache;
            }

            Debug.Log("文件不存在返回默认");
            return new ToolCache();
        }
    }
}