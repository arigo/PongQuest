using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HexCell : Cell
{
    public bool hasDirection;
    public float blinkSpeed, blinkDelta;

    internal bool ignore_hits;   /* set sometimes on the final big hex cell */

    internal float fraction_of_original = 1f;
    protected override float GetCellFraction() => fraction_of_original;

    private void Start()
    {
        if (blinkSpeed > 0)
            StartCoroutine(BlinkDirection());
    }

    static Dictionary<Material, Material> private_mat_copy = new Dictionary<Material, Material>();

    IEnumerator BlinkDirection()
    {
        var rend = GetComponent<MeshRenderer>();
        int name_id = Shader.PropertyToID("_EmissionColor");
        var bright_color = rend.sharedMaterial.GetColor(name_id);
        var pb = new MaterialPropertyBlock();

        while (true)
        {
            float t = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed - blinkDelta));
            pb.SetColor(name_id, Color.Lerp(Color.black, bright_color, t));
            rend.SetPropertyBlock(pb);
            yield return null;
        }
    }

    public override bool IgnoreHit(Vector3 velocity, bool unstoppable)
    {
        if (hasDirection && !unstoppable)
        {
            velocity = transform.InverseTransformVector(velocity);
            return velocity.z >= 0f;
        }
        return ignore_hits;
    }

    public override float VelocityBoostSpeed() => 3.1f;

    public override Vector3 LastSpeedOnPoint(Vector3 point)
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            return rb.GetPointVelocity(point);
        else
            return Vector3.zero;
    }
}
