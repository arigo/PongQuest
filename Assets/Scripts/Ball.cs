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
    internal const float SPEED_LIMIT = 1.3f;
    const float SPEED_EXPONENT = -1.5f;
    const float SPEED_UPPER_LIMIT = 23f;
    const float MIN_Z = 2.96f - 3.842f + 0.32f / 2;   // -0.722
    const float EPISODE3_CENTER = 2.401781f;

    float radius, initial_radius, rot_speed;
    Vector3 velocity, old_position;
    Vector3 rotation_axis;
    float unstoppable_until;
    Material original_material;
    bool shot, respawning, growing;
    float inflation_bonuses;
    Ball duplicate_from;

    internal static List<Vector3> start_positions;

    void Start()
    {
        Vector3 start_position;
        if (duplicate_from == null)
        {
            initial_radius = transform.localScale.y * 0.5f;
            start_position = transform.position;
            inflation_bonuses = 0;
        }
        else
        {
            /* this runs possibly one frame later than Duplicate().  Make sure the two balls are
             * at the exact same position now, and tweak the velocities */
            Debug.Assert(initial_radius != 0);
            start_position = duplicate_from.transform.position;
            var delta = Random.onUnitSphere * SPEED_LIMIT * 0.25f;
            velocity = duplicate_from.velocity - delta;
            duplicate_from.velocity += delta;
        }
        RestoreStartPosition(start_position, inflation_bonuses);
        PongPad.all_balls.Add(this);
    }

    bool IsRegularBall { get => !shot; }

    static readonly Vector3 TWO = new Vector3(2, 2, 2);

    float GetRadiusForInflationBonuses(float inflation_bonuses)
    {
        /* the ball's apparent surface is increased by a factor 'inflation_bonuses + 1' */
        return initial_radius * Mathf.Sqrt(inflation_bonuses + 1);
    }

    static float wait_for_next_respawn;

    void RestoreStartPosition(Vector3 start_position, float inflation_bonuses = 0f)
    {
        EndUnstoppable();
        old_position = start_position;
        radius = initial_radius;
        this.inflation_bonuses = inflation_bonuses;
        transform.position = start_position;
        transform.rotation = Random.rotationUniform;
        transform.localScale = TWO * GetRadiusForInflationBonuses(inflation_bonuses);
        rotation_axis = Random.onUnitSphere;
        rot_speed = 45f;

        if (velocity == Vector3.zero)
        {
            if (PongPadBuilder.instance.transformSpaceBase != null)
            {
                var tr = PongPadBuilder.instance.transformSpaceBase;
                velocity = tr.forward * -0.2f;
            }
            else if (Time.time < wait_for_next_respawn)
            {
                respawning = true;
                StartCoroutine(_DoneWaitingForABit(wait_for_next_respawn - Time.time));
            }
            wait_for_next_respawn = Mathf.Max(wait_for_next_respawn, Time.time) + 1f;
        }
    }

    IEnumerator _DoneWaitingForABit(float delay)
    {
        yield return new WaitForSeconds(delay);
        respawning = false;
    }

    public void Duplicate()
    {
        EndUnstoppable();

        var clone = Instantiate(this);
        clone.transform.position = transform.position;
        clone.initial_radius = initial_radius;
        clone.velocity = velocity;
        clone.duplicate_from = this;

        if (inflation_bonuses > 0f)
        {
            /* reduce by half each ball's inflation bonus */
            inflation_bonuses *= 0.5f;
            transform.localScale = TWO * GetRadiusForInflationBonuses(inflation_bonuses);
            clone.inflation_bonuses = inflation_bonuses;
            clone.transform.localScale = TWO * GetRadiusForInflationBonuses(inflation_bonuses);
        }
    }

    public void Unstoppable()
    {
        unstoppable_until = Time.time + 10f;
        AdjustMaterial();
    }

    public void BiggerBall()
    {
        inflation_bonuses += 1f;
        StartCoroutine(_InflateBall(inflation_bonuses));
    }

    IEnumerator _InflateBall(float inflation_bonuses)
    {
        float target_radius = GetRadiusForInflationBonuses(inflation_bonuses);

        while (true)
        {
            if (!growing)
            {
                growing = true;
                AdjustMaterial();
            }

            yield return null;
            if (radius == target_radius || inflation_bonuses != this.inflation_bonuses)
                break;

            radius = Mathf.MoveTowards(radius, target_radius, Time.deltaTime * 0.1f);
            transform.localScale = TWO * radius;
        }
        growing = false;
        AdjustMaterial();
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

    bool IsUnstoppable => unstoppable_until != 0f;

    void AdjustMaterial()
    {
        if (original_material == null)
            original_material = GetComponent<MeshRenderer>().sharedMaterial;

        Material mat;
        if (IsUnstoppable)
            mat = PongPadBuilder.instance.unstoppableBallMaterial;
        else if (growing)
            mat = PongPadBuilder.instance.growingBallMaterial;
        else
            mat = original_material;
        GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    void EndUnstoppable()
    {
        if (unstoppable_until != 0f)
        {
            unstoppable_until = 0f;
            AdjustMaterial();
        }
    }

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

    IEnumerator RespawnAfterDelay()
    {
        respawning = true;
        EndUnstoppable();
        var color = GetComponent<MeshRenderer>().sharedMaterial.color;
        color = Color.Lerp(color, Color.white, 0.7f);
        Cell.EmitHitPS(transform.position, velocity, color);

        transform.position = old_position = 1024 * Vector3.down;
        velocity = Vector3.zero;

        yield return new WaitForSeconds(2.0f);
        while (growing)
            yield return null;
        respawning = false;

        /* this return false if there are more than two regular balls alive */
        if (TryRespawnPosition(out Vector3 start_pos))
        {
            velocity = Vector3.zero;
            RestoreStartPosition(start_pos);
            Cell.EmitHitPS(start_pos, Vector3.zero, color);
            Points.AddPoints(start_pos, color, -5000, 1.5f);
        }
        else
            Destroy((GameObject)gameObject);
    }

    public void UpdateBall()
    {
        if (!this || !gameObject || PongPadBuilder.paused || respawning)
            return;

        if (unstoppable_until != 0f && Time.time >= unstoppable_until)
            EndUnstoppable();

        var out_of_bounds = old_position.z < MIN_Z || old_position.sqrMagnitude > 9f * 9f;
        if (PongPadBuilder.instance.episodeNumber == 3)
        {
            float SQRT305 = Mathf.Sqrt(3) * 0.5f;
            float x = old_position.x;
            float z = old_position.z - EPISODE3_CENTER;
            out_of_bounds |= (x * SQRT305 - z * 0.5f) < MIN_Z - EPISODE3_CENTER;
            out_of_bounds |= (x * -SQRT305 - z * 0.5f) < MIN_Z - EPISODE3_CENTER;
        }
        if (out_of_bounds)
        {
            if (!IsRegularBall)
                Destroy((GameObject)gameObject);
            else
                StartCoroutine(RespawnAfterDelay());
            return;
        }

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

        if (PongPadBuilder.instance.episodeNumber != 3)
        {
            if (Mathf.Abs(velocity.z) < Mathf.Min(SPEED_LIMIT, velocity.magnitude) * 0.42f)
                velocity.z += dt * (velocity.z >= 0f ? 0.5f : -0.35f);
        }
        else
        {
            Vector2 h_velocity = new Vector2(velocity.x, velocity.z);
            float h_mag = h_velocity.magnitude;
            if (h_mag < Mathf.Min(SPEED_LIMIT, velocity.magnitude) * 0.42f)
            {
                if (h_mag < 1e-8)
                    h_velocity = Vector2.up;
                else
                    h_velocity /= h_mag;
                h_velocity *= dt * 0.35f;
                velocity += new Vector3(h_velocity.x, 0, h_velocity.y);
            }
        }

        foreach (var collider in Physics.OverlapSphere(transform.position, radius,
                                    1 << LAYER_HALOS, QueryTriggerInteraction.Collide))
        {
            var center = collider.transform.position;
            float factor = IsUnstoppable ? 2.54f : 13.5f;
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
                    if (Vector3.Dot(transform.position - hitInfo.point, velocity) > -1e-5)
                        continue;   /* flying away from the closest point, ignore collision */
                    hitInfo.normal = (transform.position - hitInfo.point).normalized;
                }

                var cell = hitInfo.collider.GetComponent<Cell>();

                Vector3 cell_speed = cell != null ? cell.LastSpeedOnPoint(hitInfo.point) : Vector3.zero;
                if (Vector3.Dot(velocity - cell_speed, hitInfo.normal) >= 0f)
                    continue;

                bool done;
                bool unstoppable = cell != null && IsUnstoppable && !cell.finalBigCell;
                bool ignore = cell != null ? cell.IgnoreHit(velocity, unstoppable) : false;
                if (!shot)
                {
                    if (cell == null || !unstoppable)
                    {
                        transform.position += dir * hitInfo.distance;

                        bool randomly_tweak_velocity = false;
                        float bump_factor = 2f;
                        if (cell == null)
                        {
                            if (PongPadBuilder.instance.episodeNumber == 3)
                                bump_factor = 1.9f;
                            else if (velocity.z < 0f)
                                randomly_tweak_velocity = true;
                        }

                        velocity -= cell_speed;
                        velocity -= hitInfo.normal * (bump_factor * Vector3.Dot(velocity, hitInfo.normal));
                        velocity += cell_speed;

                        /* randomly tweak the velocity at every rebound that is on a wall, and goes
                         * from 'towards' to 'away from' the player */
                        if (randomly_tweak_velocity && velocity.z > 0f)
                            velocity += Random.onUnitSphere * (SPEED_LIMIT / 5f);

                        rotation_axis = Random.onUnitSphere;
                        rot_speed = Random.Range(15f, 270f);

                        if (cell == null)
                        {
                            var column_hit = hitInfo.collider.GetComponent<LevelSet3ColumnHit>();
                            if (column_hit != null)
                                column_hit.Hit(transform.position, ref velocity, hitInfo.normal);
                        }
                        else if (cell.velocityBoost && (PongPadBuilder.instance.episodeNumber != 3 || !ignore))
                        {
                            float boost = cell.VelocityBoostSpeed();
                            float extra = SPEED_LIMIT * boost - velocity.magnitude;
                            if (extra > 0f)
                                velocity += hitInfo.normal * extra;
                            rot_speed *= boost * 0.5f;
                            cell.HitVelocityBoost(cell_speed);
                        }

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

                AudioClip clip = null;
                if (cell != null)
                    cell.Hit(hitInfo, unstoppable ? 9999 : inflation_bonuses + 1, ignore, ref clip);

                if (clip == null)
                    PlayClip(PongPadBuilder.instance.ballBounceSound,
                             Mathf.Sqrt(IsUnstoppable ? 0.5f : (1f / (inflation_bonuses + 1))));
                else
                    PlayClip(clip);

                if (done)
                    return;
            }
        }
        transform.position += dir * move;
        transform.rotation = Quaternion.AngleAxis(rot_speed * dt, rotation_axis) * transform.rotation;
    }

    void PlayClip(AudioClip clip, float pitch = 1f)
    {
        var asrc = GetComponent<AudioSource>();
        if (asrc == null)   /* maybe the ball was destroyed? */
            return;
        asrc.Stop();
        asrc.clip = clip;
        asrc.pitch = pitch;
        asrc.Play();
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

    public static void RemoveAllBalls()
    {
        foreach (var ball in FindObjectsOfType<Ball>())
            Destroy((GameObject)ball.gameObject);
    }
}
