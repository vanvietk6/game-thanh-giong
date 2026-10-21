using UnityEngine;
using UnityEngine.Rendering;

namespace ThanhGiong
{
    /// <summary>Các khớp xương (pivot) của một mô hình nhân vật, dùng để tạo hoạt ảnh bằng code.</summary>
    public class Rig
    {
        public Transform root, body, head, armL, armR, legL, legR, weapon, weaponAlt;
        public Transform horse, rider, horseHead, tail, mouth;
        public Transform[] horseLegs;
    }

    /// <summary>
    /// Nhà máy mô hình: dựng toàn bộ nhân vật và cảnh vật từ các khối cơ bản (cube, sphere, cylinder)
    /// theo phong cách low-poly. Không cần file 3D bên ngoài.
    /// </summary>
    public static class ModelFactory
    {
        // ------------------------------------------------------------------ Tiện ích cơ bản
        public static Transform Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color color,
            Vector3 euler = default, float smooth = 0.15f, float metal = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localEulerAngles = euler;
            t.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MatLib.Get(color, smooth, metal);
            return t;
        }

        public static Transform Cube(Transform parent, Vector3 pos, Vector3 scale, Color color,
            Vector3 euler = default, float smooth = 0.15f, float metal = 0f)
            => Part(parent, PrimitiveType.Cube, pos, scale, color, euler, smooth, metal);

        public static Transform Pivot(Transform parent, Vector3 pos, string name = "Pivot")
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        public static void NoShadow(Transform t)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // ------------------------------------------------------------------ Thánh Gióng + ngựa sắt
        public static Rig BuildGiong(Transform parent)
        {
            var r = new Rig { root = parent };

            // Ngựa sắt
            var horse = Pivot(parent, Vector3.zero, "NguaSat");
            r.horse = horse;
            Cube(horse, new(0, 1.15f, 0), new(0.85f, 0.75f, 1.8f), Palette.Iron, default, 0.5f, 0.6f);
            Cube(horse, new(0, 1.75f, 0.78f), new(0.45f, 0.95f, 0.5f), Palette.Iron, new(35, 0, 0), 0.5f, 0.6f);
            Cube(horse, new(0, 1.95f, 0.55f), new(0.12f, 0.6f, 0.75f), Palette.Fire, new(35, 0, 0)); // bờm lửa
            Cube(horse, new(0, 1.55f, -0.05f), new(0.92f, 0.08f, 0.62f), Palette.Cloth);                // yên ngựa

            r.horseHead = Pivot(horse, new(0, 2.15f, 1.05f), "DauNgua");
            Cube(r.horseHead, new(0, 0, 0.25f), new(0.38f, 0.38f, 0.75f), Palette.Iron, default, 0.5f, 0.6f);
            Cube(r.horseHead, new(0.2f, 0.08f, 0.3f), Vector3.one * 0.09f, Palette.FireYellow);
            Cube(r.horseHead, new(-0.2f, 0.08f, 0.3f), Vector3.one * 0.09f, Palette.FireYellow);
            Cube(r.horseHead, new(0.12f, 0.25f, 0.02f), new(0.08f, 0.2f, 0.08f), Palette.IronDark);
            Cube(r.horseHead, new(-0.12f, 0.25f, 0.02f), new(0.08f, 0.2f, 0.08f), Palette.IronDark);
            r.mouth = Pivot(r.horseHead, new(0, -0.05f, 0.65f), "MiengNgua");

            r.tail = Pivot(horse, new(0, 1.4f, -0.9f), "Duoi");
            Cube(r.tail, new(0, -0.35f, -0.12f), new(0.16f, 0.8f, 0.16f), Palette.Fire, new(-20, 0, 0));

            r.horseLegs = new Transform[4];
            Vector3[] legPos =
            {
                new(0.28f, 0.85f, 0.62f), new(-0.28f, 0.85f, 0.62f),
                new(0.28f, 0.85f, -0.62f), new(-0.28f, 0.85f, -0.62f)
            };
            for (int i = 0; i < 4; i++)
            {
                var p = Pivot(horse, legPos[i], "ChanNgua" + i);
                Cube(p, new(0, -0.42f, 0), new(0.2f, 0.85f, 0.22f), Palette.Iron, default, 0.5f, 0.6f);
                Cube(p, new(0, -0.82f, 0.02f), new(0.25f, 0.1f, 0.3f), Palette.IronDark);
                r.horseLegs[i] = p;
            }

            // Gióng ngồi trên ngựa
            var rider = Pivot(horse, new(0, 1.55f, -0.05f), "Giong");
            r.rider = rider;
            Cube(rider, new(0.42f, -0.05f, 0.08f), new(0.2f, 0.6f, 0.24f), Palette.IronDark, new(0, 0, 15));
            Cube(rider, new(-0.42f, -0.05f, 0.08f), new(0.2f, 0.6f, 0.24f), Palette.IronDark, new(0, 0, -15));

            r.body = Pivot(rider, new(0, 0.1f, 0), "Than");
            Cube(r.body, new(0, 0.4f, 0), new(0.58f, 0.68f, 0.36f), Palette.Iron, default, 0.45f, 0.5f);
            Cube(r.body, new(0, 0.48f, 0.17f), new(0.42f, 0.36f, 0.06f), Palette.Gold, default, 0.5f, 0.6f);
            Cube(r.body, new(0, 0.1f, 0), new(0.6f, 0.1f, 0.4f), Palette.Gold);
            Cube(r.body, new(0.36f, 0.72f, 0), new(0.24f, 0.12f, 0.4f), Palette.Gold);
            Cube(r.body, new(-0.36f, 0.72f, 0), new(0.24f, 0.12f, 0.4f), Palette.Gold);

            r.head = Pivot(r.body, new(0, 0.95f, 0), "Dau");
            Cube(r.head, Vector3.zero, Vector3.one * 0.38f, Palette.Skin);
            Cube(r.head, new(0.09f, 0.03f, 0.19f), new(0.06f, 0.06f, 0.02f), Color.black);
            Cube(r.head, new(-0.09f, 0.03f, 0.19f), new(0.06f, 0.06f, 0.02f), Color.black);
            Cube(r.head, new(0, 0.2f, 0), new(0.44f, 0.12f, 0.44f), Palette.IronDark, default, 0.5f, 0.6f);
            Cube(r.head, new(0, 0.3f, 0), new(0.32f, 0.12f, 0.32f), Palette.Iron, default, 0.5f, 0.6f);
            Cube(r.head, new(0, 0.45f, -0.02f), new(0.08f, 0.22f, 0.08f), Palette.Fire);

            // Tay phải cầm roi sắt
            r.armR = Pivot(r.body, new(0.38f, 0.66f, 0), "TayPhai");
            Cube(r.armR, new(0, -0.27f, 0), new(0.16f, 0.55f, 0.16f), Palette.Iron);
            Cube(r.armR, new(0, -0.57f, 0), Vector3.one * 0.14f, Palette.Skin);
            r.weapon = Pivot(r.armR, new(0, -0.57f, 0), "RoiSat");
            Part(r.weapon, PrimitiveType.Cylinder, new(0, 0, 0.55f), new(0.09f, 0.95f, 0.09f), Palette.IronDark, new(90, 0, 0), 0.5f, 0.7f);
            Cube(r.weapon, new(0, 0, 1.5f), new(0.16f, 0.16f, 0.2f), Palette.Gold);

            // Bụi tre ngà (vũ khí tạm thời)
            r.weaponAlt = Pivot(r.armR, new(0, -0.57f, 0), "TreNga");
            Part(r.weaponAlt, PrimitiveType.Cylinder, new(0, 0, 0.9f), new(0.12f, 1.5f, 0.12f), Palette.BambooGold, new(90, 0, 0));
            for (int i = 0; i < 4; i++)
                Cube(r.weaponAlt, new(0, 0, 0.3f + i * 0.6f), new(0.16f, 0.16f, 0.05f), Palette.BambooGreen);
            Part(r.weaponAlt, PrimitiveType.Sphere, new(0, 0.1f, 2.45f), new(0.8f, 0.35f, 1.0f), Palette.LeafLight);
            r.weaponAlt.gameObject.SetActive(false);

            r.armL = Pivot(r.body, new(-0.38f, 0.66f, 0), "TayTrai");
            Cube(r.armL, new(0, -0.27f, 0), new(0.16f, 0.55f, 0.16f), Palette.Iron);
            Cube(r.armL, new(0, -0.57f, 0), Vector3.one * 0.14f, Palette.Skin);
            return r;
        }

