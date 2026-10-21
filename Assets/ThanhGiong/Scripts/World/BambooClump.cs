using System.Collections.Generic;
using UnityEngine;

namespace ThanhGiong
{
    /// <summary>
    /// Bụi tre ngà trong làng. Gióng có thể nhổ tre làm vũ khí (theo truyền thuyết: roi sắt gãy,
    /// Gióng nhổ bụi tre bên đường quật vào giặc). Tre mọc lại sau một thời gian.
    /// </summary>
    public class BambooClump : MonoBehaviour
    {
        public static readonly List<BambooClump> All = new();
        const float RegrowTime = 25f;

        Transform stalks;
        GameObject marker;
        float regrowTimer;

        public bool Available => regrowTimer <= 0f;

        public void Build()
        {
            stalks = ModelFactory.Pivot(transform, Vector3.zero, "Than");
            for (int i = 0; i < 5; i++)
            {
                Vector2 off = Random.insideUnitCircle * 0.45f;
                float h = Random.Range(3.4f, 4.6f);
                ModelFactory.Part(stalks, PrimitiveType.Cylinder, new(off.x, h * 0.5f, off.y), new(0.16f, h * 0.5f, 0.16f),
                    Palette.BambooGold, new(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f)));
            }
            ModelFactory.Part(stalks, PrimitiveType.Sphere, new(0, 3.9f, 0), new(1.8f, 1.2f, 1.8f), Palette.LeafLight);
            ModelFactory.Part(stalks, PrimitiveType.Sphere, new(0.4f, 3.3f, 0.3f), new(1.3f, 0.9f, 1.3f), Palette.Leaf);

            var ring = ModelFactory.Part(transform, PrimitiveType.Cylinder, new(0, 0.05f, 0), new(1.7f, 0.01f, 1.7f), new Color(0.85f, 0.75f, 0.35f));
            ModelFactory.NoShadow(ring);
            marker = ring.gameObject;
            All.Add(this);
        }

        void OnDestroy() => All.Remove(this);

        public bool TryPull()
        {
            if (!Available) return false;
            regrowTimer = RegrowTime;
            stalks.localScale = new Vector3(1f, 0.12f, 1f);
            marker.SetActive(false);
            GameManager.I.FX.Burst(transform.position + Vector3.up, Palette.BambooGreen, 25, 6f, 0.25f, 0.7f);
            return true;
        }

        public void ResetClump()
        {
            regrowTimer = 0f;
            stalks.localScale = Vector3.one;
            marker.SetActive(true);
        }

        void Update()
        {
            if (regrowTimer <= 0f) return;
            regrowTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(regrowTimer / RegrowTime);
            stalks.localScale = new Vector3(1f, Mathf.Lerp(0.12f, 1f, t), 1f);
            if (regrowTimer <= 0f) ResetClump();
        }
    }
}
