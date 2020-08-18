using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RandomAxis
{
    [MenuItem("CONTEXT/MoveCells/Random Axis")]
    static void RandomAxisCmd(MenuCommand command)
    {
        MoveCells move_cells = (MoveCells)command.context;
        Vector3 target = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.5f, 1.5f), 0);
        Vector3 axis = Vector3.ProjectOnPlane(Random.onUnitSphere, target - move_cells.transform.position);

        var so = new SerializedObject(move_cells);
        so.FindProperty("center").vector3Value = new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(-0.7f, 0.7f), Random.Range(-0.7f, 0.7f));
        so.FindProperty("rotationAxis").vector3Value = axis.normalized;
        so.FindProperty("rotationSpeed").floatValue = Random.Range(750f, 1500f);
        so.ApplyModifiedProperties();
    }
}
