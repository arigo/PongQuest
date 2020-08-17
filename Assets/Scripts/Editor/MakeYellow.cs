using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MakeYellow
{
    [MenuItem("CONTEXT/Cell/MakeYellow")]
    static void MakeYellowCmd(MenuCommand command)
    {
        Cell cell = (Cell)command.context;

        Transform parent = cell.transform.parent;


        GameObject solar_cell = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SolarCell.prefab");
        var instance = PrefabUtility.InstantiatePrefab(solar_cell, parent);
        solar_cell = instance as GameObject;

        solar_cell.transform.position = cell.transform.position;
        solar_cell.transform.rotation = cell.transform.rotation;

        Object.DestroyImmediate(cell.gameObject);
    }
}
