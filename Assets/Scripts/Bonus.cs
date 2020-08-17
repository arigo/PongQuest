using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Bonus : MonoBehaviour, IBall
{
    const float X_DELTA_MAX = 0.8595f;
    const float Y_MIN = 0.179f;
    const float Y_MAX = 1.58f;

    public static void AddBonus(Vector3 position)
    {
        var bgo = new GameObject("bonus");
        bgo.transform.position = position;

        switch (Random.Range(0, 3))
        {
            case 0: bgo.AddComponent<DoubleBallBonus>(); break;
            case 1: bgo.AddComponent<UnstoppableBallBonus>(); break;
            default: bgo.AddComponent<LaserBonus>(); break;
        }
    }

    protected abstract Color GetColor();
    protected abstract int GetPoints();

    Vector3 velocity, old_pos, new_pos;
    ParticleSystem starPS;
    bool free_falling;

    private void Start()
    {
        starPS = PongPadBuilder.instance.starPS;
        velocity = new Vector3(0, 0, -1);
        old_pos = transform.position;
        new_pos = transform.position;
        free_falling = true;
        StartCoroutine(FreeFalling());
        PongPad.all_balls.Add(this);
    }

    IEnumerator FreeFalling()
    {
        float emit_stars = 0;

        while (true)
        {
            old_pos = new_pos;
            if (old_pos.z < -2.5f)
            {
                Destroy(gameObject);
                yield break;
            }
            float dt = Time.deltaTime;

            if (Mathf.Abs(new_pos.x) > X_DELTA_MAX && velocity.x * new_pos.x > 0f)
                velocity.x = -velocity.x;

            if ((velocity.y < 0f && new_pos.y < Y_MIN) || (velocity.y > 0f && new_pos.y > Y_MAX))
                velocity.y = -velocity.y;

            transform.position = new_pos = old_pos + velocity * dt;

            Vector2 v2 = (Vector2)velocity;
            v2 += dt * (Vector2)Random.onUnitSphere * 3f;
            v2 *= Mathf.Exp(-0.2f * dt);
            velocity.x = v2.x;
            velocity.y = v2.y;

            emit_stars += dt * 40f;
            if (emit_stars >= 1f)
            {
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = transform.position,
                    startColor = GetColor(),
                };
                starPS.Emit(emitParams, (int)emit_stars);
                emit_stars -= (int)emit_stars;
            }

            yield return null;
        }
    }

    Vector3 IBall.GetVelocity()
    {
        return velocity;
    }

    void IBall.GetPositions(out Vector3 old_pos, out Vector3 new_pos)
    {
        old_pos = this.old_pos;
        new_pos = this.new_pos;
    }

    float IBall.GetRadius()
    {
        return 0.06f;
    }

    void IBall.SetPositionAndVelocity(PongPad pad, Vector3 position, Vector3 velocity)
    {
        StopAllCoroutines();
        free_falling = false;
        Hit(pad);
        Points.AddPoints(transform.position, GetColor(), GetPoints());
    }

    bool IBall.IsAlive { get => this && free_falling; }

    protected abstract void Hit(PongPad pad);

    class BonusAttachedToPad
    {
        public Bonus bonus;
    }
    static BonusAttachedToPad[] bonus_attached_to_pad;

    protected void AttachToPad(PongPad pad, System.Action<Ball> hit_ball)
    {
        var ba2p = pad.controller.GetAdditionalData(ref bonus_attached_to_pad);
        if (ba2p.bonus)
            Destroy(ba2p.bonus.gameObject);
        ba2p.bonus = this;
        pad.controller.HapticPulse(400);
        if (hit_ball != null)
            StartCoroutine(WaitForBallHit(pad, hit_ball));
    }

    protected bool IsAttachedToPad(PongPad pad)
    {
        var ba2p = pad.controller.GetAdditionalData(ref bonus_attached_to_pad);
        return ba2p.bonus == this;
    }

    IEnumerator WaitForBallHit(PongPad pad, System.Action<Ball> hit_ball)
    {
        pad.most_recent_iball_hit = null;

        float emit_stars = 0;

        while (true)
        {
            while (PongPadBuilder.paused)
                yield return null;

            if (pad.most_recent_iball_hit is Ball most_recent_ball && most_recent_ball)
            {
                hit_ball(most_recent_ball);
                Destroy(gameObject);
                yield break;
            }

            float dt = Time.deltaTime;
            emit_stars += dt * 20f;
            if (emit_stars >= 1f)
            {
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = pad.transform.position + Random.onUnitSphere * 0.07f,
                    startColor = GetColor(),
                };
                starPS.Emit(emitParams, (int)emit_stars);
                emit_stars -= (int)emit_stars;
            }
            yield return null;
        }
    }

    void IBall.UpdateBall()
    {
    }

    public static void RemoveAllBonuses()
    {
        foreach (var bonus in FindObjectsOfType<Bonus>())
            Destroy(bonus.gameObject);
    }
}

public class DoubleBallBonus : Bonus
{
    protected override Color GetColor() => new Color(0f, 1f, 0f);
    protected override int GetPoints() => 300;

    protected override void Hit(PongPad pad)
    {
        void HitBall(Ball ball)
        {
            ball.Duplicate();
        }
        AttachToPad(pad, HitBall);
    }
}

public class UnstoppableBallBonus : Bonus
{
    protected override Color GetColor() => new Color(0f, 1f, 1f);
    protected override int GetPoints() => 350;

    protected override void Hit(PongPad pad)
    {
        void HitBall(Ball ball)
        {
            ball.Unstoppable();
        }
        AttachToPad(pad, HitBall);
    }
}

public class LaserBonus : Bonus
{
    protected override Color GetColor() => new Color(1f, 0f, 0f);
    protected override int GetPoints() => 200;

    protected override void Hit(PongPad pad)
    {
        AttachToPad(pad, null);
        StartCoroutine(ShootLaser(pad));
    }

    IEnumerator ShootLaser(PongPad pad)
    {
        float next_shot = 0;
        int remaining_shots = 20;
        var prefab = PongPadBuilder.instance.shotBallPrefab;

        while (IsAttachedToPad(pad) && remaining_shots > 0)
        {
            float dt = Time.deltaTime;
            next_shot += dt * 5f;
            if (next_shot >= 1f)
            {
                next_shot -= 1f;
                remaining_shots -= 1;
                Instantiate(prefab).ShootOutOf(pad);
            }
            yield return null;
            while (PongPadBuilder.paused)
                yield return null;
        }
        Destroy(gameObject);
    }
}
