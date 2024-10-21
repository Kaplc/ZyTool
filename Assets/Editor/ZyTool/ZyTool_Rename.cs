using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZyTool
{
    public partial class ZyTool
    {
        private bool openRenameTool;

        private bool OpenRenameTool
        {
            get => openRenameTool;
            set
            {
                if (value)
                {
                    CloseAllTool();
                }

                openRenameTool = value;
                OnSelectionChange();
            }
        }

        private string newName = "";
        private string prefix = ""; // 默认前缀
        private string suffix = "";
        private string fromReplaceContent = "";
        private string toReplaceContent = "";
        private int selectedOldNameIndex = 0;
        private List<Object> selectedFiles = new List<Object>();

        // 文件对象->旧名字集合
        private Dictionary<Object, List<string>> oldNameDic = new Dictionary<Object, List<string>>();

        private void RenameToolGUI()
        {
            EditorGUILayout.LabelField("选中的文件:");
            foreach (var f in selectedFiles)
            {
                EditorGUILayout.ObjectField(f, typeof(Object), false);
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("前缀:", GUILayout.Width(50));
                prefix = EditorGUILayout.TextField(prefix);
                if (GUILayout.Button("加前缀"))
                {
                    AddPrefix(GetMultiSelection());
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("后缀:", GUILayout.Width(50));
                suffix = EditorGUILayout.TextField(suffix);
                if (GUILayout.Button("加后缀"))
                {
                    AddSuffix(GetMultiSelection());
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("替换内容:", GUILayout.Width(60));
            EditorGUILayout.BeginHorizontal();
            {
                fromReplaceContent = EditorGUILayout.TextField(fromReplaceContent);
                EditorGUILayout.LabelField("替换为", GUILayout.Width(40));
                toReplaceContent = EditorGUILayout.TextField(toReplaceContent);
                if (GUILayout.Button("替换"))
                {
                    Object[] objects = GetMultiSelection();
                    if (objects == null)
                    {
                        ReplaceName(GetSingleSelection(), fromReplaceContent, toReplaceContent);
                    }
                    else
                    {
                        foreach (var o in objects)
                        {
                            ReplaceName(o, fromReplaceContent, toReplaceContent);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("重命名:", GUILayout.Width(50));
                newName = EditorGUILayout.TextField(newName);
                if (GUILayout.Button("重命名"))
                {
                    if (newName == "")
                    {
                        WriteLogError("请输入重命名内容");
                        return;
                    }

                    Rename(GetSingleSelection(), newName, "重命名");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            string[] opt = null;
            if (GetSingleSelection() != null)
            {
                opt = GetHistoryName();

                if (opt != null)
                {
                    selectedOldNameIndex = EditorGUILayout.Popup("修改历史记录:", selectedOldNameIndex, opt);
                }
                else
                {
                    EditorGUILayout.Popup("修改历史记录:", 0, new[] { "无" });
                }
            }
            else
            {
                EditorGUILayout.Popup("修改历史记录:", 0, new[] { "无" });
            }


            if (GUILayout.Button("恢复为选中的历史名称"))
            {
                if (opt != null)
                {
                    Rename(GetSingleSelection(), opt[selectedOldNameIndex], "恢复历史名称");
                }
            }
        }

        private void AddPrefix(Object[] objects)
        {
            if (objects.Length == 0)
            {
                WriteLogWarning("请至少选择一个对象!");
                return;
            }

            foreach (var obj in objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                string oldName = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
                {
                    string newPath = Path.GetDirectoryName(path) + "/" + prefix +
                                     Path.GetFileName(path);

                    CacheName(obj, Path.GetFileName(path));

                    WriteLogInfo($"添加前缀成功{oldName} -> {Path.GetFileName(newPath)}");
                    AssetDatabase.MoveAsset(path, newPath);
                }
                else
                {
                    WriteLogError("只能操作文件!");
                }
            }

            AssetDatabase.Refresh();
        }

        private void AddSuffix(Object[] objects)
        {
            if (objects.Length == 0)
            {
                WriteLogWarning("请至少选择一个对象!");
                return;
            }

            foreach (var obj in objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                string oldName = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
                {
                    string newPath = Path.GetDirectoryName(path) + "/" +
                                     Path.GetFileNameWithoutExtension(path) + suffix +
                                     Path.GetExtension(path);
                    CacheName(obj, Path.GetFileName(path));
                    AssetDatabase.MoveAsset(path, newPath);

                    WriteLogInfo($"添加后缀成功{oldName} -> {Path.GetFileName(newPath)}");
                }
                else
                {
                    WriteLogError("只能操作文件!");
                }
            }

            AssetDatabase.Refresh();
        }

        private void CacheName(Object o, string oldName)
        {
            if (oldNameDic.ContainsKey(o))
            {
                oldNameDic[o].Add(oldName);
            }
            else
            {
                oldNameDic.Add(o, new List<string> { oldName });
            }
        }

        private void Rename(Object obj, string nName, string logTitle)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
            {
                string oName = Path.GetFileName(path);
                CacheName(obj, oName);
                AssetDatabase.RenameAsset(path, nName);
                WriteLogInfo($"{logTitle}成功：{oName} -> {nName}");
            }
        }

        private void ReplaceName(Object obj, string from, string to)
        {
            if (from == "")
            {
                WriteLogError("请输入要替换的内容!");
                return;
            }

            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && !AssetDatabase.IsValidFolder(path))
            {
                string fileName = Path.GetFileName(path);
                if (!fileName.Contains(from))
                {
                    WriteLogError($"文件名中不含该内容\"{from}\",无法替换!");
                    return;
                }

                if (to == "")
                {
                    if (ShowDialog("替换提示", $"目标字符串{from}会替换为空字符串！是否继续?"))
                    {
                        Rename(obj, fileName.Replace(from, to), "替换成功");
                    }
                    else
                    {
                        WriteLogInfo("取消替换");
                    }

                    return;
                }

                Rename(obj, fileName.Replace(from, to), "替换成功");
            }
        }

        private string[] GetHistoryName()
        {
            var obj = GetSingleSelection();
            if (obj == null)
            {
                return null;
            }

            if (oldNameDic.TryGetValue(obj, out var value))
            {
                return value.ToArray();
            }

            return null;
        }
    }
}