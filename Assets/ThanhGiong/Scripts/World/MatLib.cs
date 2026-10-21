using System.Collections.Generic;
using UnityEngine;

namespace ThanhGiong
{
    /// <summary>Bảng màu dùng chung cho toàn bộ game (phong cách low-poly).</summary>
    public static class Palette
    {
        public static readonly Color Iron = new(0.30f, 0.32f, 0.36f);
        public static readonly Color IronDark = new(0.16f, 0.17f, 0.20f);
        public static readonly Color Gold = new(0.92f, 0.70f, 0.22f);
        public static readonly Color Fire = new(1.00f, 0.45f, 0.08f);
        public static readonly Color FireYellow = new(1.00f, 0.85f, 0.30f);
        public static readonly Color FireRed = new(0.90f, 0.18f, 0.05f);
        public static readonly Color Skin = new(0.93f, 0.74f, 0.58f);
        public static readonly Color EnemySkin = new(0.80f, 0.60f, 0.46f);
        public static readonly Color Bronze = new(0.62f, 0.45f, 0.20f);
        public static readonly Color EnemyRed = new(0.55f, 0.12f, 0.10f);
        public static readonly Color EnemyDark = new(0.18f, 0.12f, 0.10f);
        public static readonly Color ArcherGreen = new(0.33f, 0.36f, 0.18f);
        public static readonly Color Cloth = new(0.72f, 0.16f, 0.10f);
        public static readonly Color Bone = new(0.90f, 0.86f, 0.72f);
        public static readonly Color Wood = new(0.45f, 0.30f, 0.15f);
        public static readonly Color WoodDark = new(0.28f, 0.18f, 0.10f);
        public static readonly Color BambooGreen = new(0.42f, 0.66f, 0.24f);
        public static readonly Color BambooGold = new(0.80f, 0.80f, 0.35f);
        public static readonly Color BambooLight = new(0.65f, 0.95f, 0.40f);
        public static readonly Color Leaf = new(0.22f, 0.48f, 0.18f);
        public static readonly Color LeafLight = new(0.35f, 0.62f, 0.22f);
        public static readonly Color Grass = new(0.40f, 0.60f, 0.27f);
        public static readonly Color GrassDark = new(0.32f, 0.50f, 0.22f);
        public static readonly Color Dirt = new(0.62f, 0.50f, 0.33f);
        public static readonly Color DirtLight = new(0.70f, 0.58f, 0.40f);
        public static readonly Color Thatch = new(0.74f, 0.58f, 0.28f);
        public static readonly Color Wall = new(0.86f, 0.77f, 0.58f);
        public static readonly Color Stone = new(0.55f, 0.55f, 0.52f);
        public static readonly Color Water = new(0.30f, 0.45f, 0.50f);
        public static readonly Color Rice = new(0.97f, 0.97f, 0.92f);
        public static readonly Color Straw = new(0.86f, 0.73f, 0.36f);
        public static readonly Color Telegraph = new(0.95f, 0.12f, 0.05f);
        public static readonly Color TelegraphDark = new(0.40f, 0.06f, 0.04f);
        public static readonly Color SlashWhite = new(1.00f, 0.95f, 0.80f);
        public static readonly Color Hurt = new(0.90f, 0.15f, 0.10f);
        public static readonly Color BarBg = new(0.10f, 0.05f, 0.05f);
        public static readonly Color BarRed = new(0.90f, 0.20f, 0.12f);
    }

    /// <summary>
    /// Thư viện vật liệu: tạo Material theo màu từ shader mặc định của URP và tái sử dụng (cache)
    /// để giảm số lượng Material, giúp SRP Batcher gộp lệnh vẽ.
    /// </summary>
    public static class MatLib
    {
        public const string BaseMaterialName = "TG_BaseLit";
        static Material template;
        static readonly Dictionary<(Color32, byte, byte), Material> cache = new();
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int SmoothId = Shader.PropertyToID("_Smoothness");
        static readonly int MetalId = Shader.PropertyToID("_Metallic");

        static Material Template
        {
            get
            {
                if (template == null)
                {
                    // Material gốc nằm trong Resources để shader URP Lit luôn được đóng gói khi build.
                    var asset = Resources.Load<Material>(BaseMaterialName);
                    if (asset != null) template = new Material(asset);
                    else
                    {
                        // Dự phòng: lấy material mặc định của primitive (chạy được trong Editor).
                        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        template = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
                        Object.Destroy(tmp);
                    }
                }
                return template;
            }
        }

        public static Material Get(Color color, float smoothness = 0.15f, float metallic = 0f)
        {
            var key = ((Color32)color, (byte)(smoothness * 255f), (byte)(metallic * 255f));
            if (cache.TryGetValue(key, out var m) && m != null) return m;

            m = new Material(Template) { name = "TG_" + ColorUtility.ToHtmlStringRGB(color) };
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, color);
            if (m.HasProperty(SmoothId)) m.SetFloat(SmoothId, smoothness);
            if (m.HasProperty(MetalId)) m.SetFloat(MetalId, metallic);
            m.enableInstancing = true;
            cache[key] = m;
            return m;
        }

        /// <summary>Vật liệu trắng dùng để nháy sáng khi trúng đòn.</summary>
        public static Material Flash => Get(Color.white, 0f);
    }
}
