using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZyTool
{
    public partial class SuperTool
    {
        private bool logFold;
        private LogInfo latestLogMsg = null;
        private GUIStyle logStyle;
        private List<LogInfo> historyLogs = new List<LogInfo>();
        private Vector2 logScroll = Vector2.zero;
        private Vector2 latestLogScroll = Vector2.zero;

        public void WriteLog(string info, Color color)
        {
            latestLogMsg = new LogInfo(info, color);
            historyLogs.Add(latestLogMsg);
        }

        public void WriteLogInfo(string info)
        {
            latestLogMsg = new LogInfo($"[{DateTime.Now.ToLongTimeString()}] [Info] {info}", Color.white);
            historyLogs.Add(latestLogMsg);
            MoveToBottom();
        }

        public void WriteLogWarning(string info)
        {
            latestLogMsg = new LogInfo($"[{DateTime.Now.ToLongTimeString()}] [Warning] {info}", Color.yellow);
            historyLogs.Add(latestLogMsg);
            MoveToBottom();
        }

        public void WriteLogError(string info)
        {
            latestLogMsg = new LogInfo($"[{DateTime.Now.ToLongTimeString()}] [Error] {info}", Color.red);
            historyLogs.Add(latestLogMsg);
            MoveToBottom();
        }

        public void MoveToBottom()
        {
            logScroll = new Vector2(0, 10000);
        }
    }
}