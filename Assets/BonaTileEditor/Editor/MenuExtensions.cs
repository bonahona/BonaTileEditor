using UnityEngine;
using System.Collections;
using UnityEditor;

public static class MenuExtensions
{
    [MenuItem("GameObject/Bona Tile editor/Map Segment")]
    [ContextMenu("test")]
    public static void CreateMapSegment()
    {
        BonaAssetUtility.CreateGameObject<MapSegment>();
    }
}
