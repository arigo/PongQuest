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

    [MenuItem("CONTEXT/Cell/Make HexDir Yellow")]
    static void MakeHexDirYellowCmd(MenuCommand command)
    {
        MakeCmd(command, "?",
            "?",
            "Assets/Prefabs/Set3/HexDir Yellow.prefab");
    }

    [MenuItem("CONTEXT/Cell/Make HexDir Green")]
    static void MakeHexDirGreenCmd(MenuCommand command)
    {
        MakeCmd(command, "?",
            "?",
            "Assets/Prefabs/Set3/HexDir Green.prefab");
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

    [MenuItem("CONTEXT/Transform/Snap to triangular grid")]
    static void SnapToTriangularGridCmd(MenuCommand command)
    {
        const float GRID_SIZE = 0.19f;
        float GRID_SIZE_SMALLER = GRID_SIZE * 0.5f * Mathf.Sqrt(3);

        Transform tr = (Transform)command.context;
        int z = Mathf.RoundToInt(tr.localPosition.z / GRID_SIZE_SMALLER);
        float dx = 0.5f * (z & 1);
        float x = Mathf.RoundToInt(tr.localPosition.x / GRID_SIZE + dx) - dx;
        tr.localPosition = new Vector3(
            x * GRID_SIZE,
            tr.localPosition.y,
            z * GRID_SIZE_SMALLER);

        EditorUtility.SetDirty(tr);
        AssetDatabase.SaveAssets();
    }
}
