using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Cell : MonoBehaviour
{
    public int energy = 1;

    bool bonus;
    Material my_material;

    public void Hit(RaycastHit hitInfo, bool unstoppable)
    {
        var b = FindObjectOfType<PongPadBuilder>();
        var ps = b.hitPS;
        var rend = GetComponent<MeshRenderer>();
        if (my_material == null)
            my_material = rend.sharedMaterial;
        var color = my_material.color;

        for (int i = 0; i < 20; i++)
            ps.Emit(hitInfo.point, hitInfo.normal + (Random.onUnitSphere * 0.5f),
                0.1f, Random.Range(0.2f, 0.5f), color);

        rend.sharedMaterial = b.cellHitMaterial;
        StartCoroutine(_Hit(unstoppable));
    }

    IEnumerator _Hit(bool unstoppable)
    {
        yield return new WaitForSeconds(0.05f);
        bonus |= Random.Range(0, 4) == 3;
        energy -= 1;
        if (energy <= 0 || unstoppable)
        {
            Destroy(gameObject);
            if (bonus)
                Bonus.AddBonus(transform.position);
        }
        else
            GetComponent<MeshRenderer>().sharedMaterial = my_material;
    }

    Vector3 last_pos;
    Quaternion last_rot;
    float last_move_time;

    public void MoveAndRotateTo(Vector3 pos, Quaternion rot)
    {
        if (last_move_time != Time.time)
        {
            last_move_time = Time.time;
            last_pos = transform.position;
            last_rot = transform.rotation;
        }
        transform.SetPositionAndRotation(pos, rot);
    }

    public Vector3 LastSpeedOnPoint(Vector3 point)
    {
        if (last_move_time != Time.time)
            return Vector3.zero;

        Vector3 v = point - transform.position;
        Vector3 v2 = transform.rotation * Quaternion.Inverse(last_rot) * v;
        return (v2 - v + transform.position - last_pos) / Time.deltaTime;
    }
}
