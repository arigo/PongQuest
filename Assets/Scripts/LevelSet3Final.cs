using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelSet3Final : MonoBehaviour
{
    public GameObject[] duplicateMe;
    public int numberOfDuplicates;

    float last_active;

    private void Start()
    {
        Ball.speed_limit += 0.01f;   /* will increase further in PongPadBuilder.KilledOneCell */

        foreach (var go in duplicateMe)
        {
            go.GetComponent<MeshRenderer>().enabled = false;
            go.GetComponent<Collider>().enabled = false;

            StartCoroutine(DuplicateMe(go));
        }

        StartCoroutine(Bounce());
    }

    Rigidbody DuplicateHexCell(GameObject go)
    {
        go = Instantiate(go, transform, worldPositionStays: true);
        go.GetComponent<MeshRenderer>().enabled = true;
        go.GetComponent<Collider>().enabled = true;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = AddMass.CELL_MASS;
        return rb;
    }

    IEnumerator DuplicateMe(GameObject prefab)
    {
        Vector3 center = prefab.transform.position;
        Stack<GameObject> actives = new Stack<GameObject>();
        int step = 0;

        while (true)
        {
            int remaining = numberOfDuplicates;
            float force = step == 0 ? 0.43f : 0.9f;

            while (remaining > 0)
            {
                yield return new WaitForSeconds(Random.Range(0.5f, 0.8f));

                if (!Physics.CheckSphere(center, 0.15f, 1 << Ball.LAYER_CELLS))
                {
                    var rb = DuplicateHexCell(prefab);
                    rb.AddForce((Vector3.up * 3f + Random.insideUnitSphere) * force, ForceMode.VelocityChange);
                    rb.AddTorque(Random.insideUnitSphere, ForceMode.VelocityChange);
                    actives.Push(rb.gameObject);

                    remaining--;
                    last_active = Time.time;
                }
            }

            step++;
            if (step == 2)
                break;

            while (actives.Count > 0)
            {
                if (actives.Peek() == null)
                    actives.Pop();
                else
                    yield return new WaitForSeconds(1f);
            }
        }
        Destroy((GameObject)prefab);
    }

    IEnumerator Bounce()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            if (Time.time - last_active >= 2f)
            {
                foreach (var cell in GetComponentsInChildren<HexCell>())
                {
                    var rb = cell.GetComponent<Rigidbody>();
                    if (rb != null && rb.IsSleeping())
                    {
                        float force = Random.Range(0.4f, 1f);
                        rb.AddForce((Vector3.up * 2f + Random.insideUnitSphere) * force, ForceMode.VelocityChange);
                        rb.AddTorque(Random.insideUnitSphere, ForceMode.VelocityChange);
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        Ball.speed_limit = Ball.SPEED_LIMIT;
    }
}
