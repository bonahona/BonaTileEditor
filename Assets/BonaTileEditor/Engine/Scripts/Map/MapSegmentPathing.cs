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
            tile.Left = GetTileOrDefault(new Point(tile.Point.X - 1, tile.Point.Y));
            tile.Right = GetTileOrDefault(new Point(tile.Point.X + 1, tile.Point.Y));
            tile.Up = GetTileOrDefault(new Point(tile.Point.X, tile.Point.Y - 1));
            tile.Down = GetTileOrDefault(new Point(tile.Point.X, tile.Point.Y + 1));
        }

        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var currentTile = PathingMap[x, y];
                if (!currentTile.IsWalkable) {
                    if(IsWalkable(new Point(x - 1, y))) {
                        currentTile.Directions = (currentTile.Directions | MapSegmentDirection.Left);
                    }
                    if (IsWalkable(new Point(x + 1, y))) {
                        currentTile.Directions = (currentTile.Directions | MapSegmentDirection.Right);
                    }
                    if (IsWalkable(new Point(x, y - 1))) {
                        currentTile.Directions = (currentTile.Directions | MapSegmentDirection.Up);
                    }
                    if (IsWalkable(new Point(x, y + 1))) {
                        currentTile.Directions = (currentTile.Directions | MapSegmentDirection.Down);
                    }
                }
            }
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
        var startTile = FindStartTile(tiles);
        var currentTile = startTile;
        var startDirection = MapSegmentDirection.Up;
        var currentDirection = MapSegmentDirection.Up;

        result.Add(startTile.GetStartPoint(MapSegmentDirection.Up).ToVector2());

        while(currentTile != startTile || currentDirection != startDirection || result.Count == 1) {
            currentTile = FindLastTileInLine(currentTile, currentDirection);
            result.Add(currentTile.GetEndPoint(currentDirection).ToVector2());
            currentDirection = GetNextDirection(currentTile, currentDirection);
        }
        
        return result;
    }

    public MapSegmentDirection GetNextDirection(MapSegmentPathTile startTile, MapSegmentDirection currentDirection)
    {
        if(currentDirection == MapSegmentDirection.Up) {
            if((startTile.Directions & MapSegmentDirection.Right) == MapSegmentDirection.Right) {
                return MapSegmentDirection.Right;
            }
            if ((startTile.Directions & MapSegmentDirection.Left) == MapSegmentDirection.Left) {
                return MapSegmentDirection.Right;
            }
            if ((startTile.Directions & MapSegmentDirection.Down) == MapSegmentDirection.Down) {
                return MapSegmentDirection.Right;
            }
        } else if (currentDirection == MapSegmentDirection.Right) {
            if ((startTile.Directions & MapSegmentDirection.Down) == MapSegmentDirection.Down) {
                return MapSegmentDirection.Down;
            }
            if ((startTile.Directions & MapSegmentDirection.Up) == MapSegmentDirection.Up) {
                return MapSegmentDirection.Up;
            }
            if ((startTile.Directions & MapSegmentDirection.Left) == MapSegmentDirection.Left) {
                return MapSegmentDirection.Left;
            }
        } else if (currentDirection == MapSegmentDirection.Down) {
            if ((startTile.Directions & MapSegmentDirection.Left) == MapSegmentDirection.Left) {
                return MapSegmentDirection.Left;
            }
            if ((startTile.Directions & MapSegmentDirection.Right) == MapSegmentDirection.Right) {
                return MapSegmentDirection.Right;
            }
            if ((startTile.Directions & MapSegmentDirection.Up) == MapSegmentDirection.Up) {
                return MapSegmentDirection.Up;
            }
        } else if (currentDirection == MapSegmentDirection.Left) {
            if ((startTile.Directions & MapSegmentDirection.Up) == MapSegmentDirection.Up) {
                return MapSegmentDirection.Up;
            }
            if ((startTile.Directions & MapSegmentDirection.Down) == MapSegmentDirection.Down) {
                return MapSegmentDirection.Down;
            }
            if ((startTile.Directions & MapSegmentDirection.Right) == MapSegmentDirection.Right) {
                return MapSegmentDirection.Right;
            }

        }

        return MapSegmentDirection.None;
    }

    public MapSegmentPathTile FindStartTile(List<MapSegmentPathTile> tiles)
    {
        MapSegmentPathTile tmpStartTile = null;
        foreach (var tile in tiles) {
            if((tile.Directions & MapSegmentDirection.Up) == MapSegmentDirection.Up) {
                tmpStartTile = tile;
            }
        }

        return FindFirstTileInLine(tmpStartTile, MapSegmentDirection.Up);
    }

    public MapSegmentPathTile FindFirstTileInLine(MapSegmentPathTile tile, MapSegmentDirection direction)
    {
        var tmpTile = tile;
        var result = tile;

        while(tmpTile != null && (tmpTile.Directions & direction) == direction) {
            result = tmpTile;
            tmpTile = GetPreviousTileInDirection(result, direction);
        }

        return result;
    }

    public MapSegmentPathTile FindLastTileInLine(MapSegmentPathTile tile, MapSegmentDirection direction)
    {
        var tmpTile = tile;
        var result = tile;

        while (tmpTile != null && (tmpTile.Directions & direction) == direction) {
            result = tmpTile;
            tmpTile = GetNextTileInDirection(result, direction);
        }

        return result;
    }

    public MapSegmentPathTile GetPreviousTileInDirection(MapSegmentPathTile tile, MapSegmentDirection direction)
    {
        if (direction == MapSegmentDirection.Up) {
            return tile.Left;
        } else if (direction == MapSegmentDirection.Down) {
            return tile.Right;
        } else if (direction == MapSegmentDirection.Left) {
            return tile.Down;
        } else if (direction == MapSegmentDirection.Right) {
            return tile.Up;
        }

        return null;
    }

    public MapSegmentPathTile GetNextTileInDirection(MapSegmentPathTile tile, MapSegmentDirection direction)
    {
        if(direction == MapSegmentDirection.Up) {
            return tile.Right;
        } else if(direction == MapSegmentDirection.Down) {
            return tile.Left;
        } else if (direction == MapSegmentDirection.Left) {
            return tile.Up;
        } else if (direction == MapSegmentDirection.Right) {
            return tile.Down;
        }

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
