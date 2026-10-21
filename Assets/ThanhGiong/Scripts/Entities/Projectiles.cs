using UnityEngine;

namespace ThanhGiong
{
    /// <summary>Mũi tên do cung thủ giặc bắn ra. Gióng có thể lướt (Space) để né.</summary>
    public class Arrow : MonoBehaviour
    {
        Vector3 velocity;
        float damage;
        float life = 3f;

        public static void Spawn(Vector3 pos, Vector3 dir, float speed, float damage)
        {
            var go = new GameObject("MuiTen");
            go.transform.SetParent(GameManager.I.RuntimeRoot, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
            ModelFactory.BuildArrow(go.transform);
            var a = go.AddComponent<Arrow>();
            a.velocity = dir * speed;
            a.damage = damage;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            transform.position += velocity * dt;
            life -= dt;
            Vector3 pos = transform.position;

            var p = GameManager.I.Player;
            if (p != null && !p.IsDead && !p.IsDashing && Arena.FlatDistance(p.transform.position, pos) < p.radius + 0.25f)
            {
                p.TakeDamage(new DamageInfo { amount = damage, sourcePos = pos - velocity.normalized, knockback = 4f });
                Destroy(gameObject);
                return;
            }

            if (life <= 0f || Arena.IsBlocked(pos) || new Vector2(pos.x, pos.z).magnitude > Arena.Radius + 3f)
            {
                GameManager.I.FX.Burst(pos, Palette.Wood, 4, 3f, 0.1f, 0.3f);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Nắm cơm làng Gióng — hồi máu khi nhặt (dân làng góp gạo nuôi Gióng).</summary>
    public class RicePickup : MonoBehaviour
    {
        const float Heal = 25f;
        float life = 14f, time;
        Transform model;

        public static void Spawn(Vector3 pos)
        {
            var go = new GameObject("NamCom");
            go.transform.SetParent(GameManager.I.RuntimeRoot, false);
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            var p = go.AddComponent<RicePickup>();
            p.model = ModelFactory.BuildRiceBall(go.transform);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            time += dt;
            life -= dt;
            model.localPosition = new Vector3(0f, 0.35f + Mathf.Sin(time * 3f) * 0.15f, 0f);
            model.Rotate(0f, 90f * dt, 0f);
            if (life < 3f) model.gameObject.SetActive(Mathf.Repeat(time * 8f, 1f) > 0.3f);
            if (life <= 0f) { Destroy(gameObject); return; }

            var gm = GameManager.I;
            var p = gm.Player;
            if (p == null || p.IsDead || gm.State != GameState.Playing) return;
            if (Arena.FlatDistance(p.transform.position, transform.position) < p.radius + 0.7f)
            {
                p.Heal(Heal);
                gm.FX.Burst(transform.position + Vector3.up * 0.5f, Palette.Rice, 14, 4f, 0.18f, 0.5f);
                gm.Audio.Play(Sfx.Pickup, 0.7f);
                Destroy(gameObject);
            }
        }
    }
}
