using System.Collections;
using UnityEngine;

namespace ThanhGiong
{
    /// <summary>
    /// Thánh Gióng cưỡi ngựa sắt — nhân vật do người chơi điều khiển.
    /// Kỹ năng: vung roi sắt (combo 3 đòn), lướt né đòn, ngựa sắt phun lửa, nhổ tre làm vũ khí.
    /// Sau mỗi đợt giặc, Gióng "vươn vai" lớn thêm (to hơn, khỏe hơn, đánh xa hơn).
    /// </summary>
    public class Player : Character
    {
        struct Commands
        {
            public Vector3 move;
            public bool attack, attackMouse, fire, fireMouse, dash, interact;
        }

        // Chỉ số (được nâng cấp qua UpgradeSystem)
        public float Damage, MoveSpeed, AttackCooldown, AttackRange, DashCooldown;
        public float FireCost, FireDamageMul, Lifesteal, CritChance;

        public const float MaxEnergy = 100f;
        const float BambooDuration = 15f;

        public float Energy { get; private set; }
        public float GrowthScale { get; private set; } = 1f;
        public int GrowthLevel { get; private set; } = 1;
        public Vector3 Velocity { get; private set; }
        public bool IsDashing => dashTimer > 0f;
        public bool HasBamboo => bambooTimer > 0f;
        public float BambooTimeLeft => bambooTimer;
        public BambooClump NearbyBamboo { get; private set; }
        public bool AutoPilot { get; set; }
        public float EffectiveRange => (AttackRange + (HasBamboo ? 1.6f : 0f)) * GrowthScale;

        Rig rig;
        int combo, swingSign = 1;
        float comboTimer, attackTimer, attackAnim, dashTimer, dashCd, invuln, bambooTimer;
        float fireAnim, fireEmit, gallop, deathTimer, noFireMsgTimer, autoDashTimer, ascendTime;
        bool ascending;
        Vector3 dashDir, fireDir;

        static GameManager GM => GameManager.I;

        public void Build()
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            rig = ModelFactory.BuildGiong(visual);
            CacheRenderers();
        }

        public void ResetForNewRun()
        {
            StopAllCoroutines();
            Damage = 20f; MoveSpeed = 6.5f; AttackCooldown = 0.32f; AttackRange = 2.7f; DashCooldown = 0.9f;
            FireCost = 35f; FireDamageMul = 1f; Lifesteal = 0f; CritChance = 0.1f;
            MaxHP = 120f; HP = MaxHP; Energy = 50f;
            GrowthScale = 1f; GrowthLevel = 1;
            ApplyScale(GrowthScale);
            IsDead = false; ascending = false;
            combo = 0;
            comboTimer = attackTimer = attackAnim = dashTimer = dashCd = invuln = bambooTimer = 0f;
            fireAnim = fireEmit = deathTimer = 0f;
            knockVel = Vector3.zero; Velocity = Vector3.zero;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 45f, 0f));
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            SetBambooVisual(false);
            ClearFlash();
        }

        // ------------------------------------------------------------------ Vòng lặp chính
        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (IsDead) { UpdateDeath(dt); return; }
            if (ascending) { UpdateAscend(dt); return; }
            if (GM == null || GM.State != GameState.Playing) { Velocity = Vector3.zero; Animate(dt, 0f); return; }

            attackTimer -= dt; dashCd -= dt; invuln -= dt; comboTimer -= dt; noFireMsgTimer -= dt;
            attackAnim = Mathf.Max(0f, attackAnim - dt / 0.2f);
            fireAnim = Mathf.Max(0f, fireAnim - dt);
            Energy = Mathf.Min(MaxEnergy, Energy + 2.5f * dt);
            if (bambooTimer > 0f)
            {
                bambooTimer -= dt;
                if (bambooTimer <= 0f)
                {
                    SetBambooVisual(false);
                    GM.UI.FloatText(transform.position + Vector3.up * 3f * GrowthScale, "Tre gãy!", Palette.BambooGold, 1f);
                }
            }

            FindNearbyBamboo();
            var cmd = AutoPilot ? AutoCommands(dt) : ReadCommands();

            if (cmd.interact && NearbyBamboo != null && !HasBamboo) PullBamboo();
            if (cmd.dash && dashCd <= 0f) StartDash(cmd.move);

            float moveAmount;
            if (dashTimer > 0f)
            {
                dashTimer -= dt;
                MoveBy(dashDir * 24f * dt);
                Velocity = dashDir * 24f;
                GM.FX.Spawn(transform.position + Vector3.up * Random.Range(0.5f, 2.2f) * GrowthScale,
                    -dashDir * 2f + Random.insideUnitSphere, Palette.Iron, 0.35f, 0.3f, 0f, 3f);
                moveAmount = 1f;
            }
            else
            {
                float speed = MoveSpeed * (attackAnim > 0f ? 0.5f : 1f) * (fireAnim > 0f ? 0.4f : 1f);
                Vector3 vel = cmd.move * speed;
                MoveBy(vel * dt);
                Velocity = vel;
                moveAmount = cmd.move.magnitude;
                if (moveAmount > 0.1f && attackAnim <= 0f && fireAnim <= 0f) FaceDirection(cmd.move, dt, 14f);
            }

            if (dashTimer <= 0f && attackTimer <= 0f)
            {
                if (cmd.attack) DoAttack(cmd);
                else if (cmd.fire)
                {
                    if (Energy >= FireCost) DoFire(cmd);
                    else if (noFireMsgTimer <= 0f)
                    {
                        noFireMsgTimer = 1f;
                        GM.UI.FloatText(transform.position + Vector3.up * 3f * GrowthScale, "Chưa đủ Lửa!", Palette.Fire, 0.9f);
                    }
                }
            }

            if (fireEmit > 0f) EmitFire(dt);
            UpdateKnockback(dt);
            Animate(dt, moveAmount);
        }

        Commands ReadCommands()
        {
            Vector2 mv = InputReader.Move;
            var c = new Commands
            {
                move = GM.Cam.FlatForward * mv.y + GM.Cam.FlatRight * mv.x,
                attackMouse = InputReader.AttackMouse,
                fireMouse = InputReader.FireMouse,
                dash = InputReader.Dash,
                interact = InputReader.Interact
            };
            c.attack = c.attackMouse || InputReader.AttackKey;
            c.fire = c.fireMouse || InputReader.FireKey;
            return c;
        }

        /// <summary>Chế độ tự chơi (dùng để chạy thử tự động / trình diễn).</summary>
        Commands AutoCommands(float dt)
        {
            var c = new Commands();
            Enemy target = null;
            float best = float.MaxValue;
            foreach (var e in GM.Enemies)
            {
                if (e == null || e.IsDead) continue;
                float d = Arena.FlatDistance(e.transform.position, transform.position);
                if (d < best) { best = d; target = e; }
            }
            if (NearbyBamboo != null && !HasBamboo) c.interact = true;
            if (target != null)
            {
                Vector3 to = target.transform.position - transform.position;
                to.y = 0f;
                if (best > EffectiveRange * 0.8f + target.radius) c.move = to.normalized;
                else c.attack = true;
                if (Energy >= FireCost && best < 6f) c.fire = true;
                autoDashTimer -= dt;
                if (autoDashTimer <= 0f && best < 3f && HP < MaxHP * 0.5f)
                {
                    autoDashTimer = 2f;
                    c.dash = true;
                    c.move = Quaternion.Euler(0f, 90f, 0f) * to.normalized;
                }
            }
            else if (transform.position.sqrMagnitude > 4f) c.move = -transform.position.normalized;
            return c;
        }

        // ------------------------------------------------------------------ Kỹ năng
        Vector3 AimDirection(bool useMouse, float range, Vector3 moveInput)
        {
            if (useMouse && InputReader.TryGetMouseGround(GM.Cam.Cam, out var mp))
            {
                Vector3 d = mp - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f) return d.normalized;
            }

            // Hỗ trợ ngắm: chọn giặc gần nhất nằm phía trước
            Vector3 fwd = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : transform.forward;
            Enemy bestEnemy = null;
            float bestScore = float.MaxValue;
            foreach (var e in GM.Enemies)
            {
                if (e == null || e.IsDead) continue;
                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > range * 1.6f + e.radius) continue;
                float angle = Vector3.Angle(fwd, to);
                if (angle > 100f) continue;
                float score = dist + angle * 0.03f;
                if (score < bestScore) { bestScore = score; bestEnemy = e; }
            }
            if (bestEnemy != null)
            {
                Vector3 d = bestEnemy.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f) return d.normalized;
            }
            return fwd;
        }

        void DoAttack(Commands cmd)
        {
            combo = comboTimer > 0f ? (combo + 1) % 3 : 0;
            comboTimer = 0.9f;
            bool finisher = combo == 2;
            float range = EffectiveRange * (finisher ? 1.15f : 1f);
            float arc = HasBamboo ? (finisher ? 360f : 220f) : (finisher ? 240f : 150f);

            transform.rotation = Quaternion.LookRotation(AimDirection(cmd.attackMouse, range, cmd.move));
            swingSign = combo % 2 == 0 ? 1 : -1;
            attackAnim = 1f;
            attackTimer = AttackCooldown * (finisher ? 1.5f : 1f);

            float dmg = Damage * (finisher ? 1.6f : 1f) * (HasBamboo ? 1.3f : 1f);
            int hits = HitEnemiesInArc(range, arc, dmg, finisher ? 13f : 6f, false, 0f);

            Color slashColor = HasBamboo ? Palette.BambooLight : (finisher ? Palette.FireYellow : Palette.SlashWhite);
            GM.FX.Slash(transform, range, arc, 1.7f * GrowthScale, swingSign, slashColor);
            GM.Audio.Play(Sfx.Swing, 0.7f, 0.15f);
            if (hits > 0)
            {
                GM.Audio.Play(Sfx.Hit, 0.9f);
                GM.FX.Hitstop(finisher ? 0.08f : 0.045f);
                GM.Cam.Shake(finisher ? 0.35f : 0.18f, 0.15f);
                Energy = Mathf.Min(MaxEnergy, Energy + 6f * hits);
                if (Lifesteal > 0f) Heal(Lifesteal * hits, false);
            }
        }

        void DoFire(Commands cmd)
        {
            Energy -= FireCost;
            float range = 7.5f * GrowthScale;
            fireDir = AimDirection(cmd.fireMouse, range, cmd.move);
            transform.rotation = Quaternion.LookRotation(fireDir);
            fireAnim = 0.55f;
            fireEmit = 0.35f;
            attackTimer = 0.45f;
            int hits = HitEnemiesInArc(range, 60f, Damage * 1.5f * FireDamageMul, 9f, true, Damage * 0.35f * FireDamageMul);
            GM.Audio.Play(Sfx.Fire, 1f);
            GM.Cam.Shake(0.3f, 0.3f);
            if (hits > 0) GM.FX.Hitstop(0.05f);
        }

        int HitEnemiesInArc(float range, float arc, float dmg, float knock, bool fire, float burnDps)
        {
            int hits = 0;
            var list = GM.Enemies;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var e = list[i];
                if (e == null || !e.IsTargetable) continue;
                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d > range + e.radius) continue;
                if (arc < 360f && d > 0.01f && Vector3.Angle(transform.forward, to) > arc * 0.5f) continue;
                bool crit = Random.value < CritChance;
                e.TakeDamage(new DamageInfo
                {
                    amount = dmg * (crit ? 1.8f : 1f), sourcePos = transform.position, knockback = knock, crit = crit, isFire = fire
                });
                if (fire && burnDps > 0f && !e.IsDead) e.ApplyBurn(burnDps, 2.5f);
                hits++;
            }
            return hits;
        }

        void EmitFire(float dt)
        {
            fireEmit -= dt;
            Vector3 origin = rig.mouth.position;
            for (int i = 0; i < 7; i++)
            {
                Vector3 d = Quaternion.AngleAxis(Random.Range(-28f, 28f), Vector3.up) * fireDir;
                d.y = Random.Range(-0.12f, 0.15f);
                Color c = (i % 3) switch { 0 => Palette.FireYellow, 1 => Palette.Fire, _ => Palette.FireRed };
                GM.FX.Spawn(origin, d.normalized * Random.Range(15f, 21f) * GrowthScale, c,
                    Random.Range(0.25f, 0.55f) * GrowthScale, Random.Range(0.35f, 0.5f), 2f, 1.5f);
            }
        }

        void StartDash(Vector3 move)
        {
            dashDir = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(dashDir);
            dashTimer = 0.2f;
            dashCd = DashCooldown;
            invuln = 0.3f;
            GM.Audio.Play(Sfx.Dash, 0.8f);
        }

        void FindNearbyBamboo()
        {
            NearbyBamboo = null;
            float best = radius + 2f;
            foreach (var b in BambooClump.All)
            {
                if (b == null || !b.Available) continue;
                float d = Arena.FlatDistance(b.transform.position, transform.position);
                if (d < best) { best = d; NearbyBamboo = b; }
            }
        }

        void PullBamboo()
        {
            if (!NearbyBamboo.TryPull()) return;
            bambooTimer = BambooDuration;
            SetBambooVisual(true);
            GM.UI.FloatText(transform.position + Vector3.up * 3.5f * GrowthScale, "Nhổ tre đánh giặc!", Palette.BambooLight, 1.3f);
            GM.Audio.Play(Sfx.Pickup, 0.8f);
            GM.Cam.Shake(0.2f, 0.2f);
        }

        void SetBambooVisual(bool bamboo)
        {
            rig.weapon.gameObject.SetActive(!bamboo);
            rig.weaponAlt.gameObject.SetActive(bamboo);
        }

        // ------------------------------------------------------------------ Máu, lớn lên
        public override void TakeDamage(DamageInfo info)
        {
            if (IsDead || invuln > 0f || dashTimer > 0f || ascending || GM.State != GameState.Playing) return;
            if (!GM.GodMode) HP -= info.amount;
            invuln = 0.6f;
            Flash(0.12f);
            Vector3 dir = transform.position - info.sourcePos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) knockVel += dir.normalized * info.knockback * 0.6f / GrowthScale;
            GM.FX.DamageNumber(transform.position + Vector3.up * 3f * GrowthScale, info.amount, Palette.Hurt, 1.2f);
            GM.FX.Burst(transform.position + Vector3.up * 1.5f, Palette.Hurt, 10, 5f, 0.2f, 0.4f);
            GM.Cam.Shake(0.35f, 0.25f);
            GM.Audio.Play(Sfx.Hurt, 0.8f);
            GM.UI.FlashHurt();
            if (HP <= 0f)
            {
                HP = 0f;
                IsDead = true;
                deathTimer = 0f;
                GM.OnPlayerDied();
            }
        }

        public void Heal(float amount, bool showText = true)
        {
            if (IsDead) return;
            float before = HP;
            HP = Mathf.Min(MaxHP, HP + amount);
            if (showText && HP > before)
                GM.UI.FloatText(transform.position + Vector3.up * 3f * GrowthScale, "+" + Mathf.RoundToInt(HP - before) + " Máu", Palette.BambooLight, 1f);
        }

        public void AddMaxHP(float amount) { MaxHP += amount; HP += amount; }
        public void FullHeal() => HP = MaxHP;

        public void Grow()
        {
            GrowthLevel++;
            float from = GrowthScale;
            GrowthScale += 0.12f;
            MaxHP += 15f;
            HP = Mathf.Min(MaxHP, HP + 40f);
            Damage += 3f;
            StartCoroutine(GrowRoutine(from, GrowthScale));
            GM.FX.Burst(transform.position + Vector3.up, Palette.Gold, 40, 7f, 0.3f, 1f, -4f, 1f);
            GM.Audio.Play(Sfx.Pickup, 0.9f);
            GM.UI.FloatText(transform.position + Vector3.up * 4f * GrowthScale, "Gióng lớn lên!", Palette.Gold, 1.5f);
        }

        IEnumerator GrowRoutine(float from, float to)
        {
            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                float k = t / 0.8f;
                float overshoot = Mathf.Sin(k * Mathf.PI) * 0.06f;
                ApplyScale(Mathf.Lerp(from, to, k) + overshoot);
                yield return null;
            }
            ApplyScale(to);
        }

        void ApplyScale(float g)
        {
            transform.localScale = Vector3.one * 0.85f * g;
            radius = 0.75f * g;
            if (GM != null && GM.Cam != null) GM.Cam.zoom = 0.85f + 0.15f * g;
        }

        // ------------------------------------------------------------------ Kết thúc: bay về trời
        public void BeginAscend()
        {
            ascending = true;
            ascendTime = 0f;
        }

        void UpdateAscend(float dt)
        {
            ascendTime += dt;
            Vector3 dir = new Vector3(1f, 0f, 1f).normalized; // hướng núi Sóc Sơn
            FaceDirection(dir, dt, 4f);
            float speed = Mathf.Min(14f, 6f + ascendTime * 3f);
            float rise = Mathf.Max(0f, ascendTime - 1.2f) * 3.5f;
            transform.position += (transform.forward * speed + Vector3.up * rise) * dt;
            for (int i = 0; i < 3; i++)
            {
                Color c = i == 0 ? Palette.FireYellow : Palette.Fire;
                GM.FX.Spawn(transform.position + Vector3.up * 0.6f * GrowthScale + Random.insideUnitSphere * 0.6f,
                    Vector3.down * 2f + Random.insideUnitSphere, c, 0.4f * GrowthScale, 0.6f, 1f, 1f);
            }
            Animate(dt, 1f);
        }

        void UpdateDeath(float dt)
        {
            deathTimer += dt;
            float t = Mathf.Clamp01(deathTimer / 0.7f);
            visual.localRotation = Quaternion.Euler(0f, 0f, 80f * t * t);
            visual.localPosition = new Vector3(0f, -0.3f * t, 0f);
        }

        // ------------------------------------------------------------------ Hoạt ảnh bằng code
        void Animate(float dt, float moveAmount)
        {
            gallop += dt * (4f + 10f * moveAmount);
            float s = Mathf.Sin(gallop);
            float swing = 38f * moveAmount;
            // Kiểu chạy nước kiệu: chân trước phải + sau trái cùng nhịp
            rig.horseLegs[0].localRotation = Quaternion.Euler(s * swing, 0f, 0f);
            rig.horseLegs[1].localRotation = Quaternion.Euler(-s * swing, 0f, 0f);
            rig.horseLegs[2].localRotation = Quaternion.Euler(-s * swing, 0f, 0f);
            rig.horseLegs[3].localRotation = Quaternion.Euler(s * swing, 0f, 0f);
            rig.horse.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Cos(gallop)) * 0.12f * moveAmount + Mathf.Sin(Time.time * 2f) * 0.02f, 0f);

            float headLift = fireAnim > 0f ? -25f * Mathf.Sin(Mathf.Clamp01(fireAnim / 0.55f) * Mathf.PI) : 0f;
            rig.horseHead.localRotation = Quaternion.Euler(Mathf.Sin(gallop * 2f) * 6f * moveAmount + headLift, 0f, 0f);
            rig.tail.localRotation = Quaternion.Euler(20f + Mathf.Sin(Time.time * 3f) * 10f + moveAmount * 30f, Mathf.Sin(Time.time * 2.3f) * 15f, 0f);

            if (attackAnim > 0f)
            {
                float t = 1f - attackAnim;
                float e = 1f - (1f - t) * (1f - t);
                rig.body.localRotation = Quaternion.Euler(0f, Mathf.Lerp(75f, -75f, e) * swingSign, 0f);
                rig.armR.localRotation = Quaternion.Euler(-95f, 0f, 0f);
            }
            else
            {
                rig.body.localRotation = Quaternion.Slerp(rig.body.localRotation, Quaternion.identity, 1f - Mathf.Exp(-12f * dt));
                SetRotX(rig.armR, -50f + Mathf.Sin(Time.time * 2f) * 3f, dt, 12f);
            }
            SetRotX(rig.armL, -35f, dt);
        }
    }
}
