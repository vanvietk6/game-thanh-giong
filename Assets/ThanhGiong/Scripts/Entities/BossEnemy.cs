using System.Collections.Generic;
using UnityEngine;

namespace ThanhGiong
{
    /// <summary>
    /// Tướng giặc Ân (trùm cuối). Có 3 đòn với vùng cảnh báo đỏ dưới đất:
    ///  - Chém quét: vùng tròn trước mặt.
    ///  - Nện đất: vòng tròn tại vị trí Gióng (giai đoạn 2: thêm 2 vòng).
    ///  - Xung phong: lao thẳng theo đường cảnh báo.
    /// Dưới 50% máu chuyển sang giai đoạn 2: nhanh hơn và gọi thêm quân tiếp viện.
    /// </summary>
    public class BossEnemy : Enemy
    {
        enum Move { Cleave, Slam, Charge }

        public const string BossName = "Tướng giặc Ân";
        const float SlamRadius = 3.2f;
        const float CleaveRadius = 3.2f;

        Move current;
        bool phase2;
        float summonTimer = 12f, chargeTime;
        bool chargeHit;
        Vector3 chargeDir, cleaveCenter;
        readonly List<Vector3> slamPoints = new();

        public bool Phase2 => phase2;
        protected override bool ShowWorldHealthBar => false;

        protected override void Configure(float hpMul, float dmgMul)
        {
            rig = ModelFactory.BuildBoss(visual);
            transform.localScale = Vector3.one * 2.1f;
            MaxHP = 1600f;
            damage = 22f;
            moveSpeed = 3.0f;
            radius = 1.2f;
            staggerResist = 1f;
            spawnDuration = 1.4f;
            ScoreValue = 3000;
            DropChance = 0f;
            attackCooldown = 1.5f;
        }

        protected override void Think(float dt)
        {
            if (!phase2 && HP <= MaxHP * 0.5f) EnterPhase2();
            if (phase2)
            {
                summonTimer -= dt;
                if (summonTimer <= 0f && GM.Enemies.Count < 8) { Summon(3); summonTimer = 14f; }
            }

            float dist = DistanceToTarget();
            switch (state)
            {
                case AIState.Chase:
                    if (dist > radius + Target.radius + 1.2f) MoveToward(Target.transform.position, moveSpeed, dt);
                    else { FaceTarget(dt, 4f); AnimateWalk(0f, dt); }
                    SetRotX(rig.armR, -40f, dt);
                    SetRotX(rig.armL, -10f, dt);
                    LeanBody(0f, dt);
                    if (attackCooldown <= 0f) ChooseMove(dist);
                    break;

                case AIState.Windup:
                    AnimateWalk(0f, dt);
                    if (current == Move.Cleave) { FaceTarget(dt, 3f); SetRotX(rig.armR, -165f, dt, 8f); LeanBody(-10f, dt); }
                    else if (current == Move.Slam) { SetRotX(rig.armR, -175f, dt, 8f); SetRotX(rig.armL, -175f, dt, 8f); LeanBody(-15f, dt); }
                    else { SetRotX(rig.armR, -60f, dt); LeanBody(20f, dt); }
                    stateTimer -= dt;
                    if (stateTimer <= 0f) ExecuteMove();
                    break;

                case AIState.Attack: // đang xung phong
                    chargeTime -= dt;
                    MoveBy(chargeDir * 20f * dt);
                    AnimateWalk(2f, dt);
                    if (Random.value < 0.6f)
                        GM.FX.Spawn(transform.position + Random.insideUnitSphere * 0.8f, Vector3.up * 2f, Palette.Dirt, 0.4f, 0.5f, -4f, 2f);
                    if (!chargeHit && DistanceToTarget() < radius + Target.radius + 0.4f)
                    {
                        chargeHit = true;
                        HitTarget(damage * 1.1f, 18f);
                        GM.Cam.Shake(0.5f, 0.3f);
                    }
                    if (chargeTime <= 0f) { state = AIState.Recover; stateTimer = 1.1f; }
                    break;

                case AIState.Recover:
                    AnimateWalk(0f, dt);
                    SetRotX(rig.armR, -20f, dt);
                    SetRotX(rig.armL, -10f, dt);
                    LeanBody(15f, dt); // thở dốc => thời cơ để tấn công
                    stateTimer -= dt;
                    if (stateTimer <= 0f) state = AIState.Chase;
                    break;
            }
        }

        void ChooseMove(float dist)
        {
            if (dist < 4.5f + Target.radius) current = Move.Cleave;
            else if (dist > 7f && Random.value < 0.55f) current = Move.Charge;
            else current = Move.Slam;

            state = AIState.Windup;
            GM.Audio.Play(Sfx.Warn, 0.6f, 0.05f);
            switch (current)
            {
                case Move.Cleave:
                    stateTimer = phase2 ? 0.6f : 0.8f;
                    transform.rotation = Quaternion.LookRotation(DirToTarget());
                    cleaveCenter = transform.position + transform.forward * 2.4f;
                    GM.FX.Telegraph(cleaveCenter, CleaveRadius, stateTimer);
                    break;
                case Move.Charge:
                    stateTimer = 0.9f;
                    chargeDir = DirToTarget();
                    transform.rotation = Quaternion.LookRotation(chargeDir);
                    GM.FX.LineTelegraph(transform.position, chargeDir, 16f, radius * 2f, stateTimer);
                    break;
                case Move.Slam:
                    stateTimer = 1.1f;
                    slamPoints.Clear();
                    slamPoints.Add(Target.transform.position);
                    if (phase2)
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 r = Random.insideUnitCircle * 5f;
                            slamPoints.Add(Arena.Resolve(Target.transform.position + new Vector3(r.x, 0f, r.y), SlamRadius * 0.5f));
                        }
                    foreach (var p in slamPoints) GM.FX.Telegraph(p, SlamRadius, stateTimer);
                    break;
            }
        }

        void ExecuteMove()
        {
            switch (current)
            {
                case Move.Cleave:
                    if (Arena.FlatDistance(Target.transform.position, cleaveCenter) < CleaveRadius + Target.radius * 0.5f)
                        HitTarget(damage, 14f);
                    GM.FX.Burst(cleaveCenter + Vector3.up * 0.3f, Palette.Dirt, 25, 7f, 0.3f, 0.6f);
                    GM.Cam.Shake(0.4f, 0.25f);
                    GM.Audio.Play(Sfx.Slam, 0.8f);
                    SetRotX(rig.armR, -40f, 1f, 100f);
                    state = AIState.Recover;
                    stateTimer = 0.7f;
                    attackCooldown = phase2 ? 0.6f : 1.0f;
                    break;

                case Move.Slam:
                    bool hit = false;
                    foreach (var p in slamPoints)
                    {
                        GM.FX.Burst(p + Vector3.up * 0.3f, Palette.Dirt, 30, 9f, 0.35f, 0.7f);
                        GM.FX.Burst(p + Vector3.up * 0.3f, Palette.Stone, 10, 6f, 0.4f, 0.8f);
                        if (!hit && Arena.FlatDistance(Target.transform.position, p) < SlamRadius + Target.radius * 0.5f)
                        {
                            hit = true;
                            HitTarget(damage * 1.2f, 12f);
                        }
                    }
                    GM.Cam.Shake(0.6f, 0.35f);
                    GM.Audio.Play(Sfx.Slam, 1f);
                    state = AIState.Recover;
                    stateTimer = 1.0f;
                    attackCooldown = phase2 ? 0.8f : 1.2f;
                    break;

                case Move.Charge:
                    state = AIState.Attack;
                    chargeTime = 0.75f;
                    chargeHit = false;
                    GM.Audio.Play(Sfx.Dash, 1f);
                    attackCooldown = phase2 ? 1.0f : 1.5f;
                    break;
            }
        }

        void EnterPhase2()
        {
            phase2 = true;
            moveSpeed *= 1.3f;
            GM.UI.ShowBanner("Tướng giặc nổi giận!", "Hắn gọi thêm quân tiếp viện", 2.5f);
            GM.Audio.Play(Sfx.Gong, 0.8f);
            GM.Cam.Shake(0.5f, 0.5f);
            Summon(4);
        }

        void Summon(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (360f / count) * i + Random.Range(-20f, 20f);
                Vector3 pos = transform.position + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 3.5f;
                GM.Waves.SpawnEnemy(EnemyKind.Spearman, Arena.Resolve(pos, 0.5f));
            }
        }

        protected override void OnDied()
        {
            GM.FX.Burst(transform.position + Vector3.up * 2f, Palette.Gold, 60, 10f, 0.4f, 1.2f);
            GM.FX.Burst(transform.position + Vector3.up * 2f, Palette.EnemyRed, 40, 8f, 0.4f, 1f);
            GM.Cam.Shake(0.8f, 0.6f);
        }
    }
}
