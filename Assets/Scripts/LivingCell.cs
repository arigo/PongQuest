using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LivingCell : Cell
{
    const int GUIDE_LAYER = 13;

    public float movingVelocity = 0.07f;

    float wobble, wobble_speed, base_scale;
    Quaternion target_rotation;
    Vector3? last_moving_target;
    float fraction_of_original = 1f;
    float moving_velocity;
    LivingCell copied_from_cell;

    void Start()
    {
        wobble = Random.Range(0, 2 * Mathf.PI);
        wobble_speed = Random.Range(2.7f, 3.0f);
        base_scale = transform.localScale.y;
        moving_velocity = movingVelocity * Random.Range(0.9f, 1.1f);
        target_rotation = transform.rotation;

        if (FindTrackCollider(out Collider track) || copied_from_cell != null)
        {
            if (copied_from_cell != null)
            {
                wobble = copied_from_cell.wobble;
                base_scale = copied_from_cell.base_scale;

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

    protected override float GetCellFraction() => fraction_of_original;

    protected override void NonFatalHit()
    {
        if (last_moving_target != null)
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
        if (wobble >= 0f)
            wobble -= 2 * Mathf.PI;
        wobble += Time.deltaTime * wobble_speed;

        float factor = 1f + Mathf.Sin(wobble) * 0.13f;
        transform.localScale = new Vector3(base_scale, base_scale, base_scale * factor);

        if (last_moving_target != null)
        {
            float t = Mathf.Exp(-Time.deltaTime * 0.7f);
            var rot = Quaternion.Lerp(target_rotation, transform.rotation, t);
            transform.rotation = rot;
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
}
