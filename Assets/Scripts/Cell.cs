using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Cell : MonoBehaviour
{
    public float energy = 1;
    public int points = 100;
    public float pointsSize;
    public bool velocityBoost, finalBigCell;

    bool bonus;
    Material _my_material;

    void FetchMyMaterial()
    {
        if (_my_material == null)
            _my_material = GetComponent<MeshRenderer>().sharedMaterial;
    }

    protected Material MyMaterial
    {
        get
        {
            FetchMyMaterial();
            return _my_material;
        }
    }

    protected virtual void ChangeMaterial(Material mat = null)
    {
        FetchMyMaterial();
        GetComponent<MeshRenderer>().sharedMaterial = mat ?? _my_material;
    }

    public static void EmitHitPS(Vector3 pos, Vector3 normal, Color color)
    {
        var b = PongPadBuilder.instance;
        var ps = b.hitPS;

        for (int i = 0; i < 20; i++)
            ps.Emit(pos, normal + (Random.onUnitSphere * 0.5f),
                0.1f, Random.Range(0.2f, 0.5f), color);
    }

    public void Hit(RaycastHit hitInfo, float subtract_energy, ref AudioClip clip)
    {
        var b = PongPadBuilder.instance;
        var ps = b.hitPS;
        var color = MyMaterial.color;
        var ignore = IgnoreHit();
        EmitHitPS(hitInfo.point, hitInfo.normal, ignore ? Color.black : color);

        if (!ignore)
        {
            ChangeMaterial(b.cellHitMaterial);
            float prev_energy = energy;
            if (energy > 0)
            {
                bonus |= Random.Range(0, 4) == 3;
                energy -= subtract_energy;
                if (energy > 0 && energy < 1e-3)
                    energy = 0;
                if (energy <= 0)
                    clip = PongPadBuilder.instance.tileBreakSound;
            }
            StartCoroutine(_Hit(new CellHitInfo
            {
                fatal = energy <= 0,
                prev_energy = prev_energy,
                hit_point = hitInfo.point,
            }));
        }
    }

    IEnumerator _Hit(CellHitInfo info)
    {
        yield return new WaitForSeconds(0.05f);
        ChangeMaterial();
        GotHit(info);

        if (info.fatal)
        {
            Destroy((GameObject)gameObject);
            if (bonus)
                Bonus.AddBonus(transform.position);

            float cell_fraction = GetCellFraction();
            int points1 = Mathf.RoundToInt(points * cell_fraction);
            Points.AddPoints(transform.position, MyMaterial.color, points1, pointsSize);
            PongPadBuilder.instance.KilledOneCell(cell_fraction);
        }
    }

    protected struct CellHitInfo
    {
        public float prev_energy;
        public bool fatal;
        public Vector3 hit_point;
    }

    protected virtual bool IgnoreHit() => finalBigCell && OtherCellsStillAround();
    protected virtual float GetCellFraction() => 1f;
    protected virtual void GotHit(CellHitInfo info) { }

    bool OtherCellsStillAround()
    {
        foreach (var cells in FindObjectsOfType<Cell>())
            if (cells != this)
                return true;
        return false;
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

    public virtual float VelocityBoostSpeed() => 2.5f;
    public virtual bool VelocityBoostCube() => true;

    public void HitVelocityBoost(Vector3 cell_speed)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetPositionAndRotation(transform.position, transform.rotation);
        go.transform.localScale = transform.lossyScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = MyMaterial;
        var vb = go.AddComponent<VelocityBooster>();
        vb.base_speed = cell_speed * 0.5f;
        if (!VelocityBoostCube())
            vb.src_mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    class VelocityBooster : MonoBehaviour
    {
        static Dictionary<string, Mesh> static_meshes = new Dictionary<string, Mesh>();

        internal Mesh src_mesh;
        internal Vector3 base_speed;

        IEnumerator Start()
        {
            string name = src_mesh ? src_mesh.name : "cube";
            if (!static_meshes.TryGetValue(name, out Mesh mesh))
            {
                var vecs = new List<Vector3>();
                var indices = new List<int>();

                if (!src_mesh)
                {
                    /* cube */
                    for (int dz = -1; dz <= 1; dz += 2)
                        for (int dy = -1; dy <= 1; dy += 2)
                            for (int dx = -1; dx <= 1; dx += 2)
                                vecs.Add(new Vector3(dx * 0.5f, dy * 0.5f, dz * 0.5f));

                    indices.AddRange(new int[] {
                        0, 1, 2, 3, 4, 5, 6, 7,
                        0, 2, 1, 3, 4, 6, 5, 7,
                        0, 4, 1, 5, 2, 6, 3, 7,
                    });
                }
                else
                {
                    /* copy the underlying mesh's edges */
                    var vec2index = new Dictionary<Vector3Int, int>();
                    var seen_edge = new HashSet<System.Tuple<int, int>>();

                    int AddVec(Vector3 v)
                    {
                        Vector3Int key = Vector3Int.RoundToInt(v * 128f);
                        if (!vec2index.TryGetValue(key, out int i))
                        {
                            i = vecs.Count;
                            vecs.Add(v);
                            vec2index[key] = i;
                        }
                        return i;
                    }

                    var src_vecs = src_mesh.vertices;
                    var src_indices = src_mesh.GetIndices(0);
                    for (int i = 0; i < src_indices.Length; i++)
                    {
                        int j = i + 1;
                        if ((j % 3) == 0)
                            j -= 3;

                        int ii = AddVec(src_vecs[src_indices[i]]);
                        int jj = AddVec(src_vecs[src_indices[j]]);

                        if (!seen_edge.Contains(System.Tuple.Create(ii, jj)) &&
                            !seen_edge.Contains(System.Tuple.Create(jj, ii)))
                        {
                            indices.Add(ii);
                            indices.Add(jj);
                            seen_edge.Add(System.Tuple.Create(jj, ii));
                        }
                    }
                }

                var norms = new Vector3[vecs.Count];
                for (int i = 0; i < vecs.Count; i++)
                    norms[i] = vecs[i].normalized;

                mesh = new Mesh();
                mesh.SetVertices(vecs);
                mesh.normals = norms;
                mesh.SetUVs(0, vecs);
                mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
                mesh.UploadMeshData(true);
                static_meshes[name] = mesh;
            }
            GetComponent<MeshFilter>().sharedMesh = mesh;

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
}
