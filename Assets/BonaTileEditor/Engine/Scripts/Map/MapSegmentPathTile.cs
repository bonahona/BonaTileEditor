using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapSegmentPathTile
{
    public const bool NULL_TILE_IS_WALKABLE = true;                 // Default value for how any non-existing nodes should be treated

    public static readonly Point UP_START_OFFSET = new Point(0, 1);
    public static readonly Point UP_END_OFFSET = new Point(1, 1);
    public static readonly Point RIGHT_START_OFFSET = new Point(1, 1);
    public static readonly Point RIGHT_END_OFFSET = new Point(1, 0);
    public static readonly Point DOWN_START_OFFSET = new Point(1, 0);
    public static readonly Point DOWN_END_OFFSET = new Point(0, 0);
    public static readonly Point LEFT_START_OFFSET = new Point(0, 0);
    public static readonly Point LEFT_END_OFFSET = new Point(0, 1);

    public bool IsWalkable { get; set; }
    public Point Point { get; set; }

    public MapSegmentPathTile NeighbourUp { get; set; }
    public MapSegmentPathTile NeighbourDown { get; set; }
    public MapSegmentPathTile NeighbourLeft { get; set; }
    public MapSegmentPathTile NeighbourRight { get; set; }

    public MapSegmentPathTile()
    {
        IsWalkable = false;
    }

    public List<MapSegmentPathTile> GetNeighbours()
    {
        var result = new List<MapSegmentPathTile>();
        result.Add(NeighbourUp);
        result.Add(NeighbourDown);
        result.Add(NeighbourLeft);
        result.Add(NeighbourRight);
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

    public bool IsNeighbourFree(MapSegmentDirection direction)
    {
        MapSegmentPathTile tile = null;

        if (direction == MapSegmentDirection.Up) {
            tile = NeighbourUp;
        } else if (direction == MapSegmentDirection.Down) {
            tile = NeighbourDown;
        } else if (direction == MapSegmentDirection.Left) {
            tile = NeighbourLeft;
        } else if (direction == MapSegmentDirection.Right) {
            tile = NeighbourRight;
        }

        return IsFree(tile);
    }

    protected bool IsFree(MapSegmentPathTile tile)
    {
        if (tile == null) {
            return NULL_TILE_IS_WALKABLE;
        } else {
            return tile.IsWalkable;
        }
    }
}
