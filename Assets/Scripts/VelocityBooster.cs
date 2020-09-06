using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VelocityBooster : MonoBehaviour
{
    internal Vector3 base_speed;

    public static Mesh MakeLinesMesh(List<Vector3> vecs, int[] indices)
    {
        var norms = new Vector3[vecs.Count];
        for (int i = 0; i < vecs.Count; i++)
            norms[i] = vecs[i].normalized;

        var mesh = new Mesh();
        mesh.SetVertices(vecs);
        mesh.normals = norms;
        mesh.SetUVs(0, vecs);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);

        mesh.UploadMeshData(true);
        return mesh;
    }

    IEnumerator Start()
    {
        Vector3 base_scale = transform.localScale;
        float delta_y = 1f;
        float vy = 0.5f;
        while (vy > 0f)
        {
            yield return null;

            float delta = Time.deltaTime * vy * 2.2f;
            delta_y += delta;
            transform.localScale = base_scale * delta_y;
            transform.position += Time.deltaTime * base_speed;
            vy -= Time.deltaTime;
        }
        Destroy((GameObject)gameObject);
    }
}
