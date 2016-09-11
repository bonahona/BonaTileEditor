using UnityEngine;
using UnityEditor;
using System.IO;

public static class BonaAssetUtility
{
    public static void CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (selectedPath == "") {
            selectedPath = "Assets";
        } else if (Path.GetExtension(selectedPath) != "") {
            selectedPath = selectedPath.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(selectedPath + "/New " + typeof(T).ToString() + ".asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    public static void CreateGameObject<T>() where T : MonoBehaviour
    {
        var gameObjectName = typeof(T).ToString();
        var gameObject = new GameObject("New " + gameObjectName);
        gameObject.AddComponent<T>();
    }
}