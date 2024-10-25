using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace ZyTool
{
    public partial class ZyTool
    {
        private string selectAtlasPath;
        private Object selectAtlasObj;
        private Object selectedHandleToolObj;
        private List<Object> selectedHandleToolObjs = new List<Object>();

        private bool openHandleTool;

        private bool OpenHandleTool
        {
            get => openHandleTool;
            set
            {
                if (value)
                {
                    CloseAllTool();
                }

                openHandleTool = value;
                OnSelectionChange();
            }
        }

        private void HandleToolGUI()
        {
            EditorGUILayout.LabelField("选中的文件或文件夹:");
            foreach (var o in selectedHandleToolObjs)
            {
                EditorGUILayout.ObjectField(o, typeof(Object), false);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("转Sprite"))
            {
                Object[] objects = GetMultiSelection();
                if (objects.Length == 0)
                {
                    return;
                }

                if (OnlyFile(objects))
                {
                    ConvertToSprite(GetFileNames(objects));
                    AssetDatabase.Refresh();
                    return;
                }

                if (OnlyFolder(objects))
                {
                    FolderConvertToSprite(GetFolderNames(objects));
                    return;
                }
            }

            if (GUILayout.Button("打图集"))
            {
                Object[] objects = GetMultiSelection();
                if (objects.Length == 0)
                {
                    return;
                }

                selectAtlasPath = EditorUtility.SaveFilePanel("保存图集", selectAtlasPath, "NewAtlas", "spriteatlas");
                if (!string.IsNullOrEmpty(selectAtlasPath))
                {
                    if (OnlyFile(objects))
                    {
                        CreateSpriteAtlas(GetFileNames(objects), selectAtlasPath);
                        return;
                    }

                    WriteLogError("不支持文件夹!");
                    selectedHandleToolObjs.Clear();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                selectAtlasObj = EditorGUILayout.ObjectField("目标图集: ", selectAtlasObj, typeof(SpriteAtlas), false);

                if (GUILayout.Button("加入目标图集"))
                {
                    Object[] objects = GetMultiSelection();
                    if (objects.Length == 0)
                    {
                        return;
                    }

                    if (selectAtlasObj == null)
                    {
                        selectAtlasPath = EditorUtility.OpenFilePanel("选择图集", selectAtlasPath, "spriteatlas");
                        if (!string.IsNullOrEmpty(selectAtlasPath))
                        {
                            if (OnlyFile(objects))
                            {
                                AddSpriteToAtlas(GetFileNames(objects), selectAtlasPath);
                                return;
                            }

                            WriteLogError("不支持文件夹!");
                            selectedHandleToolObjs.Clear();
                        }
                    }
                    else
                    {
                        if (OnlyFile(objects))
                        {
                            AddSpriteToAtlas(GetFileNames(objects), AssetDatabase.GetAssetPath(selectAtlasObj));
                            return;
                        }

                        WriteLogError("不支持文件夹!");
                        selectedHandleToolObjs.Clear();
                    }
                }

                if (GUILayout.Button("清空目标图集"))
                {
                    if (selectAtlasObj)
                    {
                        var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GetAssetPath(selectAtlasObj));
                        if (spriteAtlas != null)
                        {
                            spriteAtlas.Remove(spriteAtlas.GetPackables());
                        }
                    }

                    AssetDatabase.Refresh();
                }

                if (GUILayout.Button("打开文件夹"))
                {
                    string path = AssetDatabase.GetAssetPath(selectAtlasObj);
                    OpenInFileBrowser(path.Replace(Path.GetFileName(path), ""));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        #region 转Sprite

        public void ConvertToSprite(params string[] paths)
        {
            int count = 0;
            int successCount = 0;
            foreach (var path in paths)
            {
                count++;
                if
                (
                    Path.GetFileName(path).EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileName(path).EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileName(path).EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                )
                {
                    // 绝对路径转相对路径
                    string newPath = GetRelativePath(path);

                    TextureImporter textureImporter = AssetImporter.GetAtPath(newPath) as TextureImporter;
          
                    if (textureImporter != null)
                    {
                        if (textureImporter.textureType == TextureImporterType.Sprite && textureImporter.maxTextureSize == 1024 &&
                            textureImporter.textureCompression == TextureImporterCompression.Uncompressed)
                        {
                            continue;
                        }
                        
                        textureImporter.textureType = TextureImporterType.Sprite;
                        // 判断尺寸
                        // 从文件读取图片数据
                        byte[] imageData = File.ReadAllBytes(Application.dataPath + newPath.Replace("Assets", ""));
        
                        // 创建Texture2D，但不指定大小
                        Texture2D texture = new Texture2D(2, 2); 
        
                        // 使用 LoadImage 解析字节流，自动适应图片真实尺寸
                        texture.LoadImage(imageData);

                        int max = Mathf.Max(texture.width, texture.height);
                        if (max > 1024)
                        {
                            if (ShowDialog("设置最大尺寸提示", $"{Path.GetFileName(newPath)}图片尺寸大于1024，请选择处理方式?", "继续", "跳过"))
                            {
                                // 设置最大尺寸
                                textureImporter.maxTextureSize = 1024;
                            }
                            else
                            {
                                textureImporter.maxTextureSize = 8192;
                            }
                        }
                        else
                        {
                            // 设置最大尺寸
                            textureImporter.maxTextureSize = 1024;
                        }
                        // 设置压缩类型
                        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                        textureImporter.SaveAndReimport();

                        successCount++;
                    }
                }
                else
                {
                    WriteLogWarning("非图片文件跳过: " + Path.GetFileName(path));
                }
            }

            WriteLogInfo($"转换完成! 共{count}个文件, 转换成功{successCount}个, 跳过{count - successCount}个");
        }

        /// <summary>
        /// 文件夹下的图片转换为Sprite
        /// </summary>
        public void FolderConvertToSprite(string[] folders)
        {
            List<string> fileNames = new List<string>();
            // 找到所有文件
            foreach (var folder in folders)
            {
                // 使用 FindAssets 方法获取所有资产
                string[] guids = AssetDatabase.FindAssets("", new[] { folder });

                foreach (string guid in guids)
                {
                    string fPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetDatabase.IsValidFolder(fPath))
                    {
                        continue;
                    }

                    if (!fileNames.Contains(fPath))
                    {
                        fileNames.Add(fPath);
                    }
                }
            }

            ConvertToSprite(fileNames.ToArray());
            AssetDatabase.Refresh();
        }

        #endregion

        /// <summary>
        /// 打图集
        /// </summary>
        private void CreateSpriteAtlas(string[] spritePaths, string atlasPath)
        {
            // 创建图集
            var spriteAtlas = new SpriteAtlas();
            spriteAtlas.name = atlasPath.Split('/')[atlasPath.Split('/').Length - 1];

            foreach (var p in spritePaths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (sprite != null)
                {
                    spriteAtlas.Add(new[] { sprite });
                }
                else
                {
                    WriteLogWarning("文件可能不是Sprite? 跳过" + Path.GetFileName(p));
                }
            }

            // 保存图集
            AssetDatabase.CreateAsset(spriteAtlas, "Assets" + atlasPath.Substring(Application.dataPath.Length));
            WriteLogInfo("已创建图集: " + atlasPath.Split('/')[atlasPath.Split('/').Length - 1]);
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(spriteAtlas);
        }

        /// <summary>
        /// 加入已有图集
        /// </summary>
        private void AddSpriteToAtlas(string[] spritePaths, string atlasPath)
        {
            SpriteAtlas spriteAtlas;
            if (atlasPath.Contains("Assets"))
            {
                spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            }
            else
            {
                spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets" + atlasPath.Substring(Application.dataPath.Length));
            }
            
            if (spriteAtlas != null)
            {
                foreach (var p in spritePaths)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (sprite != null)
                    {
                        // 判断是否已经存在
                        if (!spriteAtlas.GetSprite(sprite.name))
                        {
                            spriteAtlas.Add(new[] { sprite });
                        }
                        else
                        {
                            WriteLogWarning("已存在: " + sprite.name);
                        }
                    }
                    else
                    {
                        WriteLogWarning("文件可能不是Sprite? 跳过" + Path.GetFileName(p));
                    }
                }

                WriteLogInfo("已添加到图集: " + atlasPath.Split('/')[atlasPath.Split('/').Length - 1]);
                AssetDatabase.Refresh();
            }
        }
    }
}