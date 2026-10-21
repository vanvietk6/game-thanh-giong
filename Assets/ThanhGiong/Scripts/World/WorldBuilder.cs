using UnityEngine;
using UnityEngine.Rendering;

namespace ThanhGiong
{
    /// <summary>
    /// Dựng bối cảnh làng Phù Đổng: sân đình, lũy tre làng bao quanh, nhà tranh, cây đa, giếng nước,
    /// cổng làng, ruộng lúa và núi Sóc Sơn ở phía xa.
    /// </summary>
    public static class WorldBuilder
    {
        /// <summary>Các góc (độ) của cửa làng — nơi giặc tràn vào.</summary>
        public static readonly float[] GateAngles = { 45f, 105f, 165f, 225f, 285f, 345f };
        static readonly float[] HouseAngles = { 15f, 75f, 135f, 195f, 255f, 315f };

        public static Vector3 Polar(float deg, float r)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        static float YawToward(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        public static void Build(Transform root)
        {
            Arena.Clear();
            BambooClump.All.Clear();
            var oldState = Random.state;
            Random.InitState(20250);

            // Cảnh tĩnh: gộp lại bằng static batching để vẽ nhanh hơn
            var stat = new GameObject("CanhTinh").transform;
            stat.SetParent(root, false);
            BuildGround(stat);
            BuildBambooRing(stat);
            BuildVillage(stat);
            BuildScenery(stat);
            StaticBatchingUtility.Combine(stat.gameObject);

            // Bụi tre có thể nhổ (không gộp vì có hoạt ảnh)
            foreach (float a in HouseAngles)
            {
                Vector3 pos = Polar(a, 12.5f);
                var go = new GameObject("BuiTreNga");
                go.transform.SetParent(root, false);
                go.transform.position = pos;
                go.AddComponent<BambooClump>().Build();
                Arena.AddObstacle(pos, 0.7f);
            }

            Random.state = oldState;
        }

        static void BuildGround(Transform stat)
        {
            var ground = ModelFactory.Cube(stat, new(0, -0.5f, 0), new(400f, 1f, 400f), Palette.GrassDark);
            var arena = ModelFactory.Part(stat, PrimitiveType.Cylinder, Vector3.zero, new(60f, 0.02f, 60f), Palette.Grass);
            var plaza = ModelFactory.Part(stat, PrimitiveType.Cylinder, new(0, 0.02f, 0), new(15f, 0.02f, 15f), Palette.Dirt);
            foreach (var t in new[] { ground, arena, plaza })
                t.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            foreach (float a in GateAngles)
            {
                Vector3 c = Polar(a, 18f);
                var path = ModelFactory.Cube(stat, new(c.x, 0.015f, c.z), new(2.6f, 0.03f, 23f), Palette.DirtLight,
                    new(0, 90f - a, 0));
                path.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static bool NearGate(float angle, float tolerance)
        {
            foreach (float g in GateAngles)
                if (Mathf.Abs(Mathf.DeltaAngle(angle, g)) < tolerance) return true;
            return false;
        }

        static void BuildBambooRing(Transform stat)
        {
            for (float a = 0f; a < 360f; a += 3.2f)
            {
                if (NearGate(a, 5f)) continue;
                for (int layer = 0; layer < 2; layer++)
                {
                    float r = 30.5f + layer * 2.6f + Random.Range(-0.6f, 0.6f);
                    float aa = a + Random.Range(-1f, 1f) + layer * 1.6f;
                    ModelFactory.BuildBambooCluster(stat, Polar(aa, r), Random.Range(0.85f, 1.2f));
                }
            }

            // Cổng làng ở cửa chính (phía sau màn hình)
            Vector3 gatePos = Polar(45f, 30f);
            ModelFactory.BuildGate(stat, gatePos, YawToward(gatePos, Vector3.zero));
        }

        static void BuildVillage(Transform stat)
        {
            foreach (float a in HouseAngles)
            {
                Vector3 pos = Polar(a, 23f);
                ModelFactory.BuildHouse(stat, pos, YawToward(pos, Vector3.zero));
                Arena.AddObstacle(pos, 2.5f);

                Vector3 hay = pos + Polar(a + 90f, 3.8f);
                ModelFactory.BuildHaystack(stat, hay);
                Arena.AddObstacle(hay, 1.1f);
            }

            Vector3 banyan = Polar(0f, 17f);
            ModelFactory.BuildBanyan(stat, banyan);
            Arena.AddObstacle(banyan, 1.4f);

            Vector3 well = Polar(195f, 8f);
            ModelFactory.BuildWell(stat, well);
            Arena.AddObstacle(well, 1.0f);

            (float a, float r, float s)[] rocks = { (55f, 17f, 1.1f), (140f, 17.5f, 0.9f), (235f, 18.5f, 1.2f), (295f, 16f, 0.8f), (330f, 9.5f, 0.9f) };
            foreach (var rk in rocks)
            {
                Vector3 p = Polar(rk.a, rk.r);
                ModelFactory.BuildRock(stat, p, rk.s);
                Arena.AddObstacle(p, 0.75f * rk.s);
            }
        }

        static void BuildScenery(Transform stat)
        {
            // Núi non phía xa — núi Sóc Sơn lớn nhất ở hướng cổng làng
            (float a, float d, float w, float h)[] mountains =
            {
                (45f, 95f, 50f, 42f), (20f, 115f, 45f, 28f), (75f, 108f, 42f, 30f), (0f, 135f, 60f, 30f),
                (100f, 125f, 55f, 26f), (-30f, 140f, 60f, 25f), (140f, 150f, 70f, 30f), (200f, 150f, 60f, 24f), (260f, 140f, 60f, 25f)
            };
            for (int i = 0; i < mountains.Length; i++)
            {
                var m = mountains[i];
                Vector3 p = Polar(m.a, m.d);
                Color c = Color.Lerp(new Color(0.33f, 0.47f, 0.36f), new Color(0.45f, 0.55f, 0.55f), (i % 3) / 2f);
                var t = ModelFactory.Part(stat, PrimitiveType.Sphere, new(p.x, -m.h * 0.2f, p.z), new(m.w, m.h, m.w), c);
                t.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            // Ruộng lúa và hàng cau ngoài lũy tre
            for (int i = 0; i < 14; i++)
            {
                float a = Random.Range(0f, 360f);
                float r = Random.Range(40f, 58f);
                ModelFactory.BuildPaddy(stat, Polar(a, r), -a + Random.Range(-10f, 10f));
            }
            for (int i = 0; i < 26; i++)
                ModelFactory.BuildPalm(stat, Polar(Random.Range(0f, 360f), Random.Range(37f, 62f)));
        }
    }
}
