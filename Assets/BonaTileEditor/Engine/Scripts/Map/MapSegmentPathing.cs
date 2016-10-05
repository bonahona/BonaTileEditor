﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MapSegmentPathing
{
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

    public List<Vector2> GetPoints()
    {
        var result = new List<Vector2>();

        var tileGroups = GetGroups();

        foreach (var group in tileGroups) {
            var points = GetGroupPoints(group);
            result = points;
            return result;
        }

        return result;
    }

    public List<List<MapSegmentPathTile>> GetGroups()
    {
        var checkedTiles = new HashSet<MapSegmentPathTile>();

        var result = new List<List<MapSegmentPathTile>>();

        foreach(var tile in GetAll()) {
            if (!tile.IsWalkable && !checkedTiles.Contains(tile)) {
                var tileGroup = DepthFirstSearch(tile);
                foreach (var groupTile in tileGroup) {
                    checkedTiles.Add(groupTile);
                }

                result.Add(tileGroup);
            }
        }

        return result;
    }

    public List<Vector2> GetGroupPoints(List<MapSegmentPathTile> tiles)
    {
        var result = new List<Vector2>();
        var startTile = FindStartTile(tiles, MapSegmentDirection.Up);
        var startNode = new MapSegmentTraverseResult { PathTile = startTile, Direction = MapSegmentDirection.Up };

        result.Add(startNode.PathTile.GetStartPoint(MapSegmentDirection.Up).ToVector2());

        var currentNode = new MapSegmentTraverseResult { PathTile = startTile, Direction = MapSegmentDirection.Up };
        while(!CheckTerminationCondition(currentNode, startNode, result)) {
            result.Add(currentNode.PathTile.GetEndPoint(currentNode.Direction).ToVector2());
            currentNode = GetNextNode(currentNode);
        }

        Debug.Log(result.Count);

        foreach (var point in result) {
            Debug.Log(point);

        }
        return result;
    }

    public bool CheckTerminationCondition(MapSegmentTraverseResult lastResult, MapSegmentTraverseResult startNode, List<Vector2> points)
    {
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

    public MapSegmentPathTile FindStartTile(List<MapSegmentPathTile> tiles, MapSegmentDirection direction)
    {
        foreach (var tile in tiles) {
            if (tile.IsNeighbourFree(direction)) {
                return tile;
            }
        }

        // In theory; this is not possible
        return null;
    }

    public MapSegmentTraverseResult GetNextNode(MapSegmentTraverseResult lastStep)
    {
        if(lastStep.Direction == MapSegmentDirection.Up) {
            var nextTile = lastStep.PathTile.NeighbourRight;
            if (MapSegmentPathTile.IsFree(nextTile)) {
                return new MapSegmentTraverseResult { PathTile = lastStep.PathTile, Direction = MapSegmentDirection.Right };
            }else {
                return new MapSegmentTraverseResult { PathTile = nextTile, Direction = lastStep.Direction };
            }
        }else if(lastStep.Direction == MapSegmentDirection.Right) {
            var nextTile = lastStep.PathTile.NeighbourDown;
            if (MapSegmentPathTile.IsFree(nextTile)) {
                return new MapSegmentTraverseResult { PathTile = lastStep.PathTile, Direction = MapSegmentDirection.Down };
            }else {
                return new MapSegmentTraverseResult { PathTile = nextTile, Direction = lastStep.Direction };
            }
        }else if(lastStep.Direction == MapSegmentDirection.Down) {
            var nextTile = lastStep.PathTile.NeighbourLeft;
            if (MapSegmentPathTile.IsFree(nextTile)) {
                return new MapSegmentTraverseResult { PathTile = lastStep.PathTile, Direction = MapSegmentDirection.Left };
            }else {
                return new MapSegmentTraverseResult { PathTile = nextTile, Direction = lastStep.Direction };
            }
        } else if (lastStep.Direction == MapSegmentDirection.Left) {
            var nextTile = lastStep.PathTile.NeighbourUp;
            if (MapSegmentPathTile.IsFree(nextTile)) {
                return new MapSegmentTraverseResult { PathTile = lastStep.PathTile, Direction = MapSegmentDirection.Up };
            }else {
                return new MapSegmentTraverseResult { PathTile = nextTile, Direction = lastStep.Direction };
            }
        }

        // Theorically impossible but catches and handles errors
        // TODO: Implement exceptions and exception handling for this kind of errors
        return null;
    }
    
    public List<MapSegmentPathTile> DepthFirstSearch(MapSegmentPathTile startTile)
    {
        var hashSet = new HashSet<MapSegmentPathTile>();
        DepthFirstSearch_r(hashSet, startTile);

        return hashSet.ToList();
    }

    protected void DepthFirstSearch_r(HashSet<MapSegmentPathTile> addedTiles, MapSegmentPathTile currentTile)
    {
        if(currentTile == null) {
            return;
        }

        if (addedTiles.Contains(currentTile)) {
            return;
        }

        if (currentTile.IsWalkable) {
            return;
        }

        addedTiles.Add(currentTile);

        foreach(var tile in currentTile.GetNeighbours()) {
            DepthFirstSearch_r(addedTiles, tile);
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

    public bool IsWalkable(Point point)
    {
        if(point.X < 0 || point.X >= Width) {
            return true;
        }else if(point.Y < 0 || point.Y >= Height) {
            return true;
        }

        return PathingMap[point.X, point.Y].IsWalkable;
    }

    public List<Vector2> GetLines()
    {
        var result = new List<Vector2>();
        return result;
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
