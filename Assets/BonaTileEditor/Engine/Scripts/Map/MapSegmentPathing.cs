using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MapSegmentPathing
{
    public const int MAX_POINTS_BEFORE_ERROR = 1000;            // Reasonable limit as the limit in tiles is 100x100
    public MapSegmentPathTile[,] PathingMap { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public MapSegmentPathing()
    {
        PathingMap = new MapSegmentPathTile[0, 0];
        Width = 0;
        Height = 0;
    }

    public MapSegmentPathing(int width, int height)
    {
        PathingMap = new MapSegmentPathTile[width, height];
        Width = width;
        Height = height;
    }

    public List<MapSegmentPathTile> GetAll()
    {
        var result = new List<MapSegmentPathTile>();
        for(int y = 0;y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                result.Add(PathingMap[x, y]);
            }
        }

        return result;
    }

    public MapSegmentPathTile SetCordinate(Point point, MapSegmentPathTile tile)
    {
        return SetCordinate(point.X, point.Y, tile);
    }

    public MapSegmentPathTile SetCordinate(int x, int y, MapSegmentPathTile tile)
    {
        PathingMap[x, y] = tile;
        tile.Point = new Point(x, y);
        return tile;
    }

    public void UpdateInternalBoundries()
    {
        foreach (var tile in GetAll()) {
            tile.NeighbourLeft = GetTileOrDefault(new Point(tile.Point.X - 1, tile.Point.Y));
            tile.NeighbourRight = GetTileOrDefault(new Point(tile.Point.X + 1, tile.Point.Y));
            tile.NeighbourUp = GetTileOrDefault(new Point(tile.Point.X, tile.Point.Y - 1));
            tile.NeighbourDown = GetTileOrDefault(new Point(tile.Point.X, tile.Point.Y + 1));
        }
    }

    public List<List<Vector2>> GetColliderPoints()
    {
        var result = new List<List<Vector2>>();

        var unWalkableTileGroups = GetGroups(false);
        var walkableTilegroups = GetGroups(true);

        foreach (var group in unWalkableTileGroups) {
            var groupPoints = GetGroupPointsFixed(group, walkableTilegroups);
            var minimizedGroupPoints = GetMinimizedColliderPointList(groupPoints);
            result.Add(minimizedGroupPoints);
        }

        // If this is not zero, there are walkable holes in the map that has not been taken into account and must be fixed
        if(walkableTilegroups.Count != 0) {
            foreach (var walkableTileGroup in walkableTilegroups.ToList()) {
                var groupPoints = GetGroupPointsRandom(unWalkableTileGroups, walkableTileGroup, walkableTilegroups);
                var minimizedGroupPoints = GetMinimizedColliderPointList(groupPoints);
                result.Add(minimizedGroupPoints);
            }
        }

        return result;
    }

    public List<Vector2> GetMinimizedColliderPointList(List<Vector2> vertices)
    {
        if(vertices == null) {
            return null;
        }

        if(vertices.Count <= 1) {
            return null;
        }

        var result = new List<Vector2>();

        var startNode = vertices.First();
        var currentNode = vertices.First();

        for (int i = 1; i < vertices.Count; i++) {
            var examinedNode = vertices[i];

            if (startNode.x == currentNode.x && examinedNode.x == currentNode.x) {
                currentNode = examinedNode;
            } else if (startNode.y == currentNode.y && examinedNode.y == currentNode.y) {
                currentNode = examinedNode;
            } else {
                result.Add(currentNode);
                startNode = currentNode;
                currentNode = examinedNode;
            }
        }

        result.Add(vertices.Last());

        return result;
    }

    public List<HashSet<MapSegmentPathTile>> GetGroups(bool isWalkable)
    {
        var checkedTiles = new HashSet<MapSegmentPathTile>();

        var result = new List<HashSet<MapSegmentPathTile>>();

        foreach(var tile in GetAll()) {
            if (tile.IsWalkable == isWalkable && !checkedTiles.Contains(tile)) {
                var tileGroup = DepthFirstSearch(tile, isWalkable);
                foreach (var groupTile in tileGroup) {
                    checkedTiles.Add(groupTile);
                }

                result.Add(tileGroup);
            }
        }

        return result;
    }

    public List<Vector2> GetGroupPointsFixed(ICollection<MapSegmentPathTile> tiles, List<HashSet<MapSegmentPathTile>> walkableGroups)
    {
        var startDirection = MapSegmentDirection.Up;
        var startTile = FindStartTile(tiles, MapSegmentDirection.Up);

        return GetGroupPoints(tiles, walkableGroups, startTile, startDirection);
    }

    public List<Vector2> GetGroupPointsRandom(List<HashSet<MapSegmentPathTile>> tileGroups, HashSet<MapSegmentPathTile> currentWalkableGroup, List<HashSet<MapSegmentPathTile>> walkableGroups)
    {
        var startPathTile = FindMatchingStartTile(tileGroups, currentWalkableGroup, walkableGroups);

        if(startPathTile == null) {
            throw new System.Exception("Failed to get a matching random start tile");
        }

        var startDirection = startPathTile.Direction;
        var startTile = startPathTile.PathTile;
        var tiles = GetOwningTileGroup(startTile, tileGroups);

        return GetGroupPoints(tiles, walkableGroups, startTile, startDirection);
    }

    public MapSegmentTraverseResult FindMatchingStartTile(List<HashSet<MapSegmentPathTile>> tileGroups, HashSet<MapSegmentPathTile> currentWalkableGroup, List<HashSet<MapSegmentPathTile>> walkableGroups)
    {
        foreach (var tileGroup in tileGroups) {
            foreach (var tile in tileGroup) {
                if (currentWalkableGroup.Contains(tile.NeighbourUp)) {
                    return new MapSegmentTraverseResult { PathTile = tile, Direction = MapSegmentDirection.Up };
                }else if (currentWalkableGroup.Contains(tile.NeighbourDown)) {
                    return new MapSegmentTraverseResult { PathTile = tile, Direction = MapSegmentDirection.Down };
                } else if (currentWalkableGroup.Contains(tile.NeighbourLeft)) {
                    return new MapSegmentTraverseResult { PathTile = tile, Direction = MapSegmentDirection.Left };
                } else if (currentWalkableGroup.Contains(tile.NeighbourRight)) {
                    return new MapSegmentTraverseResult { PathTile = tile, Direction = MapSegmentDirection.Right };
                }
            }
        }

        return null;
    }

    public ICollection<MapSegmentPathTile> GetOwningTileGroup(MapSegmentPathTile tile, List<HashSet<MapSegmentPathTile>> tileGroups)
    {
        foreach(var tileGroup in tileGroups) {
            if (tileGroup.Contains(tile)) {
                return tileGroup;
            }
        }

        return null;
    }

    public List<Vector2> GetGroupPoints(ICollection<MapSegmentPathTile> tiles, List<HashSet<MapSegmentPathTile>> walkableGroups, MapSegmentPathTile startTile, MapSegmentDirection startDirection)
    {
        var result = new List<Vector2>();
        var startNode = new MapSegmentTraverseResult { PathTile = startTile, Direction = startDirection };

        var currentNode = startNode;
        var point = startNode.PathTile.GetStartPoint(MapSegmentDirection.Up).ToVector2();
        result.Add(point);

        while (!CheckTerminationCondition(currentNode, startNode, result)) {
            point = currentNode.PathTile.GetEndPoint(currentNode.Direction).ToVector2();
            result.Add(point);
            currentNode = GetNextNode(currentNode, walkableGroups);
        }

        return result;
    }

    public bool CheckTerminationCondition(MapSegmentTraverseResult lastResult, MapSegmentTraverseResult startNode, List<Vector2> points)
    {
        if(points.Count > MAX_POINTS_BEFORE_ERROR) {
            Debug.LogError("Error when generating mapsegment colliders. Points in mesh exceeds " + MAX_POINTS_BEFORE_ERROR);

            return true;
        }

        // These null-guards works against dveelopment error and step-by-step implementation of this algorithm
        if(lastResult == null || startNode == null) {
            return true;
        }

        if(lastResult.PathTile ==  null || startNode.PathTile == null){
            return true;
        }

        if(lastResult.PathTile == startNode.PathTile && lastResult.Direction == startNode.Direction) {
            // Even when no loops of the while-loop has been executed, a single, startnode will be present in this list.
            if(points.Count > 1) {
                return true;
            }
        }

        return false;
    }

    public MapSegmentPathTile FindStartTile(ICollection<MapSegmentPathTile> tiles, MapSegmentDirection direction)
    {
        foreach (var tile in tiles) {
            if (MapSegmentPathTile.IsFree(tile.NeighbourUp)) {
                return tile;
            }
        }

        // In theory; this is not possible
        return null;
    }

    public MapSegmentTraverseResult GetNextNode(MapSegmentTraverseResult lastStep, List<HashSet<MapSegmentPathTile>> walkableGroups)
    {
        if(lastStep.Direction == MapSegmentDirection.Up) {
            var nextTile = lastStep.PathTile.NeighbourRight;
            if (MapSegmentPathTile.IsFree(nextTile)) {
                CheckWalkableGroups(nextTile, walkableGroups);
                return new MapSegmentTraverseResult { PathTile = lastStep.PathTile, Direction = MapSegmentDirection.Right };
            }else {
                if (MapSegmentPathTile.IsFree(nextTile.NeighbourUp)) {
                    CheckWalkableGroups(nextTile.NeighbourUp, walkableGroups);
                    return new MapSegmentTraverseResult { PathTile = nextTile, Direction = lastStep.Direction };
                }else {
                    return new MapSegmentTraverseResult { PathTile = nextTile.NeighbourUp, Direction = MapSegmentDirection.Left };
                }
            }
        }else if(lastStep.Direction == MapSegmentDirection.Right) {
            var nextTile = lastStep.PathTile.NeighbourDown;
            if (MapSegmentPathTile.IsFree(nextTile)) {
                CheckWalkableGroups(nextTile, walkableGroups);
                return new MapSegmentTraverseResult { PathTile = lastStep.PathTile, Direction = MapSegmentDirection.Down };
            }else {
                if (MapSegmentPathTile.IsFree(nextTile.NeighbourRight)) {
                    CheckWalkableGroups(nextTile.NeighbourRight, walkableGroups);
                    return new MapSegmentTraverseResult { PathTile = nextTile, Direction = lastStep.Direction };
                } else {
                    return new MapSegmentTraverseResult { PathTile = nextTile.NeighbourRight, Direction = MapSegmentDirection.Up };
                }
            }
        }else if(lastStep.Direction == MapSegmentDirection.Down) {
            var nextTile = lastStep.PathTile.NeighbourLeft;
            if (MapSegmentPathTile.IsFree(nextTile)) {
                CheckWalkableGroups(nextTile, walkableGroups);
                return new MapSegmentTraverseResult { PathTile = lastStep.PathTile, Direction = MapSegmentDirection.Left };
            }else {
                if (MapSegmentPathTile.IsFree(nextTile.NeighbourDown)) {
                    CheckWalkableGroups(nextTile.NeighbourDown, walkableGroups);
                    return new MapSegmentTraverseResult { PathTile = nextTile, Direction = lastStep.Direction };
                }else {
                    return new MapSegmentTraverseResult { PathTile = nextTile.NeighbourDown, Direction = MapSegmentDirection.Right };
                }
            }
        } else if (lastStep.Direction == MapSegmentDirection.Left) {
            var nextTile = lastStep.PathTile.NeighbourUp;
            if (MapSegmentPathTile.IsFree(nextTile)) {
                CheckWalkableGroups(nextTile, walkableGroups);
                return new MapSegmentTraverseResult { PathTile = lastStep.PathTile, Direction = MapSegmentDirection.Up };
            }else {
                if (MapSegmentPathTile.IsFree(nextTile.NeighbourLeft)) {
                    CheckWalkableGroups(nextTile.NeighbourLeft, walkableGroups);
                    return new MapSegmentTraverseResult { PathTile = nextTile, Direction = lastStep.Direction };
                }else {
                    return new MapSegmentTraverseResult { PathTile = nextTile.NeighbourLeft, Direction = MapSegmentDirection.Down };
                }
            }
        }

        // Theorically impossible but catches and handles errors
        // TODO: Implement exceptions and exception handling for this kind of errors
        return null;
    }

    public void CheckWalkableGroups(MapSegmentPathTile tile, List<HashSet<MapSegmentPathTile>> walkableGroups)
    {
        if(tile == null) {
            return;
        }

        foreach (var walkableGroup in walkableGroups.ToList()) {
            if (walkableGroup.Contains(tile)) {
                walkableGroups.Remove(walkableGroup);
            }
        }
    }
    
    public HashSet<MapSegmentPathTile> DepthFirstSearch(MapSegmentPathTile startTile, bool isWalkable)
    {
        var result = new HashSet<MapSegmentPathTile>();
        DepthFirstSearch_r(result, startTile, isWalkable);

        return result;
    }

    protected void DepthFirstSearch_r(HashSet<MapSegmentPathTile> addedTiles, MapSegmentPathTile currentTile, bool isWalkable)
    {
        if(currentTile == null) {
            return;
        }

        if (addedTiles.Contains(currentTile)) {
            return;
        }

        if (currentTile.IsWalkable != isWalkable) {
            return;
        }

        addedTiles.Add(currentTile);

        foreach(var tile in currentTile.GetNeighbours()) {
            DepthFirstSearch_r(addedTiles, tile, isWalkable);
        }
    }

    public MapSegmentPathTile GetTileOrDefault(Point point)
    {
        if (point.X < 0 || point.X >= Width) {
            return null;
        } else if (point.Y < 0 || point.Y >= Height) {
            return null;
        }

        return PathingMap[point.X, point.Y];
    }

    public override string ToString()
    {
        var result = "";

        for (int y = Height -1; y >= 0; y--) {
            for (int x = 0; x < Width; x++) {
                if(PathingMap[x, y].IsWalkable) {
                    result += "O";
                }else {
                    result += "X";
                }
            }

            result += "\n";
        }

        return result;
    }
}
