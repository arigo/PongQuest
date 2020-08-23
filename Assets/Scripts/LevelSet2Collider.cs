using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelSet2Collider : MonoBehaviour
{
    private void Start()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        var vertices = mesh.vertices;
        var _tri1 = new int[] { 0, 2, 1, 0, 3, 2, 2, 3, 1, 1, 3, 0 };
        for (int j = 0; j < mesh.subMeshCount; j++)
        {
            var indices = mesh.GetIndices(j);
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 p0 = vertices[indices[i + 0]];
                Vector3 p1 = vertices[indices[i + 1]];
                Vector3 p2 = vertices[indices[i + 2]];
                Vector3 center = (p0 + p1 + p2) / 3f;
                Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
                Vector3 eye = center - new Vector3(0f, 1f, 2f);
                if (Vector3.Dot(eye, normal) <= 0f)
                    continue;
                Vector3 p3 = center + 0.06f * normal.normalized;

                var mesh1 = new Mesh();
                mesh1.vertices = new Vector3[] { p0, p1, p2, p3 };
                mesh1.SetTriangles(_tri1, 0);

                var coll = gameObject.AddComponent<MeshCollider>();
                coll.convex = true;
                coll.sharedMesh = mesh1;
            }
        }
    }
}
