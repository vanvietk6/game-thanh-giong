using System.Collections.Generic;
using UnityEngine;

namespace ThanhGiong
{
    /// <summary>
    /// Quản lý các đợt giặc: định nghĩa số lượng từng loại lính mỗi đợt, sinh lính từ các cửa làng
    /// (tránh xa vị trí Gióng), giới hạn số lính cùng lúc và báo cho GameManager khi hết đợt.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        struct WaveDef
        {
            public int spear, archer, shield;
            public bool boss;
            public float interval;
            public int maxAlive;
            public string title, subtitle;
        }

        static readonly WaveDef[] Defs =
        {
            new() { spear = 8, interval = 1.1f, maxAlive = 6,
                    title = "Đợt 1: Giặc Ân kéo đến", subtitle = "Vung roi sắt đánh tan quân tiên phong!" },
            new() { spear = 10, archer = 4, interval = 1.0f, maxAlive = 8,
                    title = "Đợt 2: Cung thủ giặc", subtitle = "Lướt (Space) để né tên, áp sát cung thủ!" },
            new() { spear = 10, archer = 5, shield = 3, interval = 0.9f, maxAlive = 9,
                    title = "Đợt 3: Lính khiên", subtitle = "Đánh sau lưng hoặc phun lửa (K) để phá khiên!" },
            new() { spear = 14, archer = 7, shield = 5, interval = 0.8f, maxAlive = 11,
                    title = "Đợt 4: Giặc tràn khắp nơi", subtitle = "Nhổ tre ngà (E) để quét sạch quân thù!" },
            new() { spear = 6, archer = 3, boss = true, interval = 1.5f, maxAlive = 8,
                    title = "Đợt cuối: Tướng giặc Ân", subtitle = "Tránh khỏi vùng đỏ — đánh bại tướng giặc!" },
        };

        public static int TotalWaves => Defs.Length;
        public int CurrentWave { get; private set; }
        public bool Active { get; private set; }
        public BossEnemy Boss { get; private set; }
        public int Remaining => queue.Count + GameManager.I.Enemies.Count + (bossPending ? 1 : 0);

        readonly List<EnemyKind> queue = new();
        WaveDef def;
        float spawnTimer, introTimer;
        bool bossPending;

        public void BeginWave(int number)
        {
            CurrentWave = number;
            def = Defs[Mathf.Clamp(number - 1, 0, Defs.Length - 1)];
            queue.Clear();
            for (int i = 0; i < def.spear; i++) queue.Add(EnemyKind.Spearman);
            for (int i = 0; i < def.archer; i++) queue.Add(EnemyKind.Archer);
            for (int i = 0; i < def.shield; i++) queue.Add(EnemyKind.Shield);
            for (int i = queue.Count - 1; i > 0; i--) // xáo trộn (Fisher–Yates)
            {
                int j = Random.Range(0, i + 1);
                (queue[i], queue[j]) = (queue[j], queue[i]);
            }
            bossPending = def.boss;
            Boss = null;
            introTimer = 2.5f;
            spawnTimer = 0f;
            Active = true;
            GameManager.I.UI.ShowBanner(def.title, def.subtitle, 3f);
            GameManager.I.Audio.Play(Sfx.Gong, 0.9f, 0f);
            GameManager.I.Log($"Bắt đầu đợt {number}: {queue.Count} lính{(def.boss ? " + tướng giặc" : "")}");
        }

        public void StopAll()
        {
            Active = false;
            queue.Clear();
            bossPending = false;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (!Active || gm == null || gm.State != GameState.Playing) return;
            float dt = Time.deltaTime;
            if (introTimer > 0f) { introTimer -= dt; return; }

            if (bossPending)
            {
                bossPending = false;
                Boss = (BossEnemy)SpawnEnemy(EnemyKind.Boss, WorldBuilder.Polar(45f, 24f));
                gm.Cam.Shake(0.6f, 0.8f);
                gm.Audio.Play(Sfx.Gong, 1f, 0f);
            }

            spawnTimer -= dt;
            if (queue.Count > 0 && spawnTimer <= 0f && gm.Enemies.Count < def.maxAlive)
            {
                var kind = queue[queue.Count - 1];
                queue.RemoveAt(queue.Count - 1);
                SpawnEnemy(kind, PickSpawnPoint());
                spawnTimer = def.interval;
            }

            if (queue.Count == 0 && gm.Enemies.Count == 0)
            {
                Active = false;
                gm.OnWaveCleared(CurrentWave);
            }
        }

        public Enemy SpawnEnemy(EnemyKind kind, Vector3 pos)
        {
            var go = new GameObject("Giac_" + kind);
            go.transform.SetParent(GameManager.I.RuntimeRoot, false);
            go.transform.position = pos;
            Vector3 look = -pos;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(look);

            Enemy e = kind switch
            {
                EnemyKind.Spearman => go.AddComponent<SpearmanEnemy>(),
                EnemyKind.Archer => go.AddComponent<ArcherEnemy>(),
                EnemyKind.Shield => go.AddComponent<ShieldEnemy>(),
                _ => go.AddComponent<BossEnemy>()
            };
            float hpMul = 1f + 0.15f * (CurrentWave - 1);
            float dmgMul = 1f + 0.10f * (CurrentWave - 1);
            e.Setup(hpMul, dmgMul);
            return e;
        }

        Vector3 PickSpawnPoint()
        {
            Vector3 player = GameManager.I.Player.transform.position;
            var gates = WorldBuilder.GateAngles;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float a = gates[Random.Range(0, gates.Length)] + Random.Range(-3f, 3f);
                Vector3 p = WorldBuilder.Polar(a, 26.5f);
                if (Arena.FlatDistance(p, player) > 10f) return p;
            }
            return WorldBuilder.Polar(gates[Random.Range(0, gates.Length)], 26.5f);
        }
    }
}