        // ------------------------------------------------------------------ Lính giặc Ân
        public static Rig BuildSoldier(Transform parent, EnemyKind kind)
        {
            Color armor = kind switch
            {
                EnemyKind.Spearman => Palette.EnemyRed,
                EnemyKind.Archer => Palette.ArcherGreen,
                EnemyKind.Shield => Palette.Bronze,
                _ => Palette.EnemyDark
            };
            Color armorDark = armor * 0.75f;
            armorDark.a = 1f;

            var r = new Rig { root = parent };
            r.legL = Pivot(parent, new(-0.13f, 0.8f, 0), "ChanTrai");
            Cube(r.legL, new(0, -0.4f, 0), new(0.2f, 0.8f, 0.22f), Palette.EnemyDark);
            r.legR = Pivot(parent, new(0.13f, 0.8f, 0), "ChanPhai");
            Cube(r.legR, new(0, -0.4f, 0), new(0.2f, 0.8f, 0.22f), Palette.EnemyDark);

            r.body = Pivot(parent, new(0, 0.8f, 0), "Than");
            Cube(r.body, new(0, 0.36f, 0), new(0.55f, 0.7f, 0.32f), armor);
            Cube(r.body, new(0, 0.02f, 0), new(0.58f, 0.12f, 0.35f), Palette.EnemyDark);
            Cube(r.body, new(0, -0.12f, 0), new(0.6f, 0.22f, 0.36f), armorDark);

            r.head = Pivot(r.body, new(0, 0.88f, 0), "Dau");
            Cube(r.head, Vector3.zero, Vector3.one * 0.33f, Palette.EnemySkin);
            Cube(r.head, new(0.08f, 0.02f, 0.165f), new(0.06f, 0.05f, 0.02f), Color.black);
            Cube(r.head, new(-0.08f, 0.02f, 0.165f), new(0.06f, 0.05f, 0.02f), Color.black);
            Cube(r.head, new(0, 0.17f, 0), new(0.38f, 0.14f, 0.38f), Palette.Bronze, default, 0.4f, 0.5f);
            Cube(r.head, new(0, 0.28f, 0), new(0.06f, 0.12f, 0.32f), Palette.EnemyRed);

            r.armL = Pivot(r.body, new(-0.36f, 0.64f, 0), "TayTrai");
            Cube(r.armL, new(0, -0.27f, 0), new(0.15f, 0.55f, 0.15f), armor);
            Cube(r.armL, new(0, -0.56f, 0), Vector3.one * 0.13f, Palette.EnemySkin);
            r.armR = Pivot(r.body, new(0.36f, 0.64f, 0), "TayPhai");
            Cube(r.armR, new(0, -0.27f, 0), new(0.15f, 0.55f, 0.15f), armor);
            Cube(r.armR, new(0, -0.56f, 0), Vector3.one * 0.13f, Palette.EnemySkin);
            r.weapon = Pivot(r.armR, new(0, -0.56f, 0), "VuKhi");

            switch (kind)
            {
                case EnemyKind.Spearman:
                    Part(r.weapon, PrimitiveType.Cylinder, new(0, 0, 0.5f), new(0.05f, 1.1f, 0.05f), Palette.Wood, new(90, 0, 0));
                    Cube(r.weapon, new(0, 0, 1.65f), new(0.1f, 0.1f, 0.3f), Palette.Bronze, default, 0.5f, 0.6f);
                    break;
                case EnemyKind.Archer:
                    var bow = Pivot(r.armL, new(0, -0.56f, 0), "Cung");
                    Cube(bow, new(0, -0.05f, 0), new(0.05f, 0.05f, 0.9f), Palette.Wood);
                    Cube(bow, new(0, 0.05f, 0.55f), new(0.05f, 0.05f, 0.3f), Palette.Wood, new(-30, 0, 0));
                    Cube(bow, new(0, 0.05f, -0.55f), new(0.05f, 0.05f, 0.3f), Palette.Wood, new(30, 0, 0));
                    r.weaponAlt = bow;
                    Cube(r.body, new(0.12f, 0.45f, -0.22f), new(0.15f, 0.55f, 0.15f), Palette.WoodDark, new(0, 0, 20));
                    break;
                case EnemyKind.Shield:
                    var shield = Pivot(r.armL, new(0, -0.6f, 0.05f), "Khien");
                    Cube(shield, Vector3.zero, new(0.75f, 0.08f, 0.95f), Palette.Bronze, default, 0.4f, 0.5f);
                    Cube(shield, new(0, -0.05f, 0), new(0.25f, 0.06f, 0.25f), Palette.EnemyRed);
                    r.weaponAlt = shield;
                    Cube(r.weapon, new(0, 0, 0.45f), new(0.07f, 0.03f, 0.8f), Palette.Stone, default, 0.6f, 0.7f);
                    Cube(r.weapon, new(0, 0, 0.02f), new(0.2f, 0.05f, 0.05f), Palette.Bronze);
                    break;
            }
            return r;
        }

