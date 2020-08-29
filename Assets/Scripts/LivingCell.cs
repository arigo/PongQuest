using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LivingCell : Cell
{
    const int GUIDE_LAYER = 13;

    public float movingVelocity = 0.07f;
    public bool allowFlee, allowCocoon;
    public LivingCell[] spawnCellPrefabs;
    public Vector3[] spawnCellOrientation;

    float wobble, wobble_speed, base_scale;
    Quaternion target_rotation;
    Vector3? last_moving_target;
    float fraction_of_original = 1f;
    float moving_velocity;
    LivingCell copied_from_cell;
    bool cocoon_mode;
    float full_energy;

    static float dying_cell_time;
    static Vector3 dying_cell_location;

    void Start()
    {
        dying_cell_time = -99;

        wobble = Random.Range(0, 2 * Mathf.PI);
        wobble_speed = Random.Range(2.7f, 3.0f);

        base_scale = transform.localScale.y;
        moving_velocity = InitialMovingVelocity();
        target_rotation = transform.rotation;
        full_energy = energy;

        if (finalBigCell)
        {
            Debug.Assert(copied_from_cell == null);
            wobble_speed *= 0.5f;
        }
        else if (FindTrackCollider(out Collider track) || copied_from_cell != null)
        {
            if (copied_from_cell != null)
            {
                wobble = copied_from_cell.wobble;
                base_scale = copied_from_cell.base_scale;
                full_energy = copied_from_cell.full_energy;

                copied_from_cell.fraction_of_original *= 0.5f;
                fraction_of_original = copied_from_cell.fraction_of_original;

                var tweak_rot = Quaternion.Euler(25f, 0f, 0f);
                target_rotation *= tweak_rot;
                transform.rotation = target_rotation;
                copied_from_cell.target_rotation *= Quaternion.Inverse(tweak_rot);
                copied_from_cell.transform.rotation = copied_from_cell.target_rotation;

                last_moving_target = copied_from_cell.last_moving_target;
            }
            else
                last_moving_target = transform.position;

            StartCoroutine(AdjustRotation(track));
        }
    }

    float InitialMovingVelocity() => movingVelocity * Random.Range(0.9f, 1.1f);

    protected override bool IgnoreHit(Vector3 point, float subtract_energy)
    {
        if (finalBigCell && transform.childCount > 0)
            return true;
        return cocoon_mode;
    }
    protected override float GetCellFraction() => fraction_of_original;
    public override float VelocityBoostSpeed() => 1.75f;
    public override bool VelocityBoostCube() => false;

    static Dictionary<System.Tuple<Material, int>, Material> _lower_energy_mats = new Dictionary<System.Tuple<Material, int>, Material>();

    protected override void ChangeMaterial(Material mat = null)
    {
        if (mat == null && energy > 0 && energy < full_energy)
        {
            int fraction = Mathf.RoundToInt(Mathf.Clamp((energy / full_energy) * 16f, 1, 15));
            var key = System.Tuple.Create(MyMaterial, fraction);
            if (!_lower_energy_mats.TryGetValue(key, out mat))
            {
                mat = new Material(MyMaterial)
                {
                    color = MyMaterial.color * Mathf.Pow(fraction / 16f, 0.75f),
                };
                _lower_energy_mats[key] = mat;
            }
        }
        base.ChangeMaterial(mat);
    }

    protected override void GotHit(CellHitInfo info)
    {
        if (info.fatal)
        {
            dying_cell_time = Time.time;
            dying_cell_location = transform.position;
        }
        else if (spawnCellPrefabs.Length > 0)
        {
            int spawn_count = spawnCellOrientation.Length;
            Debug.Assert(spawn_count > 0);
            Debug.Assert(finalBigCell);

            StartCoroutine(PassThroughCocoonMode(spawnCellPrefabs.Length == 1 ? 2.2f : 11f));

            void SpawnNewCell(LivingCell prefab, Quaternion orientation)
            {
                var spawn = Instantiate(prefab, transform.parent);
                var q = Quaternion.LookRotation(info.hit_point - transform.position);
                q = q * orientation;
                float dist = 0.055f + 0.21f - spawn.transform.localScale.y;
                var p = transform.position + dist * (q * Vector3.forward);
                spawn.transform.SetPositionAndRotation(p, q);
                spawn.fraction_of_original = 1f / (full_energy * spawn_count + 1);
                spawn.points = Mathf.RoundToInt(spawn.points / spawn.fraction_of_original);
            }

            for (int i = Mathf.RoundToInt(energy); i < Mathf.RoundToInt(info.prev_energy); i++)
            {
                int j = i % spawnCellPrefabs.Length;
                j = spawnCellPrefabs.Length - 1 - j;
                var prefab = spawnCellPrefabs[j];
                foreach (var orientation in spawnCellOrientation)
                    SpawnNewCell(prefab, Quaternion.LookRotation(orientation));
            }
        }
        else if (last_moving_target != null)
        {
            var copy = Instantiate(this, transform.parent);
            copy.copied_from_cell = this;
        }
    }

    const float TRACK_WIDTH = 0.05f;

    bool FindTrackCollider(out Collider track)
    {
        var colliders = Physics.OverlapSphere(transform.position, TRACK_WIDTH,
            1 << GUIDE_LAYER, QueryTriggerInteraction.Collide);

        if (colliders.Length > 0)
        {
            track = colliders[Random.Range(0, colliders.Length)];
            return true;
        }
        else
        {
            track = null;
            return false;
        }
    }

    private void Update()
    {
        if (!cocoon_mode)
        {
            if (wobble >= 0f)
                wobble -= 2 * Mathf.PI;
            wobble += Time.deltaTime * wobble_speed;

            float factor = 1f + Mathf.Sin(wobble) * 0.13f;
            transform.localScale = new Vector3(base_scale, base_scale, base_scale * factor);
        }

        if (finalBigCell)
        {
            transform.rotation = Quaternion.Euler(0f, Time.time * 30f, 0f);
        }
        else if (last_moving_target != null)
        {
            if (!cocoon_mode)
            {
                float t = Mathf.Exp(-Time.deltaTime * 0.7f);
                var rot = Quaternion.Lerp(target_rotation, transform.rotation, t);
                transform.rotation = rot;
            }
            transform.position += transform.forward * (Time.deltaTime * moving_velocity);
        }
    }

    IEnumerator AdjustRotation(Collider follow_track = null)
    {
        var wait = new WaitForSeconds(Random.Range(0.3f, 0.4f));
        Debug.Assert(last_moving_target != null);

        while (true)
        {
            yield return wait;

            if (Mathf.Abs(Time.time - dying_cell_time) < 0.8f &&
                (transform.position - dying_cell_location).sqrMagnitude < 0.45f)
            {
                if (allowFlee)
                {
                    var flee_direction = transform.position - dying_cell_location;
                    flee_direction.Normalize();   /* may be zero */

                    for (int i = 0; i < 60; i++)
                    {
                        Vector3 direction = flee_direction + 1.6f * Random.insideUnitSphere;
                        if (Physics.Raycast(transform.position, direction,
                            out var hitInfo, 2.5f,
                            1 << GUIDE_LAYER, QueryTriggerInteraction.Collide))
                        {
                            target_rotation = Quaternion.LookRotation(direction);
                            transform.rotation = target_rotation;
                            float source_moving_velocity = movingVelocity * 4f;
                            float target_moving_velocity = InitialMovingVelocity();
                            float fast_distance = hitInfo.distance;
                            while (fast_distance > 0f)
                            {
                                float remaining_fraction = fast_distance / hitInfo.distance;
                                moving_velocity = Mathf.Lerp(
                                    target_moving_velocity,
                                    source_moving_velocity,
                                    Mathf.Pow(remaining_fraction, 0.3f));
                                yield return null;
                                fast_distance -= Time.deltaTime * moving_velocity;
                            }
                            moving_velocity = target_moving_velocity;
                            follow_track = hitInfo.collider;
                            goto normal_behavior;
                        }
                    }
                }
                if (allowCocoon)
                    yield return PassThroughCocoonMode(Random.Range(4f, 9f));
            }

          normal_behavior:
            const float RANGE = 0.1f;
            var rot = target_rotation;
            rot.x += Random.Range(-RANGE, RANGE);
            rot.y += Random.Range(-RANGE, RANGE);
            rot.z += Random.Range(-RANGE, RANGE);
            rot.w += Random.Range(-RANGE, RANGE);
            rot.Normalize();
            target_rotation = rot;

            Vector3 position = transform.position;
            Vector3 closest_point;
            if (follow_track != null)
                closest_point = follow_track.ClosestPoint(position);
            else
                closest_point = last_moving_target.Value;

            Vector3 target_vector = closest_point - position;
            if (target_vector.sqrMagnitude <= TRACK_WIDTH * TRACK_WIDTH)
            {
                last_moving_target = closest_point;
                continue;
            }

            /* we're exiting the zone of 'follow_track'.  Is there another collider we're on? */
            if (FindTrackCollider(out follow_track))
            {
                /* yes, move on */
                continue;
            }

            /* Steer back to the last moving target */
            target_rotation = Quaternion.LookRotation(last_moving_target.Value - position, transform.up);
        }
    }

    IEnumerator PassThroughCocoonMode(float time)
    {
        cocoon_mode = true;
        ChangeMaterial(PongPadBuilder.instance.cocoonMaterial);
        transform.localScale = Vector3.one * (base_scale * 0.9f);
        yield return new WaitForSeconds(time);
        cocoon_mode = false;
        ChangeMaterial();
        transform.localScale = Vector3.one * base_scale;
    }
}
