using System;
using UnityEngine;

namespace ZyTool
{
    public class LogInfo
    {
        public string info;
        public DateTime time;
        public GUIStyle style;

        public LogInfo(string info, Color color, DateTime time = default)
        {
            this.info = info;
            style = new GUIStyle
            {
                normal =
                {
                    textColor = color
                }
            };
            this.time = time == default ? DateTime.Now : time;
        }
    }
}