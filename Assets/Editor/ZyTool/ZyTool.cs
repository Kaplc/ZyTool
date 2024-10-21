using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using ZyTool.Data;
using Object = UnityEngine.Object;

namespace ZyTool
{
    public partial class ZyTool : EditorWindow
    {
        public static EditorWindow win;

        private int cacheIndex = -1;

        private Vector2 contentScroll = Vector2.zero;
        private Object artFolder;
        private Object prefabsFolder;
        private ToolCache toolCache;
        private TextAsset toolCacheFile;
        private FileTool fileTool;
        private PrefabsTool prefabsTool;
        private UITool uiTool;
        private List<ToolCache> toolCaches = new List<ToolCache>();
        private List<string> cacheNames = new List<string>();

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

        [MenuItem("ZyTool/Open %T")]
        public static void OpenWindow()
        {
            if (win == null)
            {
                win = GetWindow<ZyTool>("SuperTool");
            }

            win.Focus();
        }

        private void OnEnable()
        {
            if (fileTool == null) fileTool = new FileTool(this);
            if (prefabsTool == null) prefabsTool = new PrefabsTool(this);
            if (uiTool == null) uiTool = new UITool(this);

            LoadCaches();

            if (toolCaches.Count == 0)
            {
                var c = new ToolCache();
                c.Save(Application.dataPath + "/Editor/ZyTool/Data/DefaultToolCache.json");

                LoadCaches();
            }

            if (cacheIndex == -1)
            {
                cacheIndex = 0;
            }

            LoadCache(cacheIndex);

            // 注册预制体编辑模式的进入与退出事件
            PrefabStage.prefabSaving -= OnPrefabSaving;
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        private void OnPrefabSaving(GameObject obj)
        {
            // 执行检查逻辑
            if (!CheckBeforeSave())
            {
                // 打开一个对话框告知用户保存被取消
                EditorUtility.DisplayDialog("保存失败", "请检查分辨率是否正确再保存", "确定");

                // 阻止后续保存流程
                throw new OperationCanceledException("保存已被用户取消");
            }
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

        public bool ShowDialog(string t, string message, string yes, string no)
        {
            // 显示提示框
            bool result = EditorUtility.DisplayDialog(t, message, yes, no);

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
                EditorGUILayout.LabelField("所有缓存文件:", GUILayout.Width(90));
                int newIndex = EditorGUILayout.Popup(cacheIndex, cacheNames?.ToArray());
                if (newIndex != cacheIndex)
                {
                    SaveCache();
                    // 重新加载json
                    cacheIndex = newIndex;
                    LoadCache(cacheIndex);
                    toolCacheFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Editor/ZyTool/Data/" + cacheNames[cacheIndex] + ".json");
                }

                EditorGUILayout.LabelField("缓存文件:", GUILayout.Width(60));
                EditorGUILayout.ObjectField(toolCacheFile, typeof(TextAsset), false);

                if (GUILayout.Button("保存"))
                {
                    SaveCache();
                }

                if (GUILayout.Button("创建"))
                {
                    NewCache();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("常用功能");

            EditorGUILayout.BeginHorizontal();
            {
                ArtFolder = EditorGUILayout.ObjectField("素材文件夹目录:", ArtFolder, typeof(Object), true);

                if (GUILayout.Button("打开文件夹"))
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

                if (GUILayout.Button("打开文件夹"))
                {
                    if (PrefabsFolder)
                    {
                        OpenInFileBrowser(AssetDatabase.GetAssetPath(PrefabsFolder));
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!win) win = GetWindow<ZyTool>();
            EditorGUILayout.LabelField(new string('-', (int)(win.position.width)));
        }


        private void LogGUI()
        {
            #region Log

            EditorGUILayout.Space();
            if (!win) win = GetWindow<ZyTool>();

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
        private static void OpenInFileBrowser(string path)
        {
            Process.Start("explorer.exe", path.Replace("/", "\\"));
        }

        #region 缓存

        private void LoadCaches()
        {
            string path = Application.dataPath + "/Editor/ZyTool/Data/";
            // 获取文件夹下所有json
            var files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);

            toolCaches.Clear();
            cacheNames.Clear();
            foreach (var file in files)
            {
                var c = ToolCache.Load(file);
                toolCaches.Add(c);
                cacheNames.Add(Path.GetFileName(file).Replace(".json", ""));
            }
        }

        private void LoadCache(int index)
        {
            string path = Application.dataPath + "/Editor/ZyTool/Data/" + cacheNames[index] + ".json";
            toolCache = ToolCache.Load(path);

            artFolder = AssetDatabase.LoadAssetAtPath<Object>(toolCache.ArtFolderPath);
            prefabsFolder = AssetDatabase.LoadAssetAtPath<Object>(toolCache.PrefabsFolderPath);

            fileTool.outsideFolder = toolCache.OutsideFolderPath;
            fileTool.unityFolder = AssetDatabase.LoadAssetAtPath<Object>(toolCache.UnityFolderPath);

            uiTool.emptySpr = AssetDatabase.LoadAssetAtPath<Sprite>(toolCache.EmptySprPath);

            selectAtlasObj = AssetDatabase.LoadAssetAtPath<Object>(toolCache.AtlasPath);

            toolCacheFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Editor/ZyTool/Data/" + cacheNames[index] + ".json");
        }

        private void SaveCache()
        {
            toolCache.ArtFolderPath = AssetDatabase.GetAssetPath(artFolder);
            toolCache.PrefabsFolderPath = AssetDatabase.GetAssetPath(prefabsFolder);

            toolCache.OutsideFolderPath = fileTool.outsideFolder;
            toolCache.UnityFolderPath = AssetDatabase.GetAssetPath(fileTool.unityFolder);

            toolCache.EmptySprPath = AssetDatabase.GetAssetPath(uiTool.emptySpr);

            toolCache.AtlasPath = AssetDatabase.GetAssetPath(selectAtlasObj);

            string path = Application.dataPath + "/Editor/ZyTool/Data/" + cacheNames[cacheIndex] + ".json";
            toolCache.Save(path);
        }

        private void NewCache()
        {
            SaveCache();
            string path = EditorUtility.SaveFilePanel("新建缓存", Application.dataPath + "/Editor/ZyTool/Data/", "NewToolCache", "json");
            if (path != "")
            {
                toolCache = new ToolCache();
                toolCache.Save(path);

                toolCaches.Add(toolCache);
                cacheNames.Add(Path.GetFileName(path).Replace(".json", ""));
                cacheIndex = toolCaches.Count - 1;

                LoadCache(cacheIndex);
            }
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

        #region 保存前检查

        // 检查逻辑：你可以根据需要自定义这个逻辑
        private bool CheckBeforeSave()
        {
            if (GetMainGameViewSize() == new Vector2(1136, 640))
            {
                return true;
            }

            return false;
        }

        // 使用反射获取Game窗口的分辨率
        private Vector2 GetMainGameViewSize()
        {
            // 获取 GameView 的类型
            Type gameViewType = Type.GetType("UnityEditor.GameView, UnityEditor");
            // 找到 GameView 实例
            var gameView = GetWindow(gameViewType);
            var fd = gameView.GetType().GetProperty("targetSize", BindingFlags.NonPublic | BindingFlags.Instance);
            var size = fd.GetValue(gameView); // 调用 targetSize()

            // 返回窗口尺寸
            return (Vector2)size;
        }

        #endregion

        public bool KeyCodeQConfirm()
        {
            Event e = Event.current;

            if (e.control && e.type == EventType.KeyDown && e.keyCode == KeyCode.Q)
            {
                e.Use(); // 标记事件为已使用，防止其他组件继续处理
                return true;
            }

            return false;
        }

        public string GetRelativePath(string path)
        {
            return path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
        }
    }
}