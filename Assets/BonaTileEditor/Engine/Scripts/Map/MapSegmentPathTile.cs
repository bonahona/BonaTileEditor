using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapSegmentPathTile
{
    public static readonly Point UP_START_OFFSET = new Point(0, 1);
    public static readonly Point UP_END_OFFSET = new Point(1, 1);
    public static readonly Point RIGHT_START_OFFSET = new Point(1, 1);
    public static readonly Point RIGHT_END_OFFSET = new Point(1, 0);
    public static readonly Point DOWN_START_OFFSET = new Point(1, 0);
    public static readonly Point DOWN_END_OFFSET = new Point(0, 0);
    public static readonly Point LEFT_START_OFFSET = new Point(0, 0);
    public static readonly Point LEFT_END_OFFSET = new Point(0, 1);

    public bool IsWalkable { get; set; }
    public MapSegmentDirection Directions { get; set; }
    public Point Point { get; set; }

    public MapSegmentPathTile Up { get; set; }
    public MapSegmentPathTile Down { get; set; }
    public MapSegmentPathTile Left { get; set; }
    public MapSegmentPathTile Right { get; set; }

    public MapSegmentPathTile()
    {
        IsWalkable = false;
        Directions = MapSegmentDirection.None;
    }

    public List<MapSegmentPathTile> GetNeighbours()
    {
        var result = new List<MapSegmentPathTile>();
        result.Add(Up);
        result.Add(Down);
        result.Add(Left);
        result.Add(Right);
        return result;
    }

    public Point GetStartPoint(MapSegmentDirection direction)
    {
        if(direction == MapSegmentDirection.Up) {
            return Point + UP_START_OFFSET;
        }else if(direction == MapSegmentDirection.Right) {
            return Point + RIGHT_START_OFFSET;
        } else if (direction == MapSegmentDirection.Down) {
            return Point + DOWN_START_OFFSET;
        } else if (direction == MapSegmentDirection.Left) {
            return Point + LEFT_START_OFFSET;
        }

        return Point;
    }

    public Point GetEndPoint(MapSegmentDirection direction)
    {
        if (direction == MapSegmentDirection.Up) {
            return Point + UP_END_OFFSET;
        } else if (direction == MapSegmentDirection.Right) {
            return Point + RIGHT_END_OFFSET;
        } else if (direction == MapSegmentDirection.Down) {
            return Point + DOWN_END_OFFSET;
        } else if (direction == MapSegmentDirection.Left) {
            return Point + LEFT_END_OFFSET;
        }

        return Point;
    }
}
