using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZyTool.Data;
using Object = UnityEngine.Object;

namespace ZyTool
{
    public partial class ZyTool : EditorWindow
    {
        public static EditorWindow win;
        private const string Version = "3.0.0";

        private int cacheIndex = -1;

        private Vector2 contentScroll = Vector2.zero;

        private ToolCache cache;

        // generic tool
        private Object artFolder;
        private Object prefabsFolder;
        private Object scriptFolder;
        private Object workspaceFile;
        private TextAsset toolCacheFile;
        private List<ToolCache> toolCaches = new List<ToolCache>();

        private List<string> cacheNames = new List<string>();

        // sub tool
        internal FileTool fileTool;
        internal PrefabsTool prefabsTool;
        internal ReNameTool renameTool;
        internal UITool uiTool;
        internal HandleTool handleTool;
        internal CheckTool checkTool;
        internal AssetTool assetTool;


        private Object PrefabsFolder
        {
            get => prefabsFolder;
            set
            {
                // 判断是否是文件夹或预制体
                if (value != null && (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(value)) || PrefabUtility.IsPartOfPrefabAsset(value)))
                {
                    prefabsFolder = value;
                }
                else
                {
                    prefabsFolder = null;
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

        // check
        private bool resolutionCheck = true;


        [MenuItem("ZyTool/Open %T")]
        public static void OpenWindow()
        {
            if (win == null)
            {
                win = GetWindow<ZyTool>("ZyTool");
                // GetWindow<SaveCheckTool>("SaveCheckTool");
            }

            win.Focus();
        }

        #region Unity回调

        private void OnEnable()
        {
            if (fileTool == null) fileTool = new FileTool(this);
            if (prefabsTool == null) prefabsTool = new PrefabsTool(this);
            if (uiTool == null) uiTool = new UITool(this);
            if (renameTool == null) renameTool = new ReNameTool(this);
            if (handleTool == null) handleTool = new HandleTool(this);
            if (checkTool == null) checkTool = new CheckTool(this);
            if (assetTool == null) assetTool = new AssetTool(this);

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

        private void OnDisable()
        {
            SaveCache();
        }

        private void OnDestroy()
        {
            SaveCache();
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
                    renameTool.OpenRenameTool = true;
                }

                if (GUILayout.Button("资源处理", GUILayout.Height(50)))
                {
                    handleTool.OpenHandleTool = true;
                }

                if (GUILayout.Button("文件操作", GUILayout.Height(50)))
                {
                    fileTool.Open = true;
                }

                if (GUILayout.Button("检查", GUILayout.Height(50)))
                {
                    checkTool.Open = true;
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

                if (renameTool.OpenRenameTool)
                {
                    renameTool.RenameToolGUI();
                }

                if (handleTool.OpenHandleTool)
                {
                    handleTool.HandleToolGUI();
                }

                if (fileTool.Open)
                {
                    fileTool.OnGUI();
                }

                if (checkTool.Open)
                {
                    checkTool.OnGUI();
                }
            }
            EditorGUILayout.EndScrollView();

            LogGUI();

            Repaint();
        }

        #endregion

        private void OnPrefabSaving(GameObject obj)
        {
            // 执行检查逻辑
            if (!CheckBeforeSave())
            {
                win.Focus();
                // 阻止后续保存流程
                throw new OperationCanceledException("保存已被用户取消");
            }
        }

        // ------------------------------------------------------
        // 工具方法

        public void CloseAllTool()
        {
            uiTool.Open = false;
            renameTool.OpenRenameTool = false;
            handleTool.OpenHandleTool = false;
            fileTool.Open = false;
            checkTool.Open = false;
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

            PrintLogError("不是纯文件或文件夹");
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

            PrintLogError("只能选择文件!");
            return null;
        }

        public string[] GetFolderNames(Object[] folders)
        {
            if (OnlyFolder(folders))
            {
                List<string> folderNames = new List<string>();
                foreach (var f in folders)
                {
                    folderNames.Add(AssetDatabase.GetAssetPath(f));
                }

                return folderNames.ToArray();
            }

            PrintLogError("只能选择文件夹!");
            return null;
        }

        public string[] GetAllFileNamesForFolder(Object folder)
        {
            if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder)))
            {
                List<string> fileNames = new List<string>();
                string[] guids = AssetDatabase.FindAssets("", new[] { AssetDatabase.GetAssetPath(folder) });
                foreach (string guid in guids)
                {
                    // 剔除文件夹
                    if (AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        continue;
                    }

                    string fPath = AssetDatabase.GUIDToAssetPath(guid);
                    fileNames.Add(fPath);
                }

                return fileNames.ToArray();
            }

            PrintLogError("只能选择文件夹!");
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

        public void OnSelectionChange()
        {
            if (uiTool.Open)
            {
                uiTool.OnSelectionChange();
            }

            if (renameTool.OpenRenameTool)
            {
                Object[] objects = GetMultiSelection();
                renameTool.selectedFiles.Clear();
                foreach (var o in objects)
                {
                    if (o as GameObject != null)
                    {
                        PrintLogError("命名工具不支持GameObject");
                        return;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(o);
                    if (!AssetDatabase.IsValidFolder(assetPath))
                    {
                        renameTool.selectedFiles.Add(o);
                    }
                    else
                    {
                        PrintLogError("命名工具不支持文件夹");
                        renameTool.selectedFiles.Clear();
                        break;
                    }
                }

                if (renameTool.selectedFiles.Count == 1)
                {
                    PrintLogInfo("选中文件: " + renameTool.selectedFiles[0].name);
                }
                else if (renameTool.selectedFiles.Count > 1)
                {
                    PrintLogInfo($"选中{renameTool.selectedFiles.Count}个文件");
                }
            }
            else
            {
                renameTool.selectedFiles.Clear();
            }

            if (handleTool.OpenHandleTool)
            {
                Object[] objects = GetMultiSelection();
                handleTool.selectedHandleToolObjs.Clear();
                if (OnlyFile(objects) == false && OnlyFolder(objects) == false)
                {
                    PrintLogError("文件和文件夹同时不能选择!");
                    handleTool.selectedHandleToolObjs.Clear();
                    return;
                }

                foreach (var o in objects)
                {
                    handleTool.selectedHandleToolObjs.Add(o);
                }
            }
            else
            {
                handleTool.selectedHandleToolObjs.Clear();
            }

            if (fileTool.Open)
            {
                fileTool.OnSelectionChange();
            }
        }

        public GameObject GetCurrentPrefabRoot()
        {
            // 检查是否在预制体编辑模式中
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // 返回正在编辑的预制体的根对象
                return prefabStage.prefabContentsRoot;
            }

            return null;
        }

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

        /// <summary>
        /// 调用系统的文件浏览器函数
        /// </summary>
        public static void OpenInFileBrowser(string path)
        {
            Process.Start("explorer.exe", path.Replace("/", "\\"));
        }

        // ---------------------------------------------------------------------
        // GUI

        #region GUI

        private void GenericToolGUI()
        {
            EditorGUILayout.LabelField("版本: " + Version);
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
                ArtFolder = EditorGUILayout.ObjectField("素材文件夹目录:", ArtFolder, typeof(Object), false);

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
                PrefabsFolder = EditorGUILayout.ObjectField("预制体文件夹目录或预制体:", PrefabsFolder, typeof(Object), false);

                if (GUILayout.Button("打开文件夹"))
                {
                    if (PrefabsFolder)
                    {
                        if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(prefabsFolder)))
                        {
                            // 打开文件夹
                            OpenInFileBrowser(AssetDatabase.GetAssetPath(PrefabsFolder));
                        }
                        else
                        {
                            // 打开父文件夹
                            string s = AssetDatabase.GetAssetPath(PrefabsFolder).Replace(Path.GetFileName(AssetDatabase.GetAssetPath(prefabsFolder)), "");
                            OpenInFileBrowser(s);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                scriptFolder = EditorGUILayout.ObjectField("脚本文件夹目录:", scriptFolder, typeof(Object), false);
                EditorGUILayout.LabelField("workspace文件:", GUILayout.Width(90));
                workspaceFile = EditorGUILayout.ObjectField(workspaceFile, typeof(Object), false);

                if (GUILayout.Button("打开VSCode"))
                {
                    if (workspaceFile)
                    {
                        OpenInFileBrowser(AssetDatabase.GetAssetPath(workspaceFile));
                    }
                }

                if (GUILayout.Button("打开文件夹"))
                {
                    if (scriptFolder)
                    {
                        if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(scriptFolder)))
                        {
                            // 打开文件夹
                            OpenInFileBrowser(AssetDatabase.GetAssetPath(scriptFolder));
                        }
                        else
                        {
                            // 打开父文件夹
                            string s = AssetDatabase.GetAssetPath(scriptFolder).Replace(Path.GetFileName(AssetDatabase.GetAssetPath(scriptFolder)), "");
                            OpenInFileBrowser(s);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("安全检查设置:", GUILayout.Width(90));
                EditorGUILayout.LabelField("分辨率检查", GUILayout.Width(75));
                resolutionCheck = EditorGUILayout.Toggle(resolutionCheck, GUILayout.Width(30));
            }
            EditorGUILayout.EndHorizontal();

            if (!win) win = GetWindow<ZyTool>();
            EditorGUILayout.LabelField(new string('-', (int)(win.position.width)));
        }


        private void LogGUI()
        {
            EditorGUILayout.Space();
            if (!win) win = GetWindow<ZyTool>();

            EditorGUILayout.LabelField(new string('-', (int)(win.position.width)));


            // log
            if (logStyle == null)
            {
                logStyle = new GUIStyle();
            }

            EditorGUILayout.BeginHorizontal();
            {
                if (latestLogMsg != null)
                {
                    EditorGUILayout.LabelField("Log: " + latestLogMsg.info, latestLogMsg.style, GUILayout.Width(latestLogMsg.info.Length * 10));
                }
                else
                {
                    EditorGUILayout.LabelField("Log: ");
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("详细log", GUILayout.Width(100)))
                {
                    LogWindow.OpenWindow(this, () => isOpenLogWindow = false);
                    isOpenLogWindow = true;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        // ----------------------------------------------------------------------
        // 缓存
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
            cache = ToolCache.Load(path);

            artFolder = AssetDatabase.LoadAssetAtPath<Object>(cache.Get("artFolderPath"));
            prefabsFolder = AssetDatabase.LoadAssetAtPath<Object>(cache.Get("prefabsFolderPath"));
            scriptFolder = AssetDatabase.LoadAssetAtPath<Object>(cache.Get("scriptsFolderPath"));
            workspaceFile = AssetDatabase.LoadAssetAtPath<Object>(cache.Get("workspaceFilePath"));

            fileTool.outsideFolder = cache.Get("outsideFolderPath");
            fileTool.unityFolder = AssetDatabase.LoadAssetAtPath<Object>(cache.Get("unityFolderPath"));

            uiTool.emptySpr = AssetDatabase.LoadAssetAtPath<Sprite>(cache.Get("emptySprPath"));

            handleTool.selectAtlasObj = AssetDatabase.LoadAssetAtPath<Object>(cache.Get("atlasPath"));

            checkTool.checkFolder = AssetDatabase.LoadAssetAtPath<Object>(cache.Get("checkFolderPath"));
            checkTool.checkFolder2 = AssetDatabase.LoadAssetAtPath<Object>(cache.Get("checkFolder2Path"));

            toolCacheFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Editor/ZyTool/Data/" + cacheNames[index] + ".json");
        }

        private void SaveCache()
        {
            cache.Add("artFolderPath", AssetDatabase.GetAssetPath(artFolder));
            cache.Add("prefabsFolderPath", AssetDatabase.GetAssetPath(prefabsFolder));

            cache.Add("outsideFolderPath", fileTool.outsideFolder);
            cache.Add("unityFolderPath", AssetDatabase.GetAssetPath(fileTool.unityFolder));
            cache.Add("scriptsFolderPath", AssetDatabase.GetAssetPath(scriptFolder));
            cache.Add("workspaceFilePath", AssetDatabase.GetAssetPath(workspaceFile));

            cache.Add("emptySprPath", AssetDatabase.GetAssetPath(uiTool.emptySpr));

            cache.Add("atlasPath", AssetDatabase.GetAssetPath(handleTool.selectAtlasObj));

            cache.Add("checkFolderPath", AssetDatabase.GetAssetPath(checkTool.checkFolder));
            cache.Add("checkFolder2Path", AssetDatabase.GetAssetPath(checkTool.checkFolder2));

            string path = Application.dataPath + "/Editor/ZyTool/Data/" + cacheNames[cacheIndex] + ".json";
            cache.Save(path);
        }

        private void NewCache()
        {
            SaveCache();
            string path = EditorUtility.SaveFilePanel("新建缓存", Application.dataPath + "/Editor/ZyTool/Data/", "NewToolCache", "json");
            if (path != "")
            {
                cache = new ToolCache();
                cache.Save(path);

                toolCaches.Add(cache);
                cacheNames.Add(Path.GetFileName(path).Replace(".json", ""));
                cacheIndex = toolCaches.Count - 1;

                LoadCache(cacheIndex);
            }
        }

        // -----------------------------------------------------------------------

        #region 保存前检查

        // 检查逻辑：你可以根据需要自定义这个逻辑
        private bool CheckBeforeSave()
        {
            if (resolutionCheck)
            {
                if (GetMainGameViewSize() != new Vector2(1136, 640))
                {
                    // 打开一个对话框告知用户保存被取消
                    if (ShowDialog("保存失败", "请检查分辨率是否正确再保存", "确定", "关闭检查") == false)
                    {
                        resolutionCheck = false;
                    }

                    return false;
                }
            }

            var prefabRoot = GetCurrentPrefabRoot();
            if (prefabRoot != null)
            {
                var images = prefabRoot.GetComponentsInChildren<Image>(true);
                foreach (var image in images)
                {
                    if (image.sprite != null && image.sprite.name.Contains("示意图") && image.gameObject.activeSelf)
                    {
                        EditorGUIUtility.PingObject(image);
                        // 打开一个对话框告知用户保存被取消
                        if (ShowDialog("保存提示", "检测到示意图节点处于激活状态是否继续保存", "确定", "取消") == false)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
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

        // ----------------------------------------------------------------------
        // 代码功能
        private void OpenInVSCode(string workspacePath)
        {
            Process process = new Process();
            process.StartInfo.FileName = "code"; // VSCode 命令
            process.StartInfo.Arguments = $"\"{workspacePath}\""; // 引号确保路径包含空格时也能正确解析
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            try
            {
                process.Start();
                UnityEngine.Debug.Log($"VSCode Workspace Opened: {workspacePath}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to open VSCode workspace: {e.Message}");
            }
        }
    }
}