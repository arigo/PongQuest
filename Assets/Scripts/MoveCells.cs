using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCells : MonoBehaviour
{
    public Vector3 center;
    public Vector3 rotationAxis;
    public float rotationSpeed;
    public Cell[] cells;

    private void Start()
    {
        if (cells == null || cells.Length == 0)
        {
            var my_cell = GetComponent<Cell>();
            if (my_cell != null)
                cells = new Cell[] { my_cell };
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Quaternion rot = Quaternion.AngleAxis(rotationSpeed * dt, rotationAxis);
        var center1 = transform.TransformPoint(center);

        foreach (var cell in cells)
        {
            if (!cell)
                continue;
            var pos = cell.transform.position;
            var v = pos - center1;
            v = rot * v;
            cell.MoveAndRotateTo(center1 + v, rot * cell.transform.rotation);
        }
    }
}
