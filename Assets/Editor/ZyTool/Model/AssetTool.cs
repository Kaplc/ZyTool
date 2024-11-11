using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ZyTool
{
    public class AssetTool
    {
        private static ZyTool rootTool;

        public AssetTool(ZyTool rootTool)
        {
            AssetTool.rootTool = rootTool;
        }

        [MenuItem("Assets/反向查找控件(ZyTool)", false, 1)]
        public static void SpritePintPrefabs()
        {
            var obj = Selection.activeObject;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(obj));
            if (sprite)
            {
                var pref = rootTool.GetCurrentPrefabRoot();
                if (pref)
                {
                    Image[] images = pref.GetComponentsInChildren<Image>(true);
                    foreach (var image in images)
                    {
                        if (image.sprite == sprite)
                        {
                            EditorGUIUtility.PingObject(image);
                            return;
                        }
                    }
                }
            }
        }
    }
}