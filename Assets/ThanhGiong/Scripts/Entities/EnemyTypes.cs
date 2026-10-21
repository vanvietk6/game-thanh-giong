using UnityEngine;

namespace ThanhGiong
{
    /// <summary>Lính giáo: lao vào cận chiến, gồng đòn rồi đâm giáo.</summary>
    public class SpearmanEnemy : Enemy
    {
        protected override void Configure(float hpMul, float dmgMul)
        {
            rig = ModelFactory.BuildSoldier(visual, EnemyKind.Spearman);
            MaxHP = 50f * hpMul;
            damage = 10f * dmgMul;
            moveSpeed = Random.Range(3.1f, 3.8f);
            attackRange = 2.0f;
            radius = 0.45f;
            ScoreValue = 100;
        }

        protected override void Think(float dt)
        {
            float dist = DistanceToTarget();
            switch (state)
            {
                case AIState.Chase:
                    if (dist <= attackRange + Target.radius && attackCooldown <= 0f)
                    {
                        state = AIState.Windup;
                        stateTimer = 0.5f;
                        Warn();
                    }
                    else if (dist > attackRange * 0.7f + Target.radius) MoveToward(Target.transform.position, moveSpeed, dt);
                    else { FaceTarget(dt); AnimateWalk(0f, dt); }
                    SetRotX(rig.armR, -80f, dt);
                    SetRotX(rig.armL, -20f, dt);
                    LeanBody(0f, dt);
                    break;

                case AIState.Windup:
                    FaceTarget(dt, 6f);
                    AnimateWalk(0f, dt);
                    SetRotX(rig.armR, -15f, dt, 10f);
                    LeanBody(-12f, dt);
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        MoveBy(transform.forward * 0.5f);
                        if (TargetInArc(attackRange + 0.6f, 80f)) HitTarget(damage, 7f);
                        GM.Audio.Play(Sfx.Swing, 0.4f, 0.2f);
                        state = AIState.Recover;
                        stateTimer = 0.45f;
                        attackCooldown = Random.Range(1.2f, 1.8f);
                    }
                    break;

                case AIState.Recover:
                    SetRotX(rig.armR, -100f, dt, 25f);
                    LeanBody(10f, dt);
                    stateTimer -= dt;
                    if (stateTimer <= 0f) state = AIState.Chase;
                    break;
            }
        }
    }

    /// <summary>Cung thủ: giữ khoảng cách, đi ngang né tránh và bắn tên đón đầu.</summary>
    public class ArcherEnemy : Enemy
    {
        float strafeSign = 1f, strafeTimer;

        protected override void Configure(float hpMul, float dmgMul)
        {
            rig = ModelFactory.BuildSoldier(visual, EnemyKind.Archer);
            MaxHP = 35f * hpMul;
            damage = 8f * dmgMul;
            moveSpeed = 3.0f;
            radius = 0.45f;
            ScoreValue = 120;
            attackCooldown = Random.Range(1f, 2f);
        }

        protected override void Think(float dt)
        {
            float dist = DistanceToTarget();
            Vector3 toT = DirToTarget();
            switch (state)
            {
                case AIState.Chase:
                    if (dist > 11f) MoveToward(Target.transform.position, moveSpeed, dt);
                    else
                    {
                        if (dist < 6f) MoveDir(-toT, moveSpeed * 0.9f, dt);
                        else
                        {
                            strafeTimer -= dt;
                            if (strafeTimer <= 0f) { strafeSign = -strafeSign; strafeTimer = Random.Range(1.5f, 3f); }
                            MoveDir(Vector3.Cross(Vector3.up, toT) * strafeSign, moveSpeed * 0.5f, dt);
                        }
                        FaceDirection(toT, dt, 10f);
                    }
                    SetRotX(rig.armL, -85f, dt);
                    SetRotX(rig.armR, -30f, dt);
                    if (attackCooldown <= 0f && dist < 15f)
                    {
                        state = AIState.Windup;
                        stateTimer = 0.7f;
                        Warn();
                    }
                    break;

                case AIState.Windup:
                    FaceTarget(dt, 8f);
                    AnimateWalk(0f, dt);
                    SetRotX(rig.armL, -90f, dt);
                    SetRotX(rig.armR, -90f, dt);
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        // Bắn đón đầu theo vận tốc của Gióng
                        Vector3 aim = Target.transform.position + Target.Velocity * Mathf.Clamp(dist / 15f, 0f, 0.6f);
                        Vector3 dir = aim - transform.position;
                        dir.y = 0f;
                        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
                        Arrow.Spawn(transform.position + Vector3.up * 1.35f + transform.forward * 0.6f, dir.normalized, 15f, damage);
                        GM.Audio.Play(Sfx.Arrow, 0.5f, 0.15f);
                        state = AIState.Recover;
                        stateTimer = 0.35f;
                        attackCooldown = Random.Range(2.0f, 3.0f);
                    }
                    break;

                case AIState.Recover:
                    SetRotX(rig.armR, -30f, dt);
                    stateTimer -= dt;
                    if (stateTimer <= 0f) state = AIState.Chase;
                    break;
            }
        }
    }

    /// <summary>Lính khiên: chậm, trâu, đỡ được đòn đánh từ phía trước (trừ lửa).</summary>
    public class ShieldEnemy : Enemy
    {
        protected override void Configure(float hpMul, float dmgMul)
        {
            rig = ModelFactory.BuildSoldier(visual, EnemyKind.Shield);
            MaxHP = 110f * hpMul;
            damage = 14f * dmgMul;
            moveSpeed = 2.3f;
            attackRange = 1.9f;
            radius = 0.55f;
            staggerResist = 0.6f;
            ScoreValue = 200;
            DropChance = 0.25f;
        }

        protected override float ModifyDamage(ref DamageInfo info)
        {
            Vector3 toSrc = info.sourcePos - transform.position;
            toSrc.y = 0f;
            if (!info.isFire && state != AIState.Recover && Vector3.Angle(transform.forward, toSrc) < 70f)
            {
                lastHitBlocked = true;
                info.knockback *= 0.2f;
                GM.UI.FloatText(transform.position + Vector3.up * 2.6f, "Đỡ!", new Color(0.8f, 0.8f, 0.85f), 1f);
                GM.Audio.Play(Sfx.Block, 0.6f, 0.1f);
                GM.FX.Burst(transform.position + Vector3.up * 1.2f + transform.forward * 0.6f, Palette.FireYellow, 8, 6f, 0.1f, 0.25f);
                return info.amount * 0.2f;
            }
            return info.amount;
        }

        protected override void Think(float dt)
        {
            float dist = DistanceToTarget();
            switch (state)
            {
                case AIState.Chase:
                    if (dist <= attackRange + Target.radius && attackCooldown <= 0f)
                    {
                        state = AIState.Windup;
                        stateTimer = 0.65f;
                        Warn();
                    }
                    else if (dist > attackRange * 0.7f + Target.radius) MoveToward(Target.transform.position, moveSpeed, dt);
                    else { FaceTarget(dt, 5f); AnimateWalk(0f, dt); }
                    SetRotX(rig.armL, -80f, dt);
                    SetRotX(rig.armR, -60f, dt);
                    break;

                case AIState.Windup:
                    FaceTarget(dt, 5f);
                    AnimateWalk(0f, dt);
                    SetRotX(rig.armR, -170f, dt, 10f);
                    stateTimer -= dt;
                    if (stateTimer <= 0f)
                    {
                        if (TargetInArc(attackRange + 0.5f, 110f)) HitTarget(damage, 11f);
                        GM.Audio.Play(Sfx.Swing, 0.5f, 0.2f);
                        state = AIState.Recover;
                        stateTimer = 0.7f;
                        attackCooldown = Random.Range(1.4f, 2.0f);
                    }
                    break;

                case AIState.Recover:
                    SetRotX(rig.armR, -30f, dt, 25f);
                    SetRotX(rig.armL, -30f, dt); // hạ khiên => sơ hở
                    stateTimer -= dt;
                    if (stateTimer <= 0f) state = AIState.Chase;
                    break;
            }
        }
    }
}
