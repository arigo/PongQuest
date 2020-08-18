using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MakeYellow
{
    static void MakeCmd(MenuCommand command, string path)
    {
        Cell cell = (Cell)command.context;

        Transform parent = cell.transform.parent;


        GameObject solar_cell = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var instance = PrefabUtility.InstantiatePrefab(solar_cell, parent);
        solar_cell = instance as GameObject;

        solar_cell.transform.position = cell.transform.position;
        solar_cell.transform.rotation = cell.transform.rotation;
        EditorUtility.SetDirty(solar_cell.transform);

        Object.DestroyImmediate(cell.gameObject);
        EditorUtility.SetDirty(parent);

        AssetDatabase.SaveAssets();
    }

    [MenuItem("CONTEXT/Cell/Make Yellow")]
    static void MakeYellowCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/SolarCell.prefab");
    }

    [MenuItem("CONTEXT/Cell/Make Green")]
    static void MakeGreenCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/SolarCell Green.prefab");
    }

    [MenuItem("CONTEXT/Cell/Make Blue")]
    static void MakeBlueCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/SolarCell Blue.prefab");
    }

    [MenuItem("CONTEXT/Cell/Make Red")]
    static void MakeRedCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/SolarCell Red.prefab");
    }

    [MenuItem("CONTEXT/Cell/Make Purple")]
    static void MakePurpleCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/SolarCell Purple.prefab");
    }
}
