using UnityEngine;
using System.Collections;

public class MapSegmentTraverseResult
{
    public MapSegmentPathTile PathTile { get; set; }
    public MapSegmentDirection Direction { get; set; }

    public override string ToString()
    {
        return string.Format("PathTile={0}, Direction={1}", PathTile, Direction);
    }
}
