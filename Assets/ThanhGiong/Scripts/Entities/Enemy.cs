using UnityEngine;

namespace ThanhGiong
{
    public enum EnemyKind { Spearman, Archer, Shield, Boss }

    /// <summary>
    /// Lớp cơ sở của giặc Ân. AI dùng máy trạng thái (Finite State Machine):
    /// Spawning → Chase → Windup (gồng đòn) → Attack → Recover → Chase …
    /// Lớp con chỉ cần cài đặt Configure (chỉ số + mô hình) và Think (hành vi theo trạng thái).
    /// </summary>
    public abstract class Enemy : Character
    {
        protected enum AIState { Spawning, Chase, Windup, Attack, Recover }

        public int ScoreValue { get; protected set; } = 100;
        public float DropChance { get; protected set; } = 0.12f;

        protected AIState state = AIState.Spawning;
        protected float stateTimer, attackCooldown, staggerTimer;
        protected float moveSpeed = 3f, damage = 10f, attackRange = 1.8f, staggerResist;
        protected float spawnDuration = 0.7f;
        protected bool lastHitBlocked;
        protected Rig rig;

        float walkCycle, burnTimer, burnDps, burnTick, deathTimer;
        Transform hpBar, hpFill;

        /// <summary>Có thể bị đánh trúng không (không tính lúc đang chui lên từ mặt đất).</summary>
        public bool IsTargetable => !IsDead && state != AIState.Spawning;

        protected static GameManager GM => GameManager.I;
        protected static Player Target => GameManager.I.Player;
        protected virtual bool ShowWorldHealthBar => true;
        protected virtual float BarHeight => 2.25f;

        public void Setup(float hpMul, float dmgMul)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            Configure(hpMul, dmgMul);
            HP = MaxHP;
            CacheRenderers();
            if (ShowWorldHealthBar) BuildHpBar();
            state = AIState.Spawning;
            stateTimer = spawnDuration;
            visual.localPosition = new Vector3(0f, -2f, 0f);
            GM.Enemies.Add(this);
            GM.FX.Burst(transform.position + Vector3.up * 0.2f, Palette.Dirt, 14, 4f, 0.28f, 0.6f);
        }

        protected abstract void Configure(float hpMul, float dmgMul);
        protected abstract void Think(float dt);

        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (IsDead) { UpdateDeath(dt); return; }

            UpdateBurn(dt);
            if (IsDead) return;
            UpdateKnockback(dt);

            if (state == AIState.Spawning)
            {
                stateTimer -= dt;
                float t = 1f - Mathf.Clamp01(stateTimer / spawnDuration);
                visual.localPosition = new Vector3(0f, Mathf.Lerp(-2f, 0f, t), 0f);
                if (stateTimer <= 0f) { visual.localPosition = Vector3.zero; state = AIState.Chase; }
                return;
            }

            if (staggerTimer > 0f)
            {
                staggerTimer -= dt;
                if (rig.body != null) rig.body.localRotation = Quaternion.Euler(-15f, 0f, 0f);
                return;
            }

            var p = Target;
            if (p == null || p.IsDead || GM.State == GameState.Victory)
            {
                AnimateWalk(0f, dt);
                return;
            }

