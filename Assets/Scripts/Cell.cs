using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Cell : MonoBehaviour
{
    public float energy = 1;
    public int points = 100;
    public float pointsSize;
    public bool velocityBoost, finalBigCell, nextLevelIfOnlyMe;

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

        var emit_params = new ParticleSystem.EmitParams
        {
            position = pos,
            startSize = 0.1f,
            startColor = color,
        };

        for (int i = 0; i < 20; i++)
        {
            emit_params.velocity = normal + (Random.onUnitSphere * 0.5f);
            emit_params.startLifetime = Random.Range(0.2f, 0.5f);
            ps.Emit(emit_params, 1);
        }
    }

    public void Hit(RaycastHit hitInfo, float subtract_energy, bool ignore, ref AudioClip clip)
    {

        var color = MyMaterial.color;

        if (!ignore)
        {
            if (energy <= 0)
                return;
            ChangeMaterial(PongPadBuilder.instance.cellHitMaterial);
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
        EmitHitPS(hitInfo.point, hitInfo.normal, ignore ? Color.black : color);
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

    public virtual bool IgnoreHit(Vector3 velocity, bool unstoppable) =>
        finalBigCell && OtherCellsStillAround();
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

    public virtual float VelocityBoostSpeed() => 5f;
    public virtual bool VelocityBoostCube() => true;

    public void HitVelocityBoost(Vector3 cell_speed)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy((Component)go.GetComponent<Collider>());
        go.transform.SetPositionAndRotation(transform.position, transform.rotation);
        go.transform.localScale = transform.lossyScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = MyMaterial;
        go.GetComponent<MeshFilter>().sharedMesh = GetVBMesh();
        var vb = go.AddComponent<VelocityBooster>();
        vb.base_speed = cell_speed * 0.5f;
    }

    static Dictionary<string, Mesh> vb_static_meshes = new Dictionary<string, Mesh>();

    Mesh GetVBMesh()
    {
        Mesh src_mesh = VelocityBoostCube() ? null : GetComponent<MeshFilter>().sharedMesh;

        string name = src_mesh ? src_mesh.name : "cube";
        if (!vb_static_meshes.TryGetValue(name, out Mesh mesh))
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

            mesh = VelocityBooster.MakeLinesMesh(vecs, indices.ToArray());
            vb_static_meshes[name] = mesh;
        }
        return mesh;
    }
}
