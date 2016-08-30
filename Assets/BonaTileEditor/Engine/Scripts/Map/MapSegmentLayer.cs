using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Material))]
[RequireComponent(typeof(MeshCollider))]
public class MapSegmentLayer : MonoBehaviour {

    // Internal state variables
    public MapSegment MapSegment;
    public TileSetLayer TileSetLayer;
    public TileTypeCollection TilesCollection;

    public MeshFilter MeshFilter { get; set; }
}
