using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MoveCells : MonoBehaviour
{
    public Vector3 center;
    public Vector3 rotationAxis;
    public float rotationSpeed;
    public float movementFrequency;
    public Cell[] cells;

    private void Start()
    {
        if (cells == null || cells.Length == 0)
        {
            var my_cell = GetComponent<Cell>();
            if (my_cell != null)
                cells = new Cell[] { my_cell };
        }

        if (movementFrequency != 0f && transform == cells[0].transform)
            Debug.LogError("MoveCells with a movementFrequency must not be components of the Cell itself");
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Quaternion rot = Quaternion.AngleAxis(rotationSpeed * dt, rotationAxis);
        var center1 = transform.TransformPoint(center);
        float movement_target = movementFrequency != 0f ? Mathf.Sin(movementFrequency * Time.time) : 0f;

        foreach (var cell in cells)
        {
            if (!cell)
                continue;

            Vector3 center2 = center1;
            Vector3 pos = cell.transform.position;
            if (movementFrequency != 0f)
            {
                center2 -= pos;
                center2 -= Vector3.ProjectOnPlane(center2, rotationAxis);
                center2 += pos;
            }
            var v = pos - center2;
            if (movementFrequency != 0f)
            {
                float delta = Vector3.Dot(v, rotationAxis) / rotationAxis.sqrMagnitude;
                v += (movement_target - delta) * rotationAxis;
            }
            v = rot * v;
            pos = center2 + v;
            cell.MoveAndRotateTo(pos, rot * cell.transform.rotation);
        }
    }
}
