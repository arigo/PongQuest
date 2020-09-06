using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelSet3ColumnHit : MonoBehaviour
{
    const float BOOST = 2.6f;

    static Mesh static_mesh;

    private void Start()
    {
        if (static_mesh == null)
        {
            const float RADIUS = 1.2f;
            const float YD = 0.015f;
            float S = Mathf.Sqrt(3f) * 0.5f;

            static_mesh = VelocityBooster.MakeLinesMesh(new List<Vector3>
            {
                RADIUS * new Vector3( 1.0f, YD, 0),
                RADIUS * new Vector3( 0.5f, YD, S),
                RADIUS * new Vector3(-0.5f, YD, S),
                RADIUS * new Vector3(-1.0f, YD, 0),
                RADIUS * new Vector3(-0.5f, YD, -S),
                RADIUS * new Vector3( 0.5f, YD, -S),

                RADIUS * new Vector3( 1.0f, -YD, 0),
                RADIUS * new Vector3( 0.5f, -YD, S),
                RADIUS * new Vector3(-0.5f, -YD, S),
                RADIUS * new Vector3(-1.0f, -YD, 0),
                RADIUS * new Vector3(-0.5f, -YD, -S),
                RADIUS * new Vector3( 0.5f, -YD, -S),
            },
            new int[]
            {
                0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 0,
                6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 6,
            });
        }
    }

    public void Hit(Vector3 position, ref Vector3 velocity, Vector3 normal)
    {
        float extra = Ball.SPEED_LIMIT * BOOST - velocity.magnitude;
        if (extra <= 0f)
            return;

        normal = transform.InverseTransformVector(normal);
        if (Mathf.Abs(normal.y) > 0.08f * normal.magnitude)
            return;

        normal.y = 0;
        normal = transform.TransformVector(normal);
        velocity += normal * extra;

        position = transform.InverseTransformPoint(position);
        position.x = 0;
        position.z = 0;
        position = transform.TransformPoint(position);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy((Component)go.GetComponent<Collider>());
        go.transform.SetPositionAndRotation(position, transform.rotation);
        var scale = transform.lossyScale;
        scale.y = 1f;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        go.GetComponent<MeshFilter>().sharedMesh = static_mesh;
        go.AddComponent<VelocityBooster>();
    }
}
