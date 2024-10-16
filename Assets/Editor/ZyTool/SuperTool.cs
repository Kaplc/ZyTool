using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using ZyTool.Data;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ZyTool
{
    public partial class SuperTool : EditorWindow
    {
        public static EditorWindow win;

        private Vector2 contentScroll = Vector2.zero;
        private Object artFolder;
        private Object prefabsFolder;
        private ToolCache toolCache;
        private TextAsset toolCacheFile;
        private FileTool fileTool;
        private PrefabsTool prefabsTool;
        private UITool uiTool;

        private Object PrefabsFolder
        {
            get => prefabsFolder;
            set
            {
                // 判断是否是文件夹
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(value)))
                {
                    prefabsFolder = value;
                }
            }
        }

        private Object ArtFolder
        {
            get => artFolder;
            set
            {
                // 判断是否是文件夹
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(value)))
                {
                    artFolder = value;
                }
            }
        }

        [MenuItem("ZyTool/SuperTool %T")]
        public static void OpenWindow()
        {
            if (win == null)
            {
                win = GetWindow<SuperTool>("SuperTool");
            }

            win.Focus();
        }

        [MenuItem("Assets/ZyTool/SaveCheck %S")]
        private static void SaveCheck()
        {
            Debug.Log(1);
        }

        private void OnEnable()
        {
            if (fileTool == null) fileTool = new FileTool(this);
            if (prefabsTool == null) prefabsTool = new PrefabsTool(this);
            if (uiTool == null) uiTool = new UITool(this);

            if (toolCacheFile == null)
            {
                toolCacheFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Editor/ZyTool/Data/ToolCache.json");
                if (toolCacheFile == null)
                {
                    WriteLogWarning("没有找到缓存文件, 已创建");

                    toolCache = new ToolCache();
                    toolCache.Save();

                    toolCacheFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Editor/ZyTool/Data/ToolCache.json");
                }
            }

            LoadCache();
        }

        private void OnGUI()
        {
            #region 菜单按钮集合

            GenericToolGUI();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("功能菜单");
            EditorGUILayout.BeginHorizontal();

            {
                if (GUILayout.Button("UI", GUILayout.Height(50)))
                {
                    uiTool.Open = true;
                }

                if (GUILayout.Button("文件命名", GUILayout.Height(50)))
                {
                    OpenRenameTool = true;
                }

                if (GUILayout.Button("资源处理", GUILayout.Height(50)))
                {
                    OpenHandleTool = true;
                }

                if (GUILayout.Button("文件操作", GUILayout.Height(50)))
                {
                    fileTool.Open = true;
                }
            }

            EditorGUILayout.EndHorizontal();

            #endregion

            EditorGUILayout.Space();

            contentScroll = EditorGUILayout.BeginScrollView(contentScroll);
            {
                if (uiTool.Open)
                {
                    uiTool.OnGUI();
                }

                if (openRenameTool)
                {
                    RenameToolGUI();
                }

                if (openHandleTool)
                {
                    HandleToolGUI();
                }

                if (fileTool.Open)
                {
                    fileTool.OnGUI();
                }
            }
            EditorGUILayout.EndScrollView();

            LogGUI();

            Repaint();
        }

        public void CloseAllTool()
        {
            uiTool.Open = false;
            OpenRenameTool = false;
            OpenHandleTool = false;
            fileTool.Open = false;
        }

        public bool IsSelectedMultiple()
        {
            return Selection.objects.Length > 1;
        }

        public bool IsSelectedSingle()
        {
            return Selection.objects.Length == 1;
        }

        public bool OnlyFile(Object[] files)
        {
            foreach (var file in files)
            {
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(file)))
                {
                    return false;
                }
            }

            return true;
        }

        public bool OnlyTypeFile<T>(Object[] objects)
        {
            foreach (var obj in objects)
            {
                if (!(obj is T))
                {
                    return false;
                }
            }

            return true;
        }

        public bool NoneTypeFile<T>(Object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj is T)
                {
                    return false;
                }
            }

            return true;
        }

        public bool OnlyFolder(Object[] files)
        {
            foreach (var file in files)
            {
                if (!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(file)))
                {
                    return false;
                }
            }

            return true;
        }

        public bool OnlyFileOrFolder(Object[] files)
        {
            if (OnlyFile(files) || OnlyFolder(files))
            {
                return true;
            }

            WriteLogError("不是纯文件或文件夹");
            return false;
        }

        public bool IsParentChildFolder(Object[] folders)
        {
            if (OnlyFolder(folders))
            {
                // 获取所有文件夹的路径
                List<string> paths = new List<string>();
                foreach (var folder in folders)
                {
                    string path = AssetDatabase.GetAssetPath(folder);
                    if (!string.IsNullOrEmpty(path))
                    {
                        paths.Add(path);
                    }
                }

                // 检查是否存在父子关系
                for (int i = 0; i < paths.Count; i++)
                {
                    for (int j = 0; j < paths.Count; j++)
                    {
                        if (i != j && paths[i].StartsWith(paths[j] + "/"))
                        {
                            // 发现一个文件夹是另一个文件夹的子文件夹
                            return true; // 存在父子关系，返回 false
                        }
                    }
                }

                return false; // 所有文件夹之间没有父子关系
            }

            return false;
        }

        public bool IsParentChildFolder(string[] files)
        {
            // 检查是否存在父子关系
            for (int i = 0; i < files.Length; i++)
            {
                for (int j = 0; j < files.Length; j++)
                {
                    if (i != j && files[i].StartsWith(files[j] + "/"))
                    {
                        // 发现一个文件是另一个文件的子文件
                        return true; // 存在父子关系，返回 false
                    }
                }
            }

            return false; // 所有文件之间没有父子关系
        }

        public string[] GetFileNames(Object[] files)
        {
            if (OnlyFile(files))
            {
                List<string> fileNames = new List<string>();
                foreach (var file in files)
                {
                    fileNames.Add(AssetDatabase.GetAssetPath(file));
                }

                return fileNames.ToArray();
            }

            WriteLogError("只能选择文件!");
            return null;
        }

        public string[] GetFolderNames(Object[] files)
        {
            if (OnlyFolder(files))
            {
                List<string> folderNames = new List<string>();
                foreach (var file in files)
                {
                    folderNames.Add(AssetDatabase.GetAssetPath(file));
                }

                return folderNames.ToArray();
            }

            WriteLogError("只能选择文件夹!");
            return null;
        }

        /// <summary>
        /// 获取多个选中对象
        /// </summary>
        public Object[] GetMultiSelection()
        {
            return Selection.objects;
        }

        /// <summary>
        /// 获取单个选中对象
        /// </summary>
        public Object GetSingleSelection()
        {
            if (Selection.objects.Length > 1)
            {
                return null;
            }

            return Selection.activeObject;
        }


        public bool ShowDialog(string t, string message)
        {
            // 显示提示框
            bool result = EditorUtility.DisplayDialog(t, message, "确定", "取消");

            if (result)
            {
                return true;
            }

            return false;
        }

        #region GUI

        private void GenericToolGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.ObjectField("缓存文件: ", toolCacheFile, typeof(TextAsset), false);

                if (GUILayout.Button("保存"))
                {
                    SaveCache();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("常用功能");

            EditorGUILayout.BeginHorizontal();
            {
                ArtFolder = EditorGUILayout.ObjectField("素材文件夹目录:", ArtFolder, typeof(Object), true);

                if (GUILayout.Button("文件浏览器"))
                {
                    if (ArtFolder)
                    {
                        OpenInFileBrowser(AssetDatabase.GetAssetPath(ArtFolder));
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                PrefabsFolder = EditorGUILayout.ObjectField("预制体文件夹目录:", PrefabsFolder, typeof(Object), true);

                if (GUILayout.Button("文件浏览器"))
                {
                    if (PrefabsFolder)
                    {
                        OpenInFileBrowser(AssetDatabase.GetAssetPath(PrefabsFolder));
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!win) win = GetWindow<SuperTool>();
            EditorGUILayout.LabelField(new string('-', (int)(win.position.width)));
        }


        private void LogGUI()
        {
            #region Log

            EditorGUILayout.Space();
            if (!win) win = GetWindow<SuperTool>();

            EditorGUILayout.LabelField(new string('-', (int)(win.position.width)));

            // log
            if (logStyle == null)
            {
                logStyle = new GUIStyle();
            }

            if (latestLogMsg != null)
            {
                latestLogScroll = EditorGUILayout.BeginScrollView(latestLogScroll, GUILayout.Height(30));
                {
                    EditorGUILayout.LabelField("Log: " + latestLogMsg.info, (GUIStyle)latestLogMsg.style,
                        GUILayout.Width(latestLogMsg.info.Length * 10));
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("Log: ");
            }


            logFold = EditorGUILayout.Foldout(logFold, "Log详细信息");
            if (logFold)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("滑动到底部"))
                    {
                        MoveToBottom();
                    }

                    if (GUILayout.Button("清空"))
                    {
                        historyLogs.Clear();
                    }
                }
                EditorGUILayout.EndHorizontal();

                logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.Height(100));
                {
                    for (int i = 0; i < historyLogs.Count; i++)
                    {
                        EditorGUILayout.LabelField(historyLogs[i].info, historyLogs[i].style, GUILayout.Width(historyLogs[i].info.Length * 10));
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            #endregion
        }

        #endregion

        public void OnSelectionChange()
        {
            if (uiTool.Open)
            {
                uiTool.OnSelectionChange();
            }

            if (OpenRenameTool)
            {
                Object[] objects = GetMultiSelection();
                selectedFiles.Clear();
                foreach (var o in objects)
                {
                    if (o as GameObject != null)
                    {
                        WriteLogError("命名工具不支持GameObject");
                        return;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(o);
                    if (!AssetDatabase.IsValidFolder(assetPath))
                    {
                        selectedFiles.Add(o);
                    }
                    else
                    {
                        WriteLogError("命名工具不支持文件夹");
                        selectedFiles.Clear();
                        break;
                    }
                }

                if (selectedFiles.Count == 1)
                {
                    WriteLogInfo("选中文件: " + selectedFiles[0].name);
                }
                else if (selectedFiles.Count > 1)
                {
                    WriteLogInfo($"选中{selectedFiles.Count}个文件");
                }
            }
            else
            {
                selectedFiles.Clear();
            }

            if (OpenHandleTool)
            {
                Object[] objects = GetMultiSelection();
                selectedHandleToolObjs.Clear();
                if (OnlyFile(objects) == false && OnlyFolder(objects) == false)
                {
                    WriteLogError("文件和文件夹同时不能选择!");
                    selectedHandleToolObjs.Clear();
                    return;
                }

                foreach (var o in objects)
                {
                    selectedHandleToolObjs.Add(o);
                }
            }
            else
            {
                selectedHandleToolObjs.Clear();
            }

            if (fileTool.Open)
            {
                fileTool.OnSelectionChange();
            }
        }

        /// <summary>
        /// 调用系统的文件浏览器函数
        /// </summary>
        public static void OpenInFileBrowser(string path)
        {
            // 如果是Windows系统
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Process.Start("explorer.exe", path.Replace("/", "\\"));
            }
            // 如果是macOS系统
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                Process.Start("open", path);
            }
            // 如果是Linux系统
            else if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                Process.Start("xdg-open", path);
            }
        }

        #region 缓存

        private void LoadCache()
        {
            toolCache = ToolCache.Load();
            
            artFolder = AssetDatabase.LoadAssetAtPath<Object>(toolCache.ArtFolderPath);
            prefabsFolder = AssetDatabase.LoadAssetAtPath<Object>(toolCache.PrefabsFolderPath);

            fileTool.outsideFolder = toolCache.OutsideFolderPath;
            fileTool.unityFolder = AssetDatabase.LoadAssetAtPath<Object>(toolCache.UnityFolderPath);

            uiTool.emptySpr = AssetDatabase.LoadAssetAtPath<Sprite>(toolCache.EmptySprPath);
        }

        private void SaveCache()
        {
            toolCache.ArtFolderPath = AssetDatabase.GetAssetPath(artFolder);
            toolCache.PrefabsFolderPath = AssetDatabase.GetAssetPath(prefabsFolder);

            toolCache.OutsideFolderPath = fileTool.outsideFolder;
            toolCache.UnityFolderPath = AssetDatabase.GetAssetPath(fileTool.unityFolder);

            toolCache.EmptySprPath = AssetDatabase.GetAssetPath(uiTool.emptySpr);

            toolCache.Save();
        }

        #endregion

        private void OnDisable()
        {
            SaveCache();
        }

        private void OnDestroy()
        {
            SaveCache();
        }
    }
}