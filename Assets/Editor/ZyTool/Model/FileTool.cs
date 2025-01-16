using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZyTool.Data;

namespace ZyTool
{
    public class FileTool
    {
        private readonly ZyTool rootTool;

        private bool copy;
        private bool sync;
        private bool cdn;
        private bool selectedToFile;
        public string outsideFolder = "";
        public Object unityFolder;
        private Object toFile;
        private Object cdnFolder;
        private List<Object> fromFilesList = new List<Object>();
        private List<string> updatedFiles = new List<string>();

        private bool Copy
        {
            get => copy;
            set
            {
                copy = value;

                if (copy)
                {
                    fromFilesList.Clear();
                    toFile = null;
                    selectedToFile = false;
                }
            }
        }

        private bool open;

        public bool Open
        {
            get => open;
            set
            {
                if (value)
                {
                    rootTool.CloseAllTool();
                    fromFilesList.Clear();
                    toFile = null;
                }

                open = value;
            }
        }

        public FileTool(ZyTool rootTool)
        {
            this.rootTool = rootTool;
        }

        public void OnSelectionChange()
        {
            if (copy)
            {
                Object[] objects = rootTool.GetMultiSelection();

                // 选择目标位置
                if (selectedToFile)
                {
                    if (objects.Length == 1)
                    {
                        toFile = objects[0];
                        rootTool.Focus();
                        if (rootTool.OnlyFolder(fromFilesList.ToArray()) || fromFilesList.Count > 1)
                        {
                            // 文件夹只能复制到文件夹
                            if (!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(toFile)))
                            {
                                toFile = null;
                                rootTool.PrintLogError("只能复制到文件夹");
                            }
                        }
                    }
                    else
                    {
                        rootTool.PrintLogError("不支持多选");
                    }
                }
                else
                {
                    // 选择源文件
                    if (rootTool.OnlyFileOrFolder(objects))
                    {
                        if (rootTool.OnlyFile(objects))
                        {
                            if (rootTool.NoneTypeFile<GameObject>(objects))
                            {
                                fromFilesList.Clear();
                                fromFilesList.AddRange(objects);
                                rootTool.Focus();
                            }
                            else
                            {
                                fromFilesList.Clear();
                                rootTool.PrintLogError("只能选择文件");
                            }
                        }
                        else if (rootTool.OnlyFolder(objects))
                        {
                            if (rootTool.IsParentChildFolder(objects))
                            {
                                fromFilesList.Clear();
                                rootTool.PrintLogError("存在父子级嵌套");
                                return;
                            }

                            fromFilesList.Clear();
                            fromFilesList.AddRange(objects);
                            rootTool.Focus();
                        }
                    }
                    else
                    {
                        fromFilesList.Clear();
                    }
                }
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("同步", GUILayout.Height(30)))
                {
                    sync = true;
                    Copy = false;
                    cdn = false;
                }

                if (GUILayout.Button("复制文件", GUILayout.Height(30)))
                {
                    Copy = true;
                    sync = false;
                    cdn = false;
                }

                if (GUILayout.Button("提取CDN", GUILayout.Height(30)))
                {
                    cdn = true;
                    Copy = false;
                    sync = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (sync)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                {
                    unityFolder = EditorGUILayout.ObjectField("Unity工程文件夹目录:", unityFolder, typeof(Object), true);
                    if (unityFolder != null && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(unityFolder)))
                    {
                        unityFolder = null;
                        rootTool.PrintLogError("仅支持文件夹");
                    }

                    if (GUILayout.Button("选择文件夹"))
                    {
                        string path = EditorUtility.OpenFolderPanel("选择Unity工程文件夹目录", AssetDatabase.GetAssetPath(unityFolder), "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            // 相对路径
                            path = path.Replace(Application.dataPath, "Assets");
                            if (AssetDatabase.IsValidFolder(path))
                            {
                                unityFolder = AssetDatabase.LoadAssetAtPath<Object>(path);
                            }
                            else
                            {
                                unityFolder = null;
                                rootTool.PrintLogError("仅支持Asset内文件夹");
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("外部文件夹目录:" + outsideFolder);
                    if (GUILayout.Button("选择文件夹"))
                    {
                        string path = EditorUtility.OpenFolderPanel("外部文件夹目录", outsideFolder, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            outsideFolder = path;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("开始同步"))
                {
                    string unityPath = AssetDatabase.GetAssetPath(unityFolder);
                    if (!AssetDatabase.IsValidFolder(unityPath))
                    {
                        unityFolder = null;
                        rootTool.PrintLogError("只能选择文件夹进行同步");
                        return;
                    }

                    if (rootTool.ShowConfirmWindow("同步提示", "是否确认同步?"))
                    {
                        // 绝对路径
                        unityPath = Path.GetFullPath(unityPath);
                        if (FileSync(unityPath, outsideFolder))
                        {
                            rootTool.PrintLogInfo("同步完成");
                            foreach (var f in updatedFiles)
                            {
                                rootTool.handleTool.ConvertToSprite(f);
                            }

                            updatedFiles.Clear();
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }

            if (copy)
            {
                EditorGUILayout.Space();
                // 复制功能
                EditorGUILayout.BeginHorizontal();
                {
                    if (copy)
                    {
                        EditorGUILayout.BeginVertical();
                        {
                            if (fromFilesList.Count > 0)
                            {
                                foreach (Object file in fromFilesList)
                                {
                                    EditorGUILayout.ObjectField(file, typeof(Object), false);
                                }
                            }
                            else
                            {
                                EditorGUILayout.ObjectField(null, typeof(Object), false);
                            }

                            if (!selectedToFile)
                            {
                                if (GUILayout.Button("确定 Ctrl+Q") || rootTool.KeyCodeQConfirm() && fromFilesList.Count > 0)
                                {
                                    selectedToFile = true;
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("重新选择"))
                                {
                                    fromFilesList.Clear();
                                    toFile = null;
                                    selectedToFile = false;
                                }
                            }
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.LabelField("->", GUILayout.Width(20));
                        EditorGUILayout.ObjectField(toFile, typeof(Object), false);

                        if (GUILayout.Button("复制到或替换 Ctrl+Q"))
                        {
                            if (toFile)
                            {
                                CopyFileTo(fromFilesList.ToArray(), toFile);
                            }
                        }

                        if (toFile && rootTool.KeyCodeQConfirm())
                        {
                            CopyFileTo(fromFilesList.ToArray(), toFile);
                        }

                        if (GUILayout.Button("清空"))
                        {
                            fromFilesList.Clear();
                            toFile = null;
                            selectedToFile = false;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (cdn)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("提取CDN文件(后缀为CDN的Sprite会被提取):");

                EditorGUILayout.BeginHorizontal();
                {
                    cdnFolder = EditorGUILayout.ObjectField("CDN文件夹:", cdnFolder, typeof(Object), true);
                    if (GUILayout.Button("选择"))
                    {
                        string path = EditorUtility.OpenFolderPanel("选择CDN文件夹", AssetDatabase.GetAssetPath(cdnFolder), "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            // 相对路径
                            path = path.Replace(Application.dataPath, "Assets");
                            if (AssetDatabase.IsValidFolder(path))
                            {
                                cdnFolder = AssetDatabase.LoadAssetAtPath<Object>(path);
                            }
                            else
                            {
                                cdnFolder = null;
                                rootTool.PrintLogError("仅支持Asset内文件夹");
                            }
                        }
                    }

                    // 提取cdn后缀的文件
                    if (GUILayout.Button("提取到"))
                    {
                        if (cdnFolder == null)
                        {
                            rootTool.PrintLogError("请选择CDN文件夹");
                            return;
                        }

                        string cdnFolderPath = AssetDatabase.GetAssetPath(cdnFolder);
                        if (!AssetDatabase.IsValidFolder(cdnFolderPath))
                        {
                            rootTool.PrintLogError("只能选择文件夹进行提取");
                            return;
                        }

                        string oFd = EditorUtility.OpenFolderPanel("提取到", AssetDatabase.GetAssetPath(cdnFolder), "");
                        if (string.IsNullOrEmpty(oFd))
                        {
                            return;
                        }

                        // 判断是否选择了子文件夹
                        // 转换为相对路径
                        string iFd = oFd.Replace(Application.dataPath, "Assets");
                        if (rootTool.IsParentChildFolder(new[] { iFd, AssetDatabase.GetAssetPath(cdnFolder) }))
                        {
                            rootTool.PrintLogError("不支持生成在选择子文件夹");
                            return;
                        }

                        if (rootTool.ShowConfirmWindow("提取CDN文件", "是否删除源文件?", "确认删除", "不删除"))
                        {
                            ExtractCdnFile(cdnFolderPath, oFd, true);
                        }
                        else
                        {
                            ExtractCdnFile(cdnFolderPath, oFd, false);
                        }
            
                        rootTool.PrintLogInfo("提取完成");
                        AssetDatabase.Refresh();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void ExtractCdnFile(string cdnFd, string oFd, bool isDelete)
        {
            // 保持目录结构提取
            foreach (string file in Directory.GetFiles(cdnFd))
            {
                string fileName = Path.GetFileName(file);
                if (fileName.EndsWith("CDN.png") || fileName.EndsWith("CDN.jpg") || fileName.EndsWith("CDN.jpeg"))
                {
                    string targetFilePath = Path.Combine(oFd, fileName);
                    File.Copy(file, targetFilePath, true);
                    // 删除源文件
                    if (isDelete)
                    {
                        File.Delete(file);
                    }
                    rootTool.PrintLogInfo("添加文件: " + Path.GetFileName(targetFilePath));
                }
            }

            // 递归提取子文件夹
            foreach (string directory in Directory.GetDirectories(cdnFd))
            {
                string dirName = Path.GetFileName(directory);
                string targetDirectoryPath = Path.Combine(oFd, dirName);
                if (!Directory.Exists(targetDirectoryPath))
                {
                    Directory.CreateDirectory(targetDirectoryPath);
                    rootTool.PrintLogInfo("添加文件夹: " + dirName);
                }

                ExtractCdnFile(directory, targetDirectoryPath, isDelete);
            }
        }

        #region 同步

        private bool FileSync(string i, string o)
        {
            if (string.IsNullOrEmpty(i) || string.IsNullOrEmpty(o))
            {
                rootTool.PrintLogError("路径错误");
                return false;
            }

            // 确保目标文件夹存在
            if (!Directory.Exists(i))
            {
                Directory.CreateDirectory(i);
                rootTool.PrintLogInfo("添加文件夹: " + i);
            }

            // 复制所有文件
            foreach (string file in Directory.GetFiles(o))
            {
                string fileName = Path.GetFileName(file);
                string targetFilePath = Path.Combine(i, fileName);

                // 只在源文件更新时复制
                if (!File.Exists(targetFilePath) || File.GetLastWriteTime(file) > File.GetLastWriteTime(targetFilePath))
                {
                    File.Copy(file, targetFilePath, true);
                    rootTool.PrintLogInfo("添加文件: " + Path.GetFileName(targetFilePath));
                    updatedFiles.Add(targetFilePath);
                }
            }

            // 递归复制子文件夹
            foreach (string directory in Directory.GetDirectories(o))
            {
                string dirName = Path.GetFileName(directory);
                string targetDirectoryPath = Path.Combine(i, dirName);
                FileSync(targetDirectoryPath, directory); // 递归调用
            }

            // 删除不再存在的文件和文件夹
            DeleteRemovedFilesAndFolders(o, i);

            // 更新 AssetDatabase
            AssetDatabase.Refresh();

            return true;
        }

        private void DeleteRemovedFilesAndFolders(string sourceDir, string targetDir)
        {
            // 删除文件
            foreach (string targetFile in Directory.GetFiles(targetDir))
            {
                string fileName = Path.GetFileName(targetFile);
                string sourceFilePath = Path.Combine(sourceDir, fileName);
                if (!File.Exists(sourceFilePath) && !sourceFilePath.EndsWith(".meta"))
                {
                    File.Delete(targetFile);
                    rootTool.PrintLogInfo("删除文件: " + Path.GetFileName(targetFile));
                }
            }

            // 删除文件夹
            foreach (string targetDirectory in Directory.GetDirectories(targetDir))
            {
                string dirName = Path.GetFileName(targetDirectory);
                string sourceDirectoryPath = Path.Combine(sourceDir, dirName);
                if (!Directory.Exists(sourceDirectoryPath))
                {
                    Directory.Delete(targetDirectory, true); // true 用于递归删除
                    rootTool.PrintLogInfo("删除文件夹: " + Path.GetDirectoryName(targetDirectory));
                }
            }
        }

        #endregion

        #region 文件复制

        private void CopyFileTo(Object[] fromObjects, Object toObject)
        {
            foreach (Object file in fromObjects)
            {
                if (file == null)
                {
                    continue;
                }

                string fromPath = AssetDatabase.GetAssetPath(file);
                string toPath = AssetDatabase.GetAssetPath(toObject);
                if (AssetDatabase.IsValidFolder(toPath))
                {
                    string fileName = Path.GetFileName(fromPath);
                    string toPathWithFile = Path.Combine(toPath, fileName);
                    AssetDatabase.CopyAsset(fromPath, toPathWithFile);
                }
                else
                {
                    // 文件替换
                    // 获取正在编辑的预制体对象
                    GameObject prefabRoot = rootTool.GetCurrentPrefabRoot();
                    if (prefabRoot == null)
                    {
                        // 如果没有正在编辑的预制体对象，直接复制文件
                        AssetDatabase.CopyAsset(fromPath, toPath);
                        continue;
                    }

                    // 查找所有Image
                    Image[] images = prefabRoot.GetComponentsInChildren<Image>();
                    List<Image> i = new List<Image>();
                    foreach (Image image in images)
                    {
                        if (image.sprite == null)
                        {
                            continue;
                        }

                        // 找到所有使用的原图片资源
                        if (AssetDatabase.GetAssetPath(image.sprite) == toPath)
                        {
                            i.Add(image);
                        }
                    }

                    if (i.Count > 0)
                    {
                        // 复制后重新加载
                        AssetDatabase.CopyAsset(fromPath, toPath);
                        var s = AssetDatabase.LoadAssetAtPath<Sprite>(toPath);
                        foreach (Image image in i)
                        {
                            image.sprite = s;
                            image.SetNativeSize();
                            EditorUtility.SetDirty(image);
                        }
                    }
                    else
                    {
                        AssetDatabase.CopyAsset(fromPath, toPath);
                    }
                }
            }

            fromFilesList.Clear();
            toFile = null;
            selectedToFile = false;
            EditorUtility.SetDirty(rootTool.GetCurrentPrefabRoot());
            AssetDatabase.Refresh();
        }

        #endregion
    }
}