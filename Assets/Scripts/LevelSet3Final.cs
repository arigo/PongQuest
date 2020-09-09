using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelSet3Final : MonoBehaviour
{
    public GameObject[] duplicateMe;
    public int numberOfDuplicates;
    public HexCell finalHexCell;

    class Remaining
    {
        public int remaining;
        public List<Rigidbody> lives;
    }
    int complete;

    IEnumerator Start()
    {
        foreach (var go in duplicateMe)
        {
            go.GetComponent<MeshRenderer>().enabled = false;
            go.GetComponent<Collider>().enabled = false;
        }

        /* wait until we hit the finalbigcell once */
        float initial_energy = finalHexCell.energy;
        while (initial_energy == finalHexCell.energy)
            yield return null;
        SetIgnoreHits(true);

        /* Spawn coroutines that handle the regular HexCells appearing */

        Ball.speed_limit += 0.01f;   /* will increase further in PongPadBuilder.KilledOneCell */

        foreach (var go in duplicateMe)
        {
            var rem = new Remaining { remaining = numberOfDuplicates, lives = new List<Rigidbody>() };
            StartCoroutine(DuplicateMe(go, rem));
            StartCoroutine(DisposeMe(go, rem));
        }
    }

    Rigidbody DuplicateHexCell(GameObject go)
    {
        go = Instantiate(go, transform, worldPositionStays: true);
        go.GetComponent<MeshRenderer>().enabled = true;
        go.GetComponent<Collider>().enabled = true;
        var hexcell = go.GetComponent<HexCell>();
        hexcell.fraction_of_original = 1f / numberOfDuplicates;
        hexcell.points *= numberOfDuplicates;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = AddMass.CELL_MASS;
        rb.sleepThreshold *= 3.5f;
        return rb;
    }

    IEnumerator DuplicateMe(GameObject prefab, Remaining rem)
    {
        Vector3 center = prefab.transform.position;

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 0.8f));

            if (rem.remaining <= 0)
                continue;

            Rigidbody rb = DuplicateHexCell(prefab);
            rb.useGravity = false;
            rb.isKinematic = true;
            Vector3 pos1 = rb.position;
            float y_limit = pos1.y + 0.255f;

            rem.remaining -= 1;
            rem.lives.Add(rb);

            while (pos1.y < y_limit)
            {
                yield return new WaitForFixedUpdate();
                if (rb == null)   /* destroyed already */
                    break;
                pos1 += Vector3.up * (0.35f * Time.fixedDeltaTime);
                rb.MovePosition(pos1);
            }

            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
            }
        }
    }

    IEnumerator DisposeMe(GameObject prefab, Remaining rem)
    {
        Vector3 center = prefab.transform.position;
        center.y += 0.25f;

        while (rem.remaining + rem.lives.Count > 0)
        {
            int i = 0;
            while (i < rem.lives.Count)    /* rem.lives.Count can change */
            {
                var rb = rem.lives[i];
                if (rb == null)
                {
                    rem.lives[i] = rem.lives[rem.lives.Count - 1];
                    rem.lives.RemoveAt(rem.lives.Count - 1);
                    continue;
                }
                i++;

                if (rb.IsSleeping() && rb.position.y < 0.35f &&
                    Vector3.Distance(rb.position, center) > 0.1f)
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                    const float y_limit = -0.1f;
                    Vector3 pos1 = rb.position;

                    while (pos1.y > y_limit)
                    {
                        yield return new WaitForFixedUpdate();
                        if (rb == null)   /* destroyed already */
                            break;
                        pos1 -= Vector3.up * (0.35f * Time.fixedDeltaTime);
                        rb.MovePosition(pos1);
                    }

                    if (rb != null)
                    {
                        Destroy((GameObject)rb.gameObject);
                        rem.remaining += 1;
                    }
                }
                else
                    yield return null;
            }
            yield return null;
        }

        Destroy((GameObject)prefab);

        complete += 1;
        if (complete == 3)
            SetIgnoreHits(false);
    }

    private void OnDestroy()
    {
        Ball.speed_limit = Ball.SPEED_LIMIT;
    }

    void SetIgnoreHits(bool ignore_hits)
    {
        finalHexCell.ignore_hits = ignore_hits;

        IEnumerator ShiftMaterial()
        {
            Vector4 PARAMETERS_F = new Vector4(1f, 0f, 0.35295f, 0f);
            Vector4 PARAMETERS_T = new Vector4(0.75f, 0f, 1.05f, 0f);

            Vector4 parameters = ignore_hits ? PARAMETERS_F : PARAMETERS_T;
            Vector4 target = ignore_hits ? PARAMETERS_T : PARAMETERS_F;

            int parameters_id = Shader.PropertyToID("_Parameters");
            var rend = finalHexCell.GetComponent<MeshRenderer>();
            var pb = new MaterialPropertyBlock();

            while (parameters != target)
            {
                parameters = Vector4.MoveTowards(parameters, target, Time.deltaTime * 0.28f);
                pb.SetVector(parameters_id, parameters);
                rend.SetPropertyBlock(pb);
                yield return null;
            }
        }
        StartCoroutine(ShiftMaterial());
    }
}
