using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LivingCell : Cell
{
    const int GUIDE_LAYER = 13;

    public float movingVelocity = 0.07f;

    float wobble, wobble_speed, base_scale;
    Collider follow_track;
    Quaternion? target_rotation;

    void Start()
    {
        wobble = Random.Range(0, 2 * Mathf.PI);
        wobble_speed = Random.Range(2.7f, 3.0f);
        base_scale = transform.localScale.y;

        if (FindTrackCollider(out follow_track))
        {
            target_rotation = transform.rotation;
            StartCoroutine(AdjustRotation());
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

        if (target_rotation != null)
        {
            float t = Mathf.Exp(-Time.deltaTime * 0.7f);
            var rot = Quaternion.Lerp(target_rotation.Value, transform.rotation, t);
            transform.rotation = rot;
            transform.position += transform.forward * (Time.deltaTime * movingVelocity);
        }
    }

    IEnumerator AdjustRotation()
    {
        var wait = new WaitForSeconds(Random.Range(0.3f, 0.4f));
        while (true)
        {
            yield return wait;

            if (target_rotation != null)
            {
                const float RANGE = 0.1f;
                var rot = target_rotation.Value;
                rot.x += Random.Range(-RANGE, RANGE);
                rot.y += Random.Range(-RANGE, RANGE);
                rot.z += Random.Range(-RANGE, RANGE);
                rot.Normalize();
                target_rotation = rot;
            }

            Vector3 position = transform.position;
            Vector3 closest_point = follow_track.ClosestPoint(position);
            Vector3 target_vector = closest_point - position;
            if (target_vector.sqrMagnitude <= TRACK_WIDTH * TRACK_WIDTH)
                continue;

            /* we're exiting the zone of 'follow_track'.  Is there another collider we're on? */
            if (FindTrackCollider(out Collider track))
            {
                follow_track = track;   /* move on */
                continue;
            }

            /* Steer back to closest_point */
            target_rotation = Quaternion.LookRotation(target_vector, transform.up);
        }
    }
}
