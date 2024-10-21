using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ZyTool
{
    public class PrefabsTool
    {
        private ZyTool rootTool;
        
        public PrefabsTool(ZyTool rootTool)
        {
            this.rootTool = rootTool;
        }
        
        [MenuItem("GameObject/ZyTool/Add_Img", false, 1)]
        public static void AddImgSuffix()
        {
            // 获取选中的所有 GameObject
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length <= 0) return;

            // 遍历每个选中的 GameObject
            foreach (GameObject obj in selectedObjects)
            {
                // 检查是否已经添加了 _ref 后缀
                if (!obj.name.EndsWith("_Img"))
                {
                    // 添加 _ref 后缀到对象的名称
                    obj.name += "_Img";
                }
                
                // 刷新编辑器以显示名称更改
                EditorUtility.SetDirty(obj);
            }
        }
        
        /// <summary>
        /// 重命名预制体名称为Image资源名称
        /// </summary>
        [MenuItem("GameObject/ZyTool/重命名为资源名", false, 2)]
        public static void RenameFromRes()
        {
            // 获取选中的所有 GameObject
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length <= 0) return;

            // 遍历每个选中的 GameObject
            foreach (GameObject obj in selectedObjects)
            {
                var image = obj.GetComponent<Image>();
                if (image != null)
                {
                    // 检查是否已经修改过
                    if (obj.name != image.sprite.name)
                    {
                        // 修改名称
                        obj.name = image.sprite.name;
                    }
                }
                
                // 刷新编辑器以显示名称更改
                EditorUtility.SetDirty(obj);
            }
        }
    }
}