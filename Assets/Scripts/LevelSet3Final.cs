using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LevelSet3Final : MonoBehaviour
{
    public GameObject[] duplicateMe;
    public int numberOfDuplicates;

    private void Start()
    {
        Ball.speed_limit += 0.01f;   /* will increase further in PongPadBuilder.KilledOneCell */

        foreach (var go in duplicateMe)
        {
            go.GetComponent<MeshRenderer>().enabled = false;
            go.GetComponent<Collider>().enabled = false;

            StartCoroutine(DuplicateMe(go));
        }
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
        List<Rigidbody> all_cells = new List<Rigidbody>();

        for (int i = 0; i < numberOfDuplicates; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 0.8f));

            var rb = DuplicateHexCell(prefab);
            rb.useGravity = false;
            rb.isKinematic = true;
            float y_limit = rb.transform.position.y + 0.255f;

            while (rb.transform.position.y < y_limit)
            {
                yield return new WaitForFixedUpdate();
                if (rb == null)   /* destroyed already */
                    break;
                rb.transform.position += Vector3.up * (0.35f * Time.fixedDeltaTime);
            }

            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
                all_cells.Add(rb);
            }
        }

        Destroy((GameObject)prefab);

        yield return new WaitForSeconds(10f);

        /* Bounce */
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3f, 4f));

            foreach (var rb in all_cells)
            {
                if (rb != null && rb.IsSleeping())
                {
                    float force = Random.Range(0.4f, 1f);
                    rb.AddForce((Vector3.up * 2f + Random.insideUnitSphere) * force, ForceMode.VelocityChange);
                    rb.AddTorque(Random.insideUnitSphere, ForceMode.VelocityChange);
                }
            }
        }
    }

    private void OnDestroy()
    {
        Ball.speed_limit = Ball.SPEED_LIMIT;
    }
}
