using UnityEngine;

namespace ThanhGiong
{
    /// <summary>Thông tin một đòn đánh.</summary>
    public struct DamageInfo
    {
        public float amount;
        public Vector3 sourcePos;
        public float knockback;
        public bool crit;
        public bool isFire;
    }

    /// <summary>
    /// Lớp cơ sở cho mọi nhân vật (Gióng và giặc): máu, nhận sát thương, bị đẩy lùi,
    /// nháy sáng khi trúng đòn và di chuyển có va chạm.
    /// </summary>
    public abstract class Character : MonoBehaviour
    {
        public float MaxHP { get; protected set; }
        public float HP { get; protected set; }
        public bool IsDead { get; protected set; }
        public float radius = 0.5f;

        protected Transform visual;
        protected Vector3 knockVel;

        Renderer[] renderers;
        Material[] originalMats;
        float flashTimer;

        public abstract void TakeDamage(DamageInfo info);

        protected virtual void Update()
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f) ClearFlash();
            }
        }

        protected void CacheRenderers()
        {
            renderers = visual.GetComponentsInChildren<Renderer>(true);
            originalMats = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) originalMats[i] = renderers[i].sharedMaterial;
        }

        protected void Flash(float duration = 0.08f)
        {
            if (renderers == null) return;
            if (flashTimer <= 0f)
            {
                var white = MatLib.Flash;
                foreach (var r in renderers) if (r != null) r.sharedMaterial = white;
            }
            flashTimer = Mathf.Max(flashTimer, duration);
        }

        protected void ClearFlash()
        {
            flashTimer = 0f;
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].sharedMaterial = originalMats[i];
        }

        /// <summary>Di chuyển có xử lý va chạm với chướng ngại vật và biên đấu trường.</summary>
        protected void MoveBy(Vector3 delta)
        {
            Vector3 p = transform.position + delta;
            p.y = 0f;
            transform.position = Arena.Resolve(p, radius);
        }

        protected void UpdateKnockback(float dt)
        {
            if (knockVel.sqrMagnitude < 0.01f) return;
            MoveBy(knockVel * dt);
            knockVel = Vector3.Lerp(knockVel, Vector3.zero, 1f - Mathf.Exp(-10f * dt));
        }

        protected void FaceDirection(Vector3 dir, float dt, float turnSpeed = 12f)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * dt));
        }

        protected static void SetRotX(Transform t, float x, float dt, float speed = 14f)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(x, 0f, 0f), 1f - Mathf.Exp(-speed * dt));
        }
    }
}
