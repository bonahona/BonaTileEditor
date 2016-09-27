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

    public void Paint(Point point, MapSegmentSelection selection)
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
        Debug.Log(new Point(x, y));

        int uvOffset = ((y * mapSegment.Width) + x) * 4;

        for (int i = 0; i < 4; i++) {
            meshUvs[uvOffset + i] = tile.UvCords[i];
        }

        TilesCollection.SetTileType(x, y, tileType);
    }
}
