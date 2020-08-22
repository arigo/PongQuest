using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class PongPad : MonoBehaviour, IPongPad
{
    const int PAD_LAYER = 8;

    public Collider myCollider;

    internal Controller controller;
    internal IBall most_recent_iball_hit;
    Vector3 previous_position;
    float previous_time;

    internal static List<IBall> all_balls = new List<IBall>();

    public void StartFollowing(Controller ctrl)
    {
        controller = ctrl;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        previous_position = myCollider.transform.position;
        previous_time = Time.time;
    }

    public static void UpdateAllBalls()
    {
        foreach (var ball in all_balls)
            ball.UpdateBall();
    }

    public void FollowController()
    {
        Vector3 old_position = previous_position;
        Vector3 current_velocity = (myCollider.transform.position - previous_position) / (Time.time - previous_time);
        previous_position = myCollider.transform.position;
        previous_time = Time.time;

        int i = 0;
        while (i < all_balls.Count)
        {
            var ball = all_balls[i];
            if (ball.IsAlive)
            {
                BounceBall(ball, old_position, current_velocity);
                ++i;
            }
            else
            {
                all_balls[i] = all_balls[all_balls.Count - 1];
                all_balls.RemoveAt(all_balls.Count - 1);
            }
        }
    }

    bool BounceBall(IBall ball, Vector3 old_position, Vector3 current_velocity)
    {
        Vector3 axis = myCollider.transform.up;
        Vector3 relative_velocity = ball.GetVelocity() - current_velocity;
        ball.GetPositions(out Vector3 ball_old_pos, out Vector3 ball_new_pos);
        float side_position = Vector3.Dot(axis, old_position - ball_old_pos);
        float side_movement = Vector3.Dot(axis, relative_velocity);
        if (side_position * side_movement <= 0)
            return false;

        Vector3 pad_movement = previous_position - old_position;
        /* theoretical old position if we assume that the pad remained stationary at its new
         * (current) position */
        Vector3 old0 = ball_old_pos + pad_movement;

        /* Do a cast. */
        float distance;
        Vector3 v = ball_new_pos - old0;
        float max_distance = v.magnitude;
        if (max_distance < 1e-8f)
            return false;

        var hits = Physics.SphereCastAll(old0, ball.GetRadius(), v / max_distance, max_distance,
                                         1 << PAD_LAYER, QueryTriggerInteraction.Ignore);
        for (int i = 0; ; i++)
        {
            if (i >= hits.Length)
                return false;   /* no hit */

            if (hits[i].collider == myCollider)
            {
                distance = hits[i].distance;   /* maybe 0 */
                break;
            }
        }
        Vector3 fixed_pos = Vector3.Lerp(old0, ball_new_pos, distance / max_distance);

        /* now change the velocity of the ball */
        relative_velocity = Vector3.Reflect(relative_velocity, axis);
        relative_velocity -= Mathf.Sign(side_position) * 2f * axis;   /* automatic extra impulse */
        ball.SetPositionAndVelocity(this, fixed_pos, relative_velocity + current_velocity);

        controller.HapticPulse(1000);

        /*ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        //emitParams.applyShapeToPosition = true;
        ParticleSystem ps = ballScene.particleSystem;
        emitParams.applyShapeToPosition = true;
        emitParams.position = ps.transform.InverseTransformPoint(ball.transform.position);
        ps.Emit(emitParams, 20);*/

        most_recent_iball_hit = ball;
        return true;
    }

    /*bool MovingBallTouch(Ball ball, Vector3 c1, Vector3 c2)
    {
        Collider[] colls = Physics.OverlapCapsule(c1, c2, ball.GetRadius(),
            1 << PAD_LAYER, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < colls.Length; i++)
            if (colls[i] == my_collider)
                return true;
        return false;
    }*/

    void IPongPad.DestroyPad()
    {
        DestroyImmediate((GameObject)gameObject);
    }
}
