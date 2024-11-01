using System;
using UnityEditor;
using UnityEngine;

namespace ZyTool
{
    public class LogWindow : EditorWindow
    {
        private ZyTool rootTool;
        private static LogWindow window;

        private Vector2 logScroll = Vector2.zero;
        private Action closeAction;

        public static void OpenWindow(ZyTool r, Action closeAction = null)
        {
            window = GetWindow<LogWindow>();
            window.minSize = new Vector2(600, 500);
            window.Show();
            window.Focus();
            window.rootTool = r;
            window.closeAction = closeAction;

            window.MoveToBottom();
        }

        private void OnDisable()
        {
            closeAction?.Invoke();
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("滑动到底部"))
                {
                    MoveToBottom();
                }

                if (GUILayout.Button("清空"))
                {
                    rootTool.historyLogs.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            logScroll = EditorGUILayout.BeginScrollView(logScroll);
            {
                for (int i = 0; i < rootTool.historyLogs.Count; i++)
                {
                    EditorGUILayout.LabelField(rootTool.historyLogs[i].info, rootTool.historyLogs[i].style, GUILayout.Width(rootTool.historyLogs[i].info.Length * 10));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void MoveToBottom()
        {
            logScroll = new Vector2(0, 10000);
        }
        
        
    }
}