using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IBall
{
    Vector3 GetVelocity();
    void GetPositions(out Vector3 old_pos, out Vector3 new_pos);
    float GetRadius();
    void SetPositionAndVelocity(PongPad pad, Vector3 position, Vector3 velocity);
    bool IsAlive { get; }
    void UpdateBall();
}

public class Ball : MonoBehaviour, IBall
{
    internal const int LAYER_WALLS = 9;
    internal const int LAYER_CELLS = 10;
    internal const int LAYER_HALOS = 11;
    const float SPEED_LIMIT = 1.3f;
    const float SPEED_EXPONENT = -1.5f;
    const float SPEED_UPPER_LIMIT = 23f;
    const float MIN_Z = 2.96f - 3.842f + 0.32f / 2;

    float radius, rot_speed;
    Vector3 velocity, old_position;
    Vector3 rotation_axis;
    float unstoppable_until;
    Material original_material_before_unstoppable;
    bool shot;
    Ball duplicate_from;

    internal static List<Vector3> start_positions;

    void Start()
    {
        radius = transform.lossyScale.y * 0.5f;
        Vector3 start_position;
        if (duplicate_from == null)
        {
            start_position = transform.position;
        }
        else
        {
            /* this runs possibly one frame later than Duplicate().  Make sure the two balls are
             * at the exact same position now, and tweak the velocities */
            start_position = duplicate_from.transform.position;
            var delta = Random.onUnitSphere * SPEED_LIMIT * 0.25f;
            velocity = duplicate_from.velocity - delta;
            duplicate_from.velocity += delta;
        }
        RestoreStartPosition(start_position);
        PongPad.all_balls.Add(this);
    }

    bool IsRegularBall { get => !shot; }

    void RestoreStartPosition(Vector3 start_position)
    {
        EndUnstoppable();
        old_position = start_position;
        transform.position = start_position;
        transform.rotation = Random.rotationUniform;
        rotation_axis = Random.onUnitSphere;
        rot_speed = 45f;
    }

    public void Duplicate()
    {
        EndUnstoppable();
        var clone = Instantiate(this);
        clone.transform.position = transform.position;
        clone.velocity = velocity;
        clone.duplicate_from = this;
    }

    public void Unstoppable()
    {
        unstoppable_until = Time.time + 10f;
        if (original_material_before_unstoppable == null)
            original_material_before_unstoppable = GetComponent<MeshRenderer>().sharedMaterial;
        GetComponent<MeshRenderer>().sharedMaterial = PongPadBuilder.instance.unstoppableBallMaterial;
    }

    public void ShootOutOf(PongPad pad)
    {
        shot = true;
        transform.position = pad.transform.position + pad.transform.forward * 0.05f;
        velocity = pad.transform.forward * 14f;

        StartCoroutine(_UpdateShot());
    }

    IEnumerator _UpdateShot()
    {
        while (true)
        {
            yield return null;
            UpdateBall();
        }
    }

    void EndUnstoppable()
    {
        if (original_material_before_unstoppable != null)
        {
            GetComponent<MeshRenderer>().sharedMaterial = original_material_before_unstoppable;
            original_material_before_unstoppable = null;
        }
    }

    bool IsUnstoppable => original_material_before_unstoppable != null;

    bool TryRespawnPosition(out Vector3 start_pos)
    {
        Debug.Assert(start_positions.Count > 0);
        float[] min_distances = new float[start_positions.Count];
        for (int i = 0; i < min_distances.Length; i++)
            min_distances[i] = float.PositiveInfinity;

        int count = 0;
        foreach (var iball in PongPad.all_balls)
            if (iball is Ball ball && iball.IsAlive && ball.IsRegularBall)
            {
                count += 1;
                for (int i = 0; i < min_distances.Length; i++)
                {
                    float dist1 = Vector3.Distance(ball.transform.position, start_positions[i]);
                    if (dist1 < min_distances[i])
                        min_distances[i] = dist1;
                }
            }
        start_pos = Vector3.zero;
        if (count > start_positions.Count)
            return false;

        float max_distance = -1f;
        for (int i = 0; i < min_distances.Length; i++)
            if (min_distances[i] > max_distance)
            {
                max_distance = min_distances[i];
                start_pos = start_positions[i];
            }
        return true;
    }

    public void UpdateBall()
    {
        if (!this || !gameObject || PongPadBuilder.paused)
            return;

        if (old_position.z < MIN_Z || old_position.sqrMagnitude > 10f * 10f)
        {
            if (!IsRegularBall || !TryRespawnPosition(out Vector3 start_pos))
            {
                Destroy((GameObject)gameObject);
                return;
            }
            velocity = Vector3.zero;
            RestoreStartPosition(start_pos);
        }

        if (IsUnstoppable && Time.time >= unstoppable_until)
            EndUnstoppable();

        float dt = Time.deltaTime;
        float speed_reduction = Mathf.Exp(dt * SPEED_EXPONENT);

        float vmag = velocity.magnitude;
        if (vmag < 1e-5)
        {
            velocity = new Vector3(Random.value - 0.5f, (Random.value - 0.5f) * 0.6f, -0.7f) * 0.01f;
            vmag = velocity.magnitude;
        }
        float speed = vmag;
        if (speed > SPEED_UPPER_LIMIT)
            speed = SPEED_UPPER_LIMIT;

        speed = (speed - SPEED_LIMIT) * speed_reduction + SPEED_LIMIT;
        velocity *= speed / vmag;

        if (Mathf.Abs(velocity.z) < Mathf.Min(SPEED_LIMIT, velocity.magnitude) * 0.42f)
            velocity.z += dt * (velocity.z >= 0f ? 0.5f : -0.35f);

        foreach (var collider in Physics.OverlapSphere(transform.position, radius,
                                    1 << LAYER_HALOS, QueryTriggerInteraction.Collide))
        {
            var center = collider.transform.position;
            float factor = IsUnstoppable ? 0.62f : 13.5f;
            velocity += (transform.position - center).normalized * (dt * factor);
            collider.GetComponentInParent<Halo>().Pong();
        }

        old_position = transform.position;
        float move = velocity.magnitude * dt;
        Vector3 dir = velocity.normalized;

        var hits = Physics.SphereCastAll(new Ray(transform.position, dir), radius, move,
            (1 << LAYER_WALLS) | (1 << LAYER_CELLS), QueryTriggerInteraction.Ignore);

        if (hits.Length > 0)
        {
            var all_hits = new List<RaycastHit>();
            all_hits.AddRange(hits);
            all_hits.Sort((h1, h2) => h1.distance.CompareTo(h2.distance));

            foreach (var hitInfo1 in all_hits)
            {
                var hitInfo = hitInfo1;

                if (hitInfo.distance == 0)
                {
                    hitInfo.point = hitInfo.collider.ClosestPoint(transform.position);
                    if (Vector3.Distance(hitInfo.point, transform.position) < 1e-5f)
                    {
                        hitInfo.normal = -dir;
                    }
                    else
                    {
                        hitInfo.normal = (transform.position - hitInfo.point).normalized;
                    }
                }

                var cell = hitInfo.collider.GetComponent<Cell>();

                Vector3 cell_speed = cell != null ? cell.LastSpeedOnPoint(hitInfo.point) : Vector3.zero;
                if (Vector3.Dot(velocity - cell_speed, hitInfo.normal) >= 0f)
                    continue;

                bool done;
                if (!shot)
                {
                    if (cell == null || !IsUnstoppable)
                    {
                        transform.position += dir * hitInfo.distance;

                        velocity = Vector3.Reflect(velocity - cell_speed, hitInfo.normal) + cell_speed;

                        rotation_axis = Random.onUnitSphere;
                        rot_speed = Random.Range(15f, 270f);
                        done = true;
                    }
                    else
                    {
                        done = false;
                    }
                }
                else
                {
                    Destroy((GameObject)gameObject);
                    done = true;
                }

                if (cell != null)
                    cell.Hit(hitInfo, IsUnstoppable);

                if (done)
                    return;
            }
        }
        transform.position += dir * move;
        transform.rotation = Quaternion.AngleAxis(rot_speed * dt, rotation_axis) * transform.rotation;
    }

    public Vector3 GetVelocity()
    {
        return velocity;
    }

    public void SetPositionAndVelocity(PongPad pad, Vector3 p, Vector3 v)
    {
        transform.position = p;
        velocity = v;
        rot_speed *= 0.5f;
    }

    public void GetPositions(out Vector3 old_pos, out Vector3 new_pos)
    {
        old_pos = old_position;
        new_pos = transform.position;
    }

    public float GetRadius()
    {
        return radius;
    }

    bool IBall.IsAlive { get => this && !shot; }
}
