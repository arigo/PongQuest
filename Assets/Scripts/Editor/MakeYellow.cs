using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MakeYellow
{
    static void MakeCmd(MenuCommand command, string path1, string path2, string path3)
    {
        Cell cell = (Cell)command.context;
        string path = cell is LivingCell ? path2 : cell is HexCell ? path3 : path1;

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
        MakeCmd(command, "Assets/Prefabs/Set1/SolarCell.prefab",
            "Assets/Prefabs/Set2/Cell Yellow.prefab",
            "Assets/Prefabs/Set3/HexCell Yellow.prefab");
    }

    [MenuItem("CONTEXT/Cell/Make Green")]
    static void MakeGreenCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/Set1/SolarCell Green.prefab",
            "Assets/Prefabs/Set2/Cell Green.prefab",
            "Assets/Prefabs/Set3/HexCell Green.prefab");
    }

    [MenuItem("CONTEXT/Cell/Make Blue")]
    static void MakeBlueCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/Set1/SolarCell Blue.prefab",
            "Assets/Prefabs/Set2/Cell Blue.prefab",
            "?");
    }

    [MenuItem("CONTEXT/Cell/Make Red")]
    static void MakeRedCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/Set1/SolarCell Red.prefab",
            "Assets/Prefabs/Set2/Cell Red.prefab",
            "?");
    }

    [MenuItem("CONTEXT/Cell/Make Purple")]
    static void MakePurpleCmd(MenuCommand command)
    {
        MakeCmd(command, "Assets/Prefabs/Set1/SolarCell Purple.prefab",
            "Assets/Prefabs/Set2/Cell Purple.prefab",
            "?");
    }

    [MenuItem("CONTEXT/Cell/Shuffle Rotation")]
    static void ShuffleRotationCmd(MenuCommand command)
    {
        Cell cell = (Cell)command.context;
        cell.transform.rotation = Random.rotationUniform;
        EditorUtility.SetDirty(cell.transform);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("CONTEXT/Cell/Move Forward By Diameter")]
    static void MoveForwardByDiameterCmd(MenuCommand command)
    {
        Cell cell = (Cell)command.context;
        cell.transform.position += cell.transform.forward * cell.transform.lossyScale.y;
        EditorUtility.SetDirty(cell.transform);
        AssetDatabase.SaveAssets();
    }
}