        public static Rig BuildBoss(Transform parent)
        {
            var r = BuildSoldier(parent, EnemyKind.Boss);
            // Sừng, áo choàng, giáp vàng và rìu lớn
            Cube(r.head, new(0.22f, 0.3f, 0), new(0.08f, 0.35f, 0.08f), Palette.Bone, new(0, 0, -30));
            Cube(r.head, new(-0.22f, 0.3f, 0), new(0.08f, 0.35f, 0.08f), Palette.Bone, new(0, 0, 30));
            Cube(r.body, new(0, 0.2f, -0.2f), new(0.62f, 1.0f, 0.06f), Palette.EnemyRed, new(-10, 0, 0));
            Cube(r.body, new(0, 0.45f, 0.17f), new(0.4f, 0.4f, 0.04f), Palette.Gold, default, 0.5f, 0.6f);
            Cube(r.body, new(0.36f, 0.72f, 0), new(0.26f, 0.14f, 0.4f), Palette.Gold);
            Cube(r.body, new(-0.36f, 0.72f, 0), new(0.26f, 0.14f, 0.4f), Palette.Gold);
            Part(r.weapon, PrimitiveType.Cylinder, new(0, 0, 0.6f), new(0.07f, 1.2f, 0.07f), Palette.WoodDark, new(90, 0, 0));
            Cube(r.weapon, new(0, 0.25f, 1.6f), new(0.08f, 0.6f, 0.5f), Palette.Stone, default, 0.6f, 0.7f);
            Cube(r.weapon, new(0, -0.15f, 1.6f), new(0.08f, 0.25f, 0.3f), Palette.Stone, default, 0.6f, 0.7f);
            return r;
        }

        // ------------------------------------------------------------------ Vật phẩm, đạn
        public static Transform BuildRiceBall(Transform parent)
        {
            var m = Pivot(parent, new(0, 0.3f, 0), "NamCom");
            Part(m, PrimitiveType.Sphere, new(0, 0.12f, 0), new(0.5f, 0.42f, 0.5f), Palette.Rice);
            Cube(m, new(0, -0.08f, 0), new(0.6f, 0.08f, 0.6f), Palette.LeafLight, new(0, 45, 0));
            return m;
        }

        public static void BuildArrow(Transform parent)
        {
            Cube(parent, Vector3.zero, new(0.05f, 0.05f, 0.8f), Palette.Wood);
            Cube(parent, new(0, 0, 0.42f), new(0.09f, 0.09f, 0.14f), Palette.Bronze);
            Cube(parent, new(0, 0, -0.38f), new(0.14f, 0.02f, 0.14f), Palette.Rice);
        }

