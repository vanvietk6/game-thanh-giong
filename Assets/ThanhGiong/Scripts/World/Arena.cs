using System.Collections.Generic;
using UnityEngine;

namespace ThanhGiong
{
    /// <summary>
    /// Đấu trường hình tròn (làng Phù Đổng) và hệ thống va chạm đơn giản dạng hình tròn trên mặt phẳng XZ.
    /// Không dùng Physics của Unity để dễ kiểm soát và dễ giải thích trong báo cáo.
    /// </summary>
    public static class Arena
    {
        public const float Radius = 27.5f;

        struct Obstacle
        {
            public Vector2 pos;
            public float radius;
        }

        static readonly List<Obstacle> obstacles = new();

        public static void Clear() => obstacles.Clear();

        public static void AddObstacle(Vector3 pos, float radius)
            => obstacles.Add(new Obstacle { pos = new Vector2(pos.x, pos.z), radius = radius });

        /// <summary>Đẩy một hình tròn (vị trí + bán kính) ra khỏi chướng ngại vật và giữ trong đấu trường.</summary>
        public static Vector3 Resolve(Vector3 pos, float radius)
        {
            Vector2 p = new(pos.x, pos.z);
            foreach (var o in obstacles)
            {
                Vector2 d = p - o.pos;
                float min = o.radius + radius;
                float sq = d.sqrMagnitude;
                if (sq >= min * min) continue;
                float m = Mathf.Sqrt(sq);
                Vector2 n = m > 0.0001f ? d / m : Vector2.right;
                p = o.pos + n * min;
            }

            float limit = Radius - radius;
            if (p.sqrMagnitude > limit * limit) p = p.normalized * limit;
            return new Vector3(p.x, pos.y, p.y);
        }

        /// <summary>Điểm có nằm trong một chướng ngại vật không (dùng cho mũi tên).</summary>
        public static bool IsBlocked(Vector3 pos, float radius = 0f)
        {
            Vector2 p = new(pos.x, pos.z);
            foreach (var o in obstacles)
            {
                float r = o.radius + radius;
                if ((p - o.pos).sqrMagnitude < r * r) return true;
            }
            return false;
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
