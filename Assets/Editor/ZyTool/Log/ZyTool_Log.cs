using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZyTool
{
    public partial class ZyTool
    {
        private bool isOpenLogWindow;
        private bool logFold;
        private LogInfo latestLogMsg = null;
        public GUIStyle logStyle;
        public readonly List<LogInfo> historyLogs = new List<LogInfo>();
        private Vector2 latestLogScroll = Vector2.zero;

        public void PrintLog(string info, Color color)
        {
            latestLogMsg = new LogInfo(info, color);
            historyLogs.Add(latestLogMsg);
        }

        public void PrintLogInfo(string info)
        {
            latestLogMsg = new LogInfo($"[{DateTime.Now.ToLongTimeString()}] [Info] {info}", Color.white);
            historyLogs.Add(latestLogMsg);
            if (isOpenLogWindow)
            {
                LogWindow.OpenWindow(this, () => isOpenLogWindow = false);
            }
        }

        public void PrintLogWarning(string info)
        {
            latestLogMsg = new LogInfo($"[{DateTime.Now.ToLongTimeString()}] [Warning] {info}", Color.yellow);
            historyLogs.Add(latestLogMsg);
            if (isOpenLogWindow)
            {
                LogWindow.OpenWindow(this, () => isOpenLogWindow = false);
            }
        }

        public void PrintLogError(string info)
        {
            latestLogMsg = new LogInfo($"[{DateTime.Now.ToLongTimeString()}] [Error] {info}", Color.red);
            historyLogs.Add(latestLogMsg);
            if (isOpenLogWindow)
            {
                LogWindow.OpenWindow(this, () => isOpenLogWindow = false);
            }
        }
    }
}