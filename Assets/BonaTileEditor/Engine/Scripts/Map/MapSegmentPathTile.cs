using UnityEngine;
using System.Collections;

public class MapSegmentPathTile
{
    public bool IsWalkable { get; set; }
    public MapSegmentDirection Directions { get; set; }

    public MapSegmentPathTile()
    {
        IsWalkable = false;
        Directions = MapSegmentDirection.None;
    }
}