        // ------------------------------------------------------------------ Cảnh vật làng quê
        public static void BuildHouse(Transform parent, Vector3 pos, float yaw)
        {
            var h = Pivot(parent, pos, "NhaTranh");
            h.localEulerAngles = new(0, yaw, 0);
            Cube(h, new(0, 0.15f, 0), new(4.4f, 0.3f, 3.6f), Palette.Stone);
            Cube(h, new(0, 1.2f, 0), new(4f, 2.0f, 3.2f), Palette.Wall);
            Cube(h, new(0, 2.75f, 0), new(4.7f, 2.7f, 2.7f), Palette.Thatch, new(45, 0, 0));
            Cube(h, new(0, 4.66f, 0), new(4.9f, 0.18f, 0.3f), Palette.WoodDark);
            Cube(h, new(0, 1.0f, 1.61f), new(0.9f, 1.6f, 0.05f), Palette.WoodDark);
            Cube(h, new(1.3f, 1.3f, 1.61f), new(0.7f, 0.6f, 0.05f), Palette.WoodDark);
            Cube(h, new(-1.3f, 1.3f, 1.61f), new(0.7f, 0.6f, 0.05f), Palette.WoodDark);
        }

        public static void BuildBambooCluster(Transform parent, Vector3 pos, float scale)
        {
            var c = Pivot(parent, pos, "LuyTre");
            int n = Random.Range(4, 7);
            for (int i = 0; i < n; i++)
            {
                Vector2 off = Random.insideUnitCircle * 0.7f;
                float h = Random.Range(4.5f, 7.5f) * scale;
                Color col = Color.Lerp(Palette.BambooGreen, Palette.BambooGold, Random.value * 0.4f);
                Part(c, PrimitiveType.Cylinder, new(off.x, h * 0.5f, off.y), new(0.14f, h * 0.5f, 0.14f), col,
                    new(Random.Range(-7f, 7f), 0, Random.Range(-7f, 7f)));
            }
            for (int j = 0; j < 3; j++)
            {
                Part(c, PrimitiveType.Sphere,
                    new(Random.Range(-0.8f, 0.8f), Random.Range(4.5f, 6.8f) * scale, Random.Range(-0.8f, 0.8f)),
                    new Vector3(2.0f, 1.4f, 2.0f) * scale * Random.Range(0.8f, 1.2f),
                    Color.Lerp(Palette.Leaf, Palette.LeafLight, Random.value));
            }
        }