            attackCooldown -= dt;
            LeanBody(0f, dt); // trả thân về tư thế đứng; lớp con có thể ghi đè trong Think
            Think(dt);
            Separate();
        }

        void LateUpdate()
        {
            if (hpBar == null || !hpBar.gameObject.activeSelf) return;
            hpBar.rotation = GM.Cam.transform.rotation;
            hpFill.localScale = new Vector3(Mathf.Clamp01(HP / MaxHP), 1f, 1f);
        }

        // ------------------------------------------------------------------ Nhận sát thương
        public override void TakeDamage(DamageInfo info)
        {
            if (IsDead || state == AIState.Spawning) return;
            lastHitBlocked = false;
            float amount = ModifyDamage(ref info);
            HP -= amount;
            Flash();
            if (hpBar != null) hpBar.gameObject.SetActive(true);

            Vector3 dir = transform.position - info.sourcePos;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.001f ? dir.normalized : -transform.forward;
            knockVel += dir * info.knockback * (1f - staggerResist);

            float h = BarHeight * transform.localScale.y;
            Color c = info.isFire ? Palette.Fire : (info.crit ? Palette.FireYellow : Color.white);
            GM.FX.DamageNumber(transform.position + Vector3.up * h, amount, c, info.crit ? 1.4f : 1f);
            GM.FX.Burst(transform.position + Vector3.up * h * 0.5f, info.isFire ? Palette.Fire : Palette.Hurt, 6, 5f, 0.15f, 0.35f);

            if (!lastHitBlocked && staggerResist < 0.99f)
            {
                staggerTimer = 0.28f * (1f - staggerResist);
                if (state == AIState.Windup)
                {
                    state = AIState.Chase; // bị đánh trúng khi đang gồng => mất đòn
                    attackCooldown = Mathf.Max(attackCooldown, 0.4f);
                }
            }

            if (HP <= 0f) Die(true);
        }

        protected virtual float ModifyDamage(ref DamageInfo info) => info.amount;

        public void ApplyBurn(float dps, float duration)
        {
            if (IsDead) return;
            burnDps = Mathf.Max(burnDps, dps);
            burnTimer = Mathf.Max(burnTimer, duration);
        }

        void UpdateBurn(float dt)
        {
            if (burnTimer <= 0f) return;
            burnTimer -= dt;
            burnTick -= dt;
            if (Random.value < 0.4f)
                GM.FX.Spawn(transform.position + Vector3.up * Random.Range(0.5f, 1.8f) * transform.localScale.y,
                    Vector3.up * 2f, Random.value < 0.5f ? Palette.Fire : Palette.FireYellow, 0.2f, 0.4f, 1f, 1f);
            if (burnTick > 0f) return;
            burnTick = 0.5f;
            float dmg = burnDps * 0.5f;
            HP -= dmg;
            GM.FX.DamageNumber(transform.position + Vector3.up * BarHeight * transform.localScale.y, dmg, Palette.Fire, 0.8f);
            if (HP <= 0f) Die(true);
        }

        public void ForceKill() => Die(false);

        protected void Die(bool award)
        {
            if (IsDead) return;
            IsDead = true;
            burnTimer = 0f;
            deathTimer = 0f;
            GM.Enemies.Remove(this);
            if (award) GM.OnEnemyKilled(this);
            if (hpBar != null) hpBar.gameObject.SetActive(false);
            GM.FX.Burst(transform.position + Vector3.up, Palette.EnemyRed, 18, 6f, 0.25f, 0.7f);
            GM.Audio.Play(Sfx.EnemyDie, 0.6f, 0.15f);
            OnDied();
        }

        protected virtual void OnDied() { }

        void UpdateDeath(float dt)
        {
            deathTimer += dt;
            float t = Mathf.Clamp01(deathTimer / 0.35f);
            visual.localRotation = Quaternion.Euler(-90f * t * t, 0f, 0f);
            if (deathTimer > 1.2f) visual.localPosition += Vector3.down * dt * 1.2f;
            if (deathTimer > 2.2f) Destroy(gameObject);
        }

        // ------------------------------------------------------------------ Tiện ích cho AI
        protected float DistanceToTarget() => Arena.FlatDistance(transform.position, Target.transform.position);

        protected Vector3 DirToTarget()
        {
            Vector3 d = Target.transform.position - transform.position;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : transform.forward;
        }

        protected void FaceTarget(float dt, float speed = 10f) => FaceDirection(DirToTarget(), dt, speed);

        protected void MoveToward(Vector3 target, float speed, float dt)
        {
            Vector3 d = target - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            Vector3 dir = d.normalized;
            FaceDirection(dir, dt, 10f);
            MoveBy(dir * speed * dt);
            AnimateWalk(1f, dt);
        }

        protected void MoveDir(Vector3 dir, float speed, float dt)
        {
            MoveBy(dir * speed * dt);
            AnimateWalk(Mathf.Clamp01(speed / Mathf.Max(0.1f, moveSpeed)), dt);
        }

        protected bool TargetInArc(float range, float arcDegrees)
        {
            Vector3 to = Target.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude > range + Target.radius) return false;
            return Vector3.Angle(transform.forward, to) <= arcDegrees * 0.5f;
        }

        protected void HitTarget(float amount, float knockback)
        {
            Target.TakeDamage(new DamageInfo { amount = amount, sourcePos = transform.position, knockback = knockback });
        }

        protected void AnimateWalk(float amount, float dt)
        {
            walkCycle += dt * 9f * amount;
            float s = Mathf.Sin(walkCycle) * 35f * Mathf.Clamp01(amount);
            if (rig.legL != null) rig.legL.localRotation = Quaternion.Euler(s, 0f, 0f);
            if (rig.legR != null) rig.legR.localRotation = Quaternion.Euler(-s, 0f, 0f);
            visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(walkCycle)) * 0.06f * amount, 0f);
        }

        protected void LeanBody(float x, float dt)
        {
            if (rig.body != null)
                rig.body.localRotation = Quaternion.Slerp(rig.body.localRotation, Quaternion.Euler(x, 0f, 0f), 1f - Mathf.Exp(-12f * dt));
        }

        protected void Warn()
        {
            GM.UI.FloatText(transform.position + Vector3.up * (BarHeight + 0.4f) * transform.localScale.y, "!", Palette.Telegraph, 1.3f);
        }

        /// <summary>Đẩy các lính giặc ra xa nhau để không chồng lên nhau.</summary>
        void Separate()
        {
            var list = GM.Enemies;
            Vector3 pos = transform.position;
            Vector3 push = Vector3.zero;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                if (o == this || o == null) continue;
                Vector3 d = pos - o.transform.position;
                d.y = 0f;
                float min = radius + o.radius;
                float sq = d.sqrMagnitude;
                if (sq >= min * min || sq < 0.00001f) continue;
                float m = Mathf.Sqrt(sq);
                push += d / m * (min - m) * 0.5f;
            }
            Vector3 toPlayer = pos - Target.transform.position;
            toPlayer.y = 0f;
            float pm = radius + Target.radius;
            if (toPlayer.sqrMagnitude < pm * pm && toPlayer.sqrMagnitude > 0.00001f)
                push += toPlayer.normalized * (pm - toPlayer.magnitude);
            if (push.sqrMagnitude > 0f) MoveBy(push);
        }

        void BuildHpBar()
        {
            hpBar = new GameObject("ThanhMau").transform;
            hpBar.SetParent(transform, false);
            hpBar.localPosition = new Vector3(0f, BarHeight, 0f);
            ModelFactory.Cube(hpBar, Vector3.zero, new(1.0f, 0.14f, 0.02f), Palette.BarBg);
            hpFill = ModelFactory.Pivot(hpBar, new(-0.48f, 0f, -0.02f), "Fill");
            ModelFactory.Cube(hpFill, new(0.48f, 0f, 0f), new(0.96f, 0.1f, 0.02f), Palette.BarRed);
            ModelFactory.NoShadow(hpBar);
            hpBar.gameObject.SetActive(false);
        }
    }
}
