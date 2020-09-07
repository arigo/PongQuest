using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AddMass : MonoBehaviour
{
    public const float CELL_MASS = 2.5f;

    void Start()
    {
        foreach (var hexcell in GetComponentsInChildren<HexCell>())
        {
            Debug.Assert(hexcell.GetComponent<Rigidbody>() == null);
            if (!hexcell.hasDirection)
            {
                var rb = hexcell.gameObject.AddComponent<Rigidbody>();
                rb.mass = CELL_MASS;
            }
        }
    }
}
