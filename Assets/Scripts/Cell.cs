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

    protected void ChangeMaterial(Material mat)
    {
        FetchMyMaterial();
        GetComponent<MeshRenderer>().sharedMaterial = mat;
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
            if (energy > 0)
            {
                bonus |= Random.Range(0, 4) == 3;
                energy -= subtract_energy;
                if (energy > 0 && energy < 1e-3)
                    energy = 0;
                if (energy <= 0)
                    clip = PongPadBuilder.instance.tileBreakSound;
            }
            StartCoroutine(_Hit(energy <= 0));
        }
    }

    IEnumerator _Hit(bool fatal)
    {
        yield return new WaitForSeconds(0.05f);
        ChangeMaterial(MyMaterial);
        GotHit(fatal);

        if (fatal)
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

    protected virtual bool IgnoreHit() => finalBigCell && OtherCellsStillAround();
    protected virtual float GetCellFraction() => 1f;
    protected virtual void GotHit(bool fatal) { }

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

    public void HitVelocityBoost(Vector3 cell_speed)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetPositionAndRotation(transform.position, transform.rotation);
        go.transform.localScale = transform.lossyScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = MyMaterial;
        go.AddComponent<VelocityBooster>().base_speed = cell_speed * 0.5f;
    }

    class VelocityBooster : MonoBehaviour
    {
        static Mesh static_mesh;

        internal Vector3 base_speed;

        IEnumerator Start()
        {
            if (static_mesh == null)
            {
                var vecs = new Vector3[8];
                var norms = new Vector3[8];
                int i = 0;
                for (int dz = -1; dz <= 1; dz += 2)
                    for (int dy = -1; dy <= 1; dy += 2)
                        for (int dx = -1; dx <= 1; dx += 2)
                        {
                            vecs[i] = new Vector3(dx * 0.5f, dy * 0.5f, dz * 0.5f);
                            norms[i] = vecs[i].normalized;
                            i++;
                        }

                var indices = new int[] {
                    0, 1, 2, 3, 4, 5, 6, 7,
                    0, 2, 1, 3, 4, 6, 5, 7,
                    0, 4, 1, 5, 2, 6, 3, 7,
                };

                static_mesh = new Mesh();
                static_mesh.vertices = vecs;
                static_mesh.normals = norms;
                static_mesh.SetUVs(0, new List<Vector3>(vecs));
                static_mesh.SetIndices(indices, MeshTopology.Lines, 0);
                static_mesh.UploadMeshData(true);
            }
            GetComponent<MeshFilter>().sharedMesh = static_mesh;

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
