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

        var b = PongPadBuilder.instance;
        mesh_renderer = Instantiate(b.haloPrefab, transform.position, transform.rotation, transform);
        mesh_renderer.GetComponent<MeshFilter>().sharedMesh = static_mesh;

        base_color = GetComponent<MeshRenderer>().material.color;
        SetHighlight(false);

        var cell = GetComponent<Cell>();
        cell.points *= 5;
        cell.pointsSize = 1.35f;
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
        while (PongPadBuilder.paused)
            yield return new WaitForEndOfFrame();
        SetHighlight(false);
    }
}
