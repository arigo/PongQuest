using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HexCell : Cell
{
    public bool hasDirection;
    public float blinkSpeed;

    private void Start()
    {
        if (blinkSpeed > 0)
            StartCoroutine(BlinkDirection());
    }

    static Dictionary<Material, Material> private_mat_copy = new Dictionary<Material, Material>();

    IEnumerator BlinkDirection()
    {
        var rend = GetComponent<MeshRenderer>();
        var original_mat = rend.sharedMaterial;
        int name_id = Shader.PropertyToID("_EmissionColor");
        var bright_color = original_mat.GetColor(name_id);

        if (!private_mat_copy.TryGetValue(original_mat, out var copy_mat))
        {
            copy_mat = rend.material;   /* make a copy */
            private_mat_copy[original_mat] = copy_mat;
        }
        rend.sharedMaterial = copy_mat;

        while (true)
        {
            float t = Mathf.Sin(Time.time * blinkSpeed);
            copy_mat.SetColor(name_id, Color.Lerp(Color.black, bright_color, t));
            yield return null;
        }
    }

    public override bool IgnoreHit(Vector3 point, bool unstoppable)
    {
        if (hasDirection && !unstoppable)
        {
            point = transform.InverseTransformPoint(point);
            return point.z < 0.04f;
        }
        return false;
    }

    public override float VelocityBoostSpeed() => 2.5f;
}