        public static void BuildBanyan(Transform parent, Vector3 pos)
        {
            var t = Pivot(parent, pos, "CayDa");
            Part(t, PrimitiveType.Cylinder, new(0, 2f, 0), new(1.2f, 2f, 1.2f), Palette.Wood);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                var root = Pivot(t, Vector3.zero, "Re");
                root.localEulerAngles = new(0, a, 0);
                Cube(root, new(0, 0.3f, 0.8f), new(0.35f, 0.5f, 1.2f), Palette.WoodDark, new(20, 0, 0));
            }
            Part(t, PrimitiveType.Sphere, new(0, 5.2f, 0), new(7f, 3.6f, 7f), Palette.Leaf);
            Part(t, PrimitiveType.Sphere, new(2.2f, 4.6f, 1.2f), new(4.5f, 3f, 4.5f), Palette.LeafLight);
            Part(t, PrimitiveType.Sphere, new(-2f, 4.8f, -1.4f), new(4.5f, 3f, 4.5f), Palette.LeafLight);
            for (int i = 0; i < 6; i++)
            {
                Vector2 p = Random.insideUnitCircle * 2.6f;
                Part(t, PrimitiveType.Cylinder, new(p.x, 3.2f, p.y), new(0.06f, 1.1f, 0.06f), Palette.WoodDark);
            }
        }

        public static void BuildWell(Transform parent, Vector3 pos)
        {
            var w = Pivot(parent, pos, "Gieng");
            Part(w, PrimitiveType.Cylinder, new(0, 0.45f, 0), new(1.7f, 0.45f, 1.7f), Palette.Stone);
            Part(w, PrimitiveType.Cylinder, new(0, 0.91f, 0), new(1.3f, 0.02f, 1.3f), Palette.Water, default, 0.8f);
            Cube(w, new(0.9f, 1.3f, 0), new(0.12f, 1.4f, 0.12f), Palette.Wood);
            Cube(w, new(-0.9f, 1.3f, 0), new(0.12f, 1.4f, 0.12f), Palette.Wood);
            Cube(w, new(0, 2.05f, 0), new(2.2f, 0.12f, 0.12f), Palette.WoodDark);
        }

        public static void BuildGate(Transform parent, Vector3 pos, float yaw)
        {
            var g = Pivot(parent, pos, "CongLang");
            g.localEulerAngles = new(0, yaw, 0);
            Color tile = new(0.55f, 0.25f, 0.15f);
            Cube(g, new(2.4f, 1.9f, 0), new(0.7f, 3.8f, 0.7f), Palette.Wall);
            Cube(g, new(-2.4f, 1.9f, 0), new(0.7f, 3.8f, 0.7f), Palette.Wall);
            Cube(g, new(0, 3.6f, 0), new(5.4f, 0.5f, 0.8f), Palette.Wall);
            Cube(g, new(0, 4.05f, 0), new(6.4f, 0.35f, 1.6f), tile);
            Cube(g, new(0, 4.4f, 0), new(4.6f, 0.35f, 1.1f), tile);
            Cube(g, new(0, 3.6f, 0.42f), new(2.2f, 0.4f, 0.05f), Palette.Gold);
        }

        public static void BuildHaystack(Transform parent, Vector3 pos)
        {
            var h = Pivot(parent, pos, "DongRom");
            Part(h, PrimitiveType.Cylinder, new(0, 0.6f, 0), new(1.4f, 0.6f, 1.4f), Palette.Straw);
            Part(h, PrimitiveType.Sphere, new(0, 1.2f, 0), new(1.4f, 1.1f, 1.4f), Palette.Straw);
            Cube(h, new(0, 1.9f, 0), new(0.08f, 0.6f, 0.08f), Palette.WoodDark);
        }

        public static void BuildRock(Transform parent, Vector3 pos, float size)
        {
            Part(parent, PrimitiveType.Sphere, pos + new Vector3(0, size * 0.25f, 0),
                new Vector3(1.4f, 0.9f, 1.2f) * size, Palette.Stone, new(0, Random.Range(0f, 360f), Random.Range(-10f, 10f)));
        }

        public static void BuildPalm(Transform parent, Vector3 pos)
        {
            var p = Pivot(parent, pos, "CayCau");
            float h = Random.Range(5f, 8f);
            Part(p, PrimitiveType.Cylinder, new(0, h * 0.5f, 0), new(0.25f, h * 0.5f, 0.25f), Palette.Stone);
            for (int i = 0; i < 5; i++)
            {
                var leaf = Pivot(p, new(0, h, 0), "La");
                leaf.localEulerAngles = new(0, i * 72f, 0);
                Cube(leaf, new(0, -0.2f, 0.9f), new(0.4f, 0.08f, 2f), Palette.LeafLight, new(25, 0, 0));
            }
        }

        public static void BuildPaddy(Transform parent, Vector3 pos, float yaw)
        {
            var p = Pivot(parent, pos, "RuongLua");
            p.localEulerAngles = new(0, yaw, 0);
            Cube(p, new(0, 0.03f, 0), new(7f, 0.06f, 5f), new Color(0.38f, 0.52f, 0.35f), default, 0.7f);
            for (int i = 0; i < 10; i++)
                Cube(p, new(-3.15f + i * 0.7f, 0.2f, 0), new(0.25f, 0.35f, 4.6f), new Color(0.48f, 0.74f, 0.28f));
        }
    }
}
