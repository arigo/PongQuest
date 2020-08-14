using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Halo : MonoBehaviour
{
    static Mesh static_mesh;

    MeshRenderer mesh_renderer;
    Color base_color;

    void Start()
    {
        if (static_mesh == null)
        {
            static_mesh = new Mesh();
            const float E = 0.001f;
            static_mesh.vertices = new Vector3[] { new Vector2(-E, -E), new Vector2(E, -E), new Vector2(-E, E), new Vector2(E, E) };
            static_mesh.uv = new Vector2[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) };
            static_mesh.triangles = new int[] { 0, 1, 2, 1, 2, 3 };
        }

        GetComponent<MeshFilter>().sharedMesh = static_mesh;
        mesh_renderer = GetComponent<MeshRenderer>();
        base_color = transform.parent.GetComponent<MeshRenderer>().material.color;

        SetHighlight(false);
    }

    void SetHighlight(bool highlight)
    {
        base_color.a = highlight ? 1f : 0.75f;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", base_color);
        mesh_renderer.SetPropertyBlock(mpb);
    }

    public void Pong()
    {
        SetHighlight(true);
        StartCoroutine(_PongDone());
    }

    IEnumerator _PongDone()
    {
        yield return new WaitForEndOfFrame();
        SetHighlight(false);
    }
}
