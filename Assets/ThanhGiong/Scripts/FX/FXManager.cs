using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThanhGiong
{
    /// <summary>
    /// Hiệu ứng hình ảnh: hệ hạt tự viết (dùng object pool), số sát thương, vùng cảnh báo,
    /// vệt chém và "hitstop" (khựng hình khi đánh trúng để tạo cảm giác lực).
    /// </summary>
    public class FXManager : MonoBehaviour
    {
        class Particle
        {
            public Transform t;
            public Renderer r;
            public Vector3 vel;
            public float life, maxLife, size, gravity, drag;
        }

        const int MaxParticles = 700;
        readonly List<Particle> active = new();
        readonly Stack<Particle> free = new();
        Transform root;
        float hitstopTimer;

        static GameManager GM => GameManager.I;

        void Awake() => root = new GameObject("FX_Particles").transform;

        Particle Get()
        {
            if (free.Count > 0)
            {
                var p = free.Pop();
                p.t.gameObject.SetActive(true);
                return p;
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root, false);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return new Particle { t = go.transform, r = r };
        }

        public void Spawn(Vector3 pos, Vector3 vel, Color color, float size, float life, float gravity = -9f, float drag = 1f)
        {
            if (active.Count >= MaxParticles) return;
            var p = Get();
            p.t.SetPositionAndRotation(pos, Random.rotation);
            p.t.localScale = Vector3.one * size;
            p.r.sharedMaterial = MatLib.Get(color, 0f);
            p.vel = vel;
            p.life = p.maxLife = Mathf.Max(0.05f, life);
            p.size = size;
            p.gravity = gravity;
            p.drag = drag;
            active.Add(p);
        }

        public void Burst(Vector3 pos, Color color, int count, float speed, float size = 0.18f, float life = 0.5f,
            float gravity = -12f, float upBias = 0.6f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) + upBias;
                Spawn(pos, dir.normalized * speed * Random.Range(0.5f, 1f), color,
                    size * Random.Range(0.7f, 1.3f), life * Random.Range(0.7f, 1.2f), gravity);
            }
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].t.gameObject.SetActive(false);
                free.Push(active[i]);
            }
            active.Clear();
            hitstopTimer = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.t.gameObject.SetActive(false);
                    free.Push(p);
                    active[i] = active[active.Count - 1];
                    active.RemoveAt(active.Count - 1);
                    continue;
                }
                p.vel.y += p.gravity * dt;
                p.vel *= 1f / (1f + p.drag * dt);
                Vector3 pos = p.t.position + p.vel * dt;
                if (pos.y < 0.05f && p.gravity < 0f)
                {
                    pos.y = 0.05f;
                    p.vel = new Vector3(p.vel.x * 0.7f, -p.vel.y * 0.3f, p.vel.z * 0.7f);
                }
                p.t.position = pos;
                p.t.localScale = Vector3.one * (p.size * (p.life / p.maxLife));
            }

            // Hitstop: làm chậm thời gian trong tích tắc rồi trả lại bình thường
            if (hitstopTimer > 0f && GM != null)
            {
                hitstopTimer -= Time.unscaledDeltaTime;
                if (GM.State == GameState.Playing) Time.timeScale = hitstopTimer > 0f ? 0.08f : GM.BaseTimeScale;
                else hitstopTimer = 0f;
            }
        }

        public void Hitstop(float duration)
        {
            if (GM.State != GameState.Playing) return;
            hitstopTimer = Mathf.Max(hitstopTimer, duration);
            Time.timeScale = 0.08f;
        }

        public void DamageNumber(Vector3 worldPos, float amount, Color color, float scale = 1f)
        {
            GM.UI.FloatText(worldPos, Mathf.Max(1, Mathf.RoundToInt(amount)).ToString(), color, scale);
        }

        // ------------------------------------------------------------------ Vùng cảnh báo đòn đánh
        public void Telegraph(Vector3 pos, float radius, float duration)
        {
            var go = new GameObject("CanhBao");
            go.transform.SetParent(GM.RuntimeRoot, false);
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            ModelFactory.Part(go.transform, PrimitiveType.Cylinder, new(0, 0.06f, 0), new(radius * 2f, 0.01f, radius * 2f), Palette.TelegraphDark, default, 0f);
            var inner = ModelFactory.Part(go.transform, PrimitiveType.Cylinder, new(0, 0.08f, 0), new(0.01f, 0.01f, 0.01f), Palette.Telegraph, default, 0f);
            ModelFactory.NoShadow(go.transform);
            go.AddComponent<TelegraphFX>().Init(inner, new Vector3(0.01f, 0.01f, 0.01f), new Vector3(radius * 2f, 0.01f, radius * 2f), duration);
        }

        public void LineTelegraph(Vector3 start, Vector3 dir, float length, float width, float duration)
        {
            var go = new GameObject("CanhBaoThang");
            go.transform.SetParent(GM.RuntimeRoot, false);
            go.transform.SetPositionAndRotation(new Vector3(start.x, 0f, start.z), Quaternion.LookRotation(dir));
            ModelFactory.Cube(go.transform, new(0, 0.06f, length * 0.5f), new(width, 0.02f, length), Palette.TelegraphDark, default, 0f);
            var grow = ModelFactory.Pivot(go.transform, Vector3.zero, "Grow");
            ModelFactory.Cube(grow, new(0, 0.08f, 0.5f), new(width * 0.85f, 0.02f, 1f), Palette.Telegraph, default, 0f);
            ModelFactory.NoShadow(go.transform);
            go.AddComponent<TelegraphFX>().Init(grow, new Vector3(1f, 1f, 0.01f), new Vector3(1f, 1f, length), duration);
        }

        // ------------------------------------------------------------------ Vệt chém
        public void Slash(Transform owner, float range, float arc, float height, int sign, Color color)
        {
            var go = new GameObject("VetChem");
            go.transform.SetParent(GM.RuntimeRoot, false);
            go.transform.SetPositionAndRotation(owner.position + Vector3.up * height, Quaternion.Euler(0f, owner.eulerAngles.y, 0f));
            go.AddComponent<SlashFX>().Init(range, arc, sign, color);
        }
    }

    /// <summary>Phóng to dần một phần của vùng cảnh báo cho tới khi đòn đánh nổ ra.</summary>
    public class TelegraphFX : MonoBehaviour
    {
        Transform grow;
        Vector3 from, to;
        float duration, time;

        public void Init(Transform growPart, Vector3 fromScale, Vector3 toScale, float dur)
        {
            grow = growPart;
            from = fromScale;
            to = toScale;
            duration = Mathf.Max(0.05f, dur);
            grow.localScale = from;
        }

        void Update()
        {
            time += Time.deltaTime;
            grow.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(time / duration));
            if (time >= duration) Destroy(gameObject);
        }
    }

    /// <summary>Vệt chém hình cung: các mảnh nhỏ xuất hiện lần lượt theo chiều vung rồi thu nhỏ dần.</summary>
    public class SlashFX : MonoBehaviour
    {
        const float Duration = 0.22f;
        Transform[] pieces;
        Vector3[] baseScale;
        float time;

        public void Init(float range, float arc, int sign, Color color)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(arc / 12f), 6, 30);
            pieces = new Transform[n];
            baseScale = new Vector3[n];
            float r = range * 0.8f;
            float step = arc / (n - 1);
            float segLen = 2f * r * Mathf.Sin(step * 0.5f * Mathf.Deg2Rad) * 1.25f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(arc * 0.5f, -arc * 0.5f, i / (n - 1f)) * sign;
                var pivot = ModelFactory.Pivot(transform, Vector3.zero, "Manh");
                pivot.localRotation = Quaternion.Euler(0f, a, 0f);
                var piece = ModelFactory.Cube(pivot, new(0f, 0f, r), new(segLen, 0.04f, range * 0.14f), color, default, 0f);
                ModelFactory.NoShadow(piece);
                pieces[i] = piece;
                baseScale[i] = piece.localScale;
                piece.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            time += Time.deltaTime;
            int n = pieces.Length;
            float life = Duration * 0.6f;
            for (int i = 0; i < n; i++)
            {
                float age = time - (i / (n - 1f)) * Duration * 0.45f;
                bool visible = age >= 0f && age < life;
                pieces[i].gameObject.SetActive(visible);
                if (visible)
                {
                    float k = 1f - age / life;
                    pieces[i].localScale = new Vector3(baseScale[i].x, baseScale[i].y, baseScale[i].z * k);
                }
            }
            if (time > Duration * 1.2f) Destroy(gameObject);
        }
    }
}
