﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ZyTool
{
    public class UITool
    {
        private ZyTool rootTool;

        private bool generic;
        private bool move;
        private bool generate;

        private bool controlling;

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
                else
                {
                    StopMoveTool();
                    uiObj = null;
                }

                open = value;
                rootTool.OnSelectionChange();
            }
        }

        // generic
        private bool copySimpleTsf;
        private bool addEmptySpr;
        private bool addSprToImg;
        private Vector3 copyPos;
        public Sprite emptySpr;
        private Sprite originSpr;
        private Object uiObj;
        private GameObject copySimpleObj;
        private RectTransform selectedRect;
        private Stack<Vector2> revokeStack = new Stack<Vector2>();
        private Stack<Vector2> forwardStack = new Stack<Vector2>();
        private Dictionary<Object, Vector3> revokePastePos = new Dictionary<Object, Vector3>();
        private Dictionary<Object, Sprite> revokeEmptySpr = new Dictionary<Object, Sprite>();

        // generate
        private bool isConfirm;
        private GameObject gnrParentObj; // 要生成的父对象
        private List<Texture2D> spriteList = new List<Texture2D>();
        private GameObject selectedImgObj;
        private Sprite selectedImgSpr;

        // copy
        private bool copy;
        private Object copyObj;
        private Dictionary<string, TransformData> copyData = new Dictionary<string, TransformData>();

        // 子功能开关
        private bool Generic
        {
            set
            {
                generic = value;
                if (value)
                {
                    Move = false;
                    Generate = false;
                }
            }
        }

        private bool Move
        {
            set
            {
                move = value;
                if (value)
                {
                    Generic = false;
                    Generate = false;
                    StartMoveTool();
                }
            }
        }

        private bool Generate
        {
            set
            {
                generate = value;
                if (value)
                {
                    Move = false;
                    Generic = false;
                }
            }
        }

        private bool Copy
        {
            set
            {
                copy = value;
                if (value)
                {
                    Move = false;
                    Generate = false;
                    Generic = false;
                }
            }
        }


        public UITool(ZyTool rootTool)
        {
            this.rootTool = rootTool;
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("二级菜单");
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("常用", GUILayout.Height(30)))
                {
                    Generic = true;
                }

                if (GUILayout.Button("移动", GUILayout.Height(30)))
                {
                    Move = true;
                }

                if (GUILayout.Button("生成", GUILayout.Height(30)))
                {
                    Generate = true;
                }

                if (GUILayout.Button("复制", GUILayout.Height(30)))
                {
                    Copy = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (move)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("当前选中的控件：");
                EditorGUILayout.ObjectField(uiObj, typeof(GameObject), true);

                if (GUILayout.Button("停止移动工具"))
                {
                    StopMoveTool();
                    return;
                }

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("撤销"))
                    {
                        if (revokeStack.Count > 0)
                        {
                            // 同时压入反撤销
                            forwardStack.Push(selectedRect.anchoredPosition);
                            selectedRect.anchoredPosition = revokeStack.Pop();
                        }
                    }

                    if (GUILayout.Button("前进"))
                    {
                        if (forwardStack.Count > 0)
                        {
                            // 同时压入撤销
                            revokeStack.Push(selectedRect.anchoredPosition);
                            selectedRect.anchoredPosition = forwardStack.Pop();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                Input();
            }

            if (generic)
            {
                SimpleCopyTsfGUI();

                AddEmptySprGUI();

                AddSprToImgGUI();

                AllText2SetWWWWGUI();
            }

            if (generate)
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("当前选中的Sprite：");
                    EditorGUILayout.BeginVertical();
                    {
                        if (spriteList.Count > 0)
                        {
                            foreach (var sprite in spriteList)
                            {
                                EditorGUILayout.ObjectField(sprite, typeof(Sprite), true);
                            }
                        }
                        else
                        {
                            EditorGUILayout.ObjectField(null, typeof(Sprite), true);
                        }
                    }

                    if (!isConfirm)
                    {
                        if (GUILayout.Button("确认选中的Sprite"))
                        {
                            isConfirm = true;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("重新选择"))
                        {
                            spriteList.Clear();
                            gnrParentObj = null;
                            isConfirm = false;
                        }
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.LabelField("当前选中的GameObject");
                    EditorGUILayout.ObjectField(gnrParentObj, typeof(GameObject), true);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();


                if (GUILayout.Button("选中的Sprite生成Image控件"))
                {
                    if (gnrParentObj != null && spriteList.Count > 0)
                    {
                        GenerateImageObj(gnrParentObj, spriteList);
                    }
                }
            }

            if (copy)
            {
                var sel = rootTool.GetSingleSelection() as GameObject;

                if (sel != null) copyObj = sel;
                copyObj = EditorGUILayout.ObjectField(copyObj, typeof(GameObject), true);

                if (GUILayout.Button("复制所有Tsf"))
                {
                    CopyAllTransform(sel.transform as RectTransform);
                }

                if (GUILayout.Button("粘贴"))
                {
                    PasteAllTransform(sel.transform as RectTransform);
                }
            }
        }

        public void OnSelectionChange()
        {
            if (generic)
            {
                if (copySimpleTsf || addEmptySpr)
                {
                    selectedRect = Selection.activeTransform as RectTransform;
                    if (selectedRect == null)
                    {
                        rootTool.PrintLogError("不存在RectTransform组件");
                        uiObj = null;
                        return;
                    }

                    uiObj = selectedRect ? selectedRect.gameObject : null;

                    if (addEmptySpr)
                    {
                        ZyTool.win.Focus();
                    }
                }

                if (addSprToImg)
                {
                    Object[] objs = rootTool.GetMultiSelection();

                    selectedImgSpr = null;
                    selectedImgObj = null;

                    foreach (var obj in objs)
                    {
                        if (obj is Texture2D texture2D)
                        {
                            // 通过 Texture 对象获取该对象的路径
                            string assetPath = AssetDatabase.GetAssetPath(texture2D);

                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                selectedImgSpr = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
                            }
                        }

                        if (obj is GameObject go)
                        {
                            if (go.GetComponent<Image>() != null)
                            {
                                selectedImgObj = go;
                            }
                        }
                    }

                    if (selectedImgObj != null && selectedImgSpr != null)
                    {
                        ZyTool.win.Focus();
                    }
                }
            }

            if (move)
            {
                selectedRect = Selection.activeTransform as RectTransform;
                if (selectedRect == null)
                {
                    rootTool.PrintLogError("不存在RectTransform组件");
                    uiObj = null;
                    return;
                }

                uiObj = selectedRect ? selectedRect.gameObject : null;

                rootTool.PrintLogInfo("切换控件为: " + selectedRect.name);
                // 记录初始位置
                revokeStack.Push(selectedRect.anchoredPosition);
                // 自动焦点到窗口
                ZyTool.OpenWindow();
            }

            if (generate)
            {
                if (!isConfirm)
                {
                    spriteList.Clear();

                    Object[] objs = rootTool.GetMultiSelection();

                    foreach (var obj in objs)
                    {
                        if (obj is Texture2D)
                        {
                            // 通过 Texture 对象获取该对象的路径
                            string assetPath = AssetDatabase.GetAssetPath(obj);

                            // 使用 TextureImporter 来获取导入器
                            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                            if (textureImporter != null)
                            {
                                // 判断纹理类型是否为 Sprite
                                if (textureImporter.textureType == TextureImporterType.Sprite)
                                {
                                    spriteList.Add(obj as Texture2D);
                                }
                                else
                                {
                                    rootTool.PrintLogError("不是Sprite");
                                }
                            }
                        }
                    }
                }
                else
                {
                    gnrParentObj = rootTool.GetSingleSelection() as GameObject;
                }
            }
        }

        private void Input()
        {
            if (selectedRect != null)
            {
                if (Event.current.control)
                {
                    // 处理移动逻辑
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.UpArrow:
                            RecordCurrent();
                            selectedRect.anchoredPosition += new Vector2(0, 1.5f);
                            break;
                        case KeyCode.DownArrow:
                            RecordCurrent();
                            selectedRect.anchoredPosition += new Vector2(0, -1.5f);
                            break;
                        case KeyCode.LeftArrow:
                            RecordCurrent();
                            selectedRect.anchoredPosition += new Vector2(-1.5f, 0);
                            break;
                        case KeyCode.RightArrow:
                            RecordCurrent();
                            selectedRect.anchoredPosition += new Vector2(1.5f, 0);
                            break;
                    }
                }
                else
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.UpArrow:
                            RecordCurrent();
                            selectedRect.anchoredPosition += new Vector2(0, 0.25f);
                            break;
                        case KeyCode.DownArrow:
                            RecordCurrent();
                            selectedRect.anchoredPosition += new Vector2(0, -0.25f);
                            break;
                        case KeyCode.LeftArrow:
                            RecordCurrent();
                            selectedRect.anchoredPosition += new Vector2(-0.25f, 0);
                            break;
                        case KeyCode.RightArrow:
                            RecordCurrent();
                            selectedRect.anchoredPosition += new Vector2(0.25f, 0);
                            break;
                    }
                }

                // 检测方向键抬起
                if (Event.current.type == EventType.KeyUp)
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.UpArrow:
                        case KeyCode.DownArrow:
                        case KeyCode.LeftArrow:
                        case KeyCode.RightArrow:
                            controlling = false;
                            break;
                    }
                }
            }
        }

        private void RecordCurrent()
        {
            if (controlling == false)
            {
                // 记录当前位置
                revokeStack.Push(selectedRect.anchoredPosition);
                controlling = true;
            }
        }

        #region 启动相关

        private void StartMoveTool()
        {
            if (move) return;

            move = true;
            rootTool.OnSelectionChange();
            rootTool.PrintLogInfo("移动工具启动成功");
        }

        private void StopMoveTool()
        {
            if (!move) return;

            move = false;
            revokeStack.Clear();
            forwardStack.Clear();
            rootTool.PrintLogInfo("移动工具停止");
        }

        #endregion

        // --------------------------------------------------------------------------------
        // 复制rect
        private void SimpleCopyTsfGUI()
        {
            EditorGUILayout.Space();

            if (!copySimpleTsf)
            {
                if (GUILayout.Button("复制RectTransform", GUILayout.Width(200)))
                {
                    copySimpleTsf = true;
                    addEmptySpr = false;
                    addSprToImg = false;
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("关闭", GUILayout.Width(70)))
                    {
                        copySimpleTsf = false;
                    }

                    EditorGUILayout.LabelField("当前选中的控件：", GUILayout.Width(50));
                    EditorGUILayout.ObjectField(uiObj, typeof(GameObject), true);
                    EditorGUILayout.LabelField("<--复制源:", GUILayout.Width(70));
                    EditorGUILayout.ObjectField(copySimpleObj, typeof(Object), true);

                    if (GUILayout.Button("复制"))
                    {
                        if (selectedRect)
                        {
                            CopyRect(selectedRect);
                        }
                    }

                    if (GUILayout.Button("粘贴"))
                    {
                        if (selectedRect)
                        {
                            PasteRect(selectedRect);
                        }
                    }

                    if (GUILayout.Button("撤销"))
                    {
                        if (selectedRect)
                        {
                            RevokeRect(selectedRect);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void CopyRect(RectTransform r)
        {
            copyPos = r.anchoredPosition;
            copySimpleObj = r.gameObject;
        }

        private void PasteRect(RectTransform r)
        {
            revokePastePos[r.gameObject] = r.anchoredPosition;
            r.anchoredPosition = copyPos;
        }

        private void RevokeRect(RectTransform r)
        {
            if (revokePastePos.ContainsKey(r.gameObject))
            {
                r.anchoredPosition = revokePastePos[r.gameObject];
            }
        }

        // --------------------------------------------------------------------------------
        // 添加空白图片
        private void AddEmptySprGUI()
        {
            EditorGUILayout.Space();

            if (!addEmptySpr)
            {
                if (GUILayout.Button("添加空白图片", GUILayout.Width(200)))
                {
                    copySimpleTsf = false;
                    addEmptySpr = true;
                    addSprToImg = false;

                    uiObj = rootTool.GetSingleSelection() as GameObject;
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("关闭", GUILayout.Width(70)))
                    {
                        addEmptySpr = false;
                    }

                    EditorGUILayout.LabelField("当前选中的控件：", GUILayout.Width(50));
                    EditorGUILayout.ObjectField(uiObj, typeof(GameObject), true);

                    EditorGUILayout.LabelField("空白图片资源", GUILayout.Width(100));
                    emptySpr = EditorGUILayout.ObjectField(emptySpr, typeof(Sprite), false) as Sprite;
                    if (GUILayout.Button("添加Ctrl+Q"))
                    {
                        if (selectedRect)
                        {
                            AddEmptyPicToImageCpm(selectedRect.gameObject);
                        }
                    }

                    if (rootTool.KeyCodeQConfirm())
                    {
                        if (selectedRect)
                        {
                            AddEmptyPicToImageCpm(selectedRect.gameObject);
                        }
                    }

                    if (GUILayout.Button("撤销"))
                    {
                        if (selectedRect)
                        {
                            RevokeEmptyPicToImageCpm(selectedRect.gameObject);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddEmptyPicToImageCpm(Object obj)
        {
            if (emptySpr == null)
            {
                rootTool.PrintLogError("空白图片为空");
                return;
            }

            GameObject go = obj as GameObject;

            if (go == null)
            {
                return;
            }

            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                rootTool.PrintLogError("没有Image组件");
                return;
            }

            revokeEmptySpr[go] = image.sprite;
            image.sprite = emptySpr;

            // 标记为已修改
            EditorUtility.SetDirty(go);
            ZyTool.win.Focus();
        }

        private void RevokeEmptyPicToImageCpm(Object obj)
        {
            if (emptySpr == null)
            {
                rootTool.PrintLogError("空白图片为空");
                return;
            }

            GameObject go = obj as GameObject;
            if (go == null)
            {
                return;
            }

            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                rootTool.PrintLogError("没有Image组件");
                return;
            }

            if (revokeEmptySpr.ContainsKey(go))
            {
                image.sprite = revokeEmptySpr[go];
            }

            // 标记为已修改
            EditorUtility.SetDirty(go);
        }

        // --------------------------------------------------------------------------------
        // 图片生成Image

        private void GenerateImageObj(GameObject parent, List<Texture2D> sprites)
        {
            foreach (var s in sprites)
            {
                GameObject go = new GameObject();
                go.name = s.name + "_Img";
                go.transform.SetParent(parent.transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                Image i = go.AddComponent<Image>();
                i.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(s));
                i.SetNativeSize();
            }

            // 标记为已修改
            EditorUtility.SetDirty(parent);
        }

        // --------------------------------------------------------------------------------
        // 添加图片到Image
        private void AddSprToImgGUI()
        {
            EditorGUILayout.Space();

            if (!addSprToImg)
            {
                if (GUILayout.Button("添加图片到Image", GUILayout.Width(200)))
                {
                    copySimpleTsf = false;
                    addEmptySpr = false;
                    addSprToImg = true;
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("关闭", GUILayout.Width(70)))
                    {
                        addSprToImg = false;
                    }

                    EditorGUILayout.LabelField("当前选中的Image控件：", GUILayout.Width(130));
                    EditorGUILayout.ObjectField(selectedImgObj, typeof(GameObject), true);
                    if (selectedImgObj == null || selectedImgObj.GetComponent<Image>() == null)
                    {
                        selectedImgObj = null;
                    }

                    EditorGUILayout.LabelField("当前选中的Sprite：", GUILayout.Width(100));
                    EditorGUILayout.ObjectField(selectedImgSpr, typeof(Sprite), true);
                    if (selectedImgSpr == null)
                    {
                        selectedImgSpr = null;
                    }

                    if (GUILayout.Button("添加 (Shift+Q)"))
                    {
                        if (selectedImgObj && selectedImgSpr)
                        {
                            AddSprToImgCpm(selectedImgObj.GetComponent<Image>(), selectedImgSpr);
                        }
                    }

                    if (selectedImgObj != null && selectedImgSpr != null)
                    {
                        Event e = Event.current;

                        if (e.control && e.type == EventType.KeyDown && e.keyCode == KeyCode.Q)
                        {
                            AddSprToImgCpm(selectedImgObj.GetComponent<Image>(), selectedImgSpr);
                            e.Use(); // 标记事件为已使用，防止其他组件继续处理
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddSprToImgCpm(Image i, Sprite s)
        {
            if (i && s)
            {
                i.sprite = s;
                i.SetNativeSize();

                // 标记为已修改
                EditorUtility.SetDirty(i);
            }
        }


        // --------------------------------------------------------------------------------
        // 复制所有Transform
        private void CopyAllTransform(RectTransform t)
        {
            copyData.Clear();
            RectTransform[] children = t.GetComponentsInChildren<RectTransform>();
            foreach (RectTransform child in children)
            {
                TransformData data = new TransformData();
                data.position = child.localPosition;
                data.rotation = child.localRotation;
                data.scale = child.localScale;
                data.size = child.sizeDelta;
                var name = GetTransformPath(child);
                if (copyData.ContainsKey(name) == false)
                {
                    copyData.Add(name, data);
                }
            }
        }

        private string GetTransformPath(Transform target)
        {
            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }

        private void PasteAllTransform(RectTransform t)
        {
            RectTransform[] children = t.GetComponentsInChildren<RectTransform>();
            foreach (RectTransform child in children)
            {
                var name = GetTransformPath(child);
                if (copyData.ContainsKey(name))
                {
                    child.localPosition = copyData[name].position;
                    child.localRotation = copyData[name].rotation;
                    child.localScale = copyData[name].scale;
                    child.sizeDelta = copyData[name].size;
                }
            }
        }

        // --------------------------------------------------------------------------------
        // 所有text2文本设置为wwww
        private void AllText2SetWWWWGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("所有text2文本设置为wwww", GUILayout.Width(200)))
            {
                copySimpleTsf = false;
                addEmptySpr = false;
                addSprToImg = false;
                
                AllText2SetWWWW();
            }
        }

        private void AllText2SetWWWW()
        {
            GameObject root = rootTool.GetCurrentPrefabRoot();
            if (root == null)
            {
                return;
            }

            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text t in texts)
            {
                t.text = "wwww";
            }
            
            // 标记为已修改
            EditorUtility.SetDirty(root);
        }
    }
}