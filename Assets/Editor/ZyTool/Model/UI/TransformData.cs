using UnityEngine;

namespace ZyTool
{
    public struct TransformData
    {
        public Vector3 position;
        public Vector2 size;
        public Quaternion rotation;
        public Vector3 scale;
        
        public TransformData(Vector3 position, Quaternion rotation, Vector3 scale, Vector2 size)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.size = size;
        }
    }
}