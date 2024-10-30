using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ZyTool.Generic
{
    public class AutoCloseDialog : EditorWindow
    {
        private static AutoCloseDialog window;
        private float countdown = 5f; // 5秒倒计时
        private string sure = "确定";
        private string cancel = "取消";
        private string msg = "是否继续操作?";
        private bool defaultOption = true;
        private Action<bool> onCloseCallback; // 回调函数，用于处理用户选择
        private int s;

        // 打开弹窗的方法
        public static void ShowDialog(string title, string message, int timeout, Action<bool> callback,
            string yesOption = "确定", string noOption = "取消",
            bool defaultOption = true)
        {
            // 创建并显示窗口
            window = GetWindow<AutoCloseDialog>(title);
            window.position = new Rect(Screen.width , Screen.height , 300, 150);
            window.maxSize = new Vector2(300, 150);
            window.minSize = new Vector2(300, 150);
            window.countdown = timeout;
            window.onCloseCallback = callback;

            window.sure = yesOption;
            window.cancel = noOption;
            window.msg = message;

            window.defaultOption = defaultOption;

            window.s = DateTime.Now.Minute * 60 + DateTime.Now.Second + timeout;

            // 在弹窗期间禁止编辑器交互
            window.ShowModal();
        }

        private void OnGUI()
        {
            // 创建一个居中的 GUIStyle
            GUIStyle centeredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter, // 水平和垂直居中对齐
                fontSize = 14, // 设置字体大小
                wordWrap = true  // 启用自动换行
            };

            // 显示消息内容
            EditorGUILayout.LabelField(msg, centeredStyle, GUILayout.Height(115));
            EditorGUILayout.Space(10);

            // 手动选择按钮
            if (defaultOption)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(sure + $"({countdown}秒后自动确认)"))
                    {
                        CloseDialog(true);
                    }

                    if (GUILayout.Button(cancel))
                    {
                        CloseDialog(false);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(sure))
                    {
                        CloseDialog(true);
                    }

                    if (GUILayout.Button(cancel + $"({countdown}秒后自动确认)"))
                    {
                        CloseDialog(false);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            var time = DateTime.Now.Minute * 60 + DateTime.Now.Second;
            // 每帧减少倒计时
            countdown = s - time;

            // 如果倒计时结束，自动选择默认选项
            if (time > s)
            {
                CloseDialog(defaultOption); // 自动选择 "Yes"
            }
            
            Repaint();
        }

        private void CloseDialog(bool result)
        {
            // 调用回调函数并关闭窗口
            onCloseCallback?.Invoke(result);
            window.Close();
        }
    }
}