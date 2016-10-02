using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Material))]
[RequireComponent(typeof(BoxCollider))]
public class MapSegmentLayer : MonoBehaviour {

    // Internal state variables
    public MapSegment MapSegment;
    public TileSetLayer TileSetLayer;
    public TileTypeCollection TilesCollection;

    public MeshFilter MeshFilter { get; set; }

    public MeshFilter GetMeshFilter()
    {
        if (MeshFilter == null) {
            MeshFilter = GetComponent<MeshFilter>();
        }

        return MeshFilter;
    }

    public void Paint(Point point, MapSegmentPaletteSelection selection)
    {
        var meshFilter = GetMeshFilter();
        var meshUvs = meshFilter.sharedMesh.uv;

        for (int y = 0; y < selection.Height; y++) {
            for (int x = 0; x < selection.Width; x++) {
                if (MapSegment.ValidateBounds(x, y, point)) {
                    var currentX = point.X + x;
                    var currentY = point.Y - y;

                    // The tiles are counted from down to up in the inspector, so this needs to be inverted
                    var tileY = (selection.Height - 1) - y;

                    var tileType = selection.GetTileType(x, tileY);
                    var tile = TileSetLayer.Tiles[tileType];

                    SetTileTypeToTile(currentX, currentY, tile, tileType, MapSegment, meshUvs);
                }
            }
        }

        meshFilter.sharedMesh.uv = meshUvs;
    }

    public void SetTileTypeToTile(int x, int y, Tile tile, int tileType, MapSegment mapSegment, Vector2[] meshUvs)
    {
        int uvOffset = ((y * mapSegment.Width) + x) * 4;

        for (int i = 0; i < 4; i++) {
            meshUvs[uvOffset + i] = tile.UvCords[i];
        }

        TilesCollection.SetTileType(x, y, tileType);
    }

    public List<Point> FindAllAdjecentTilesOfType(Point startPoint, int tileType)
    {
        HashSet<Point> result = new HashSet<Point>();
        SearchDepthFirst(startPoint, result, tileType);

        return new List<Point>(result);
    }

    public void SearchDepthFirst(Point currentPoint, HashSet<Point> result, int tileTypeId)
    {
        // If the result set already contains this, the tile has already been processedes
        if (result.Contains(currentPoint)) {
            return;
        }

        // We are looking outside the map segments max and min values regarding width and height
        if (!IsWithinBounds(currentPoint)) {
            return;
        }

        // This is not the tiletype where looking for
        var currentTileTypeId = TilesCollection.GetTileType(currentPoint);
        if (currentTileTypeId != tileTypeId) {
            return;
        }

        result.Add(currentPoint);

        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                if ((x != 0 || y != 0) && Mathf.Abs(x) != Mathf.Abs(y)) {
                    Point tmpPoint = new Point(currentPoint.X + x, currentPoint.Y + y);
                    SearchDepthFirst(tmpPoint, result, tileTypeId);
                }
            }
        }
    }

    public bool IsWithinBounds(Point point)
    {
        if (point.X < 0 || point.Y < 0) {
            return false;
        }

        if (point.X >= MapSegment.Width || point.Y >= MapSegment.Height) {
            return false;
        }

        return true;
    }
}
