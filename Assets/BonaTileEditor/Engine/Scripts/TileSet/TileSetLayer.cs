using UnityEngine;
using System.Collections;
using System;

[System.Serializable]
public class TileSetLayer
{
    public String Guid;
    public string Name;
    public TileSetLayerType LayerType;


    public int TileSetWidth;
    public int TileSetHeight;
    public Texture2D Texture;

    public Vector2 UvOffsetSize;
    public bool Applied = true;

    public Tile[] Tiles;

    // Make this serialized to, to make the editor easier to use
    public bool IsOpenInEditor;

    public TileSetLayer()
    {
        this.Guid = System.Guid.NewGuid().ToString();
        this.Name = "Unnamed layer";
    }

    public TileSetLayer(TileSetLayer other)
    {
        Tiles = new Tile[0];
        CopyFrom(other);
    }

    public void CopyFrom(TileSetLayer other)
    {
        Guid = other.Guid;
        Name = other.Name;
        TileSetWidth = other.TileSetWidth;
        TileSetHeight = other.TileSetHeight;
        LayerType = other.LayerType;
        Texture = other.Texture;
        UvOffsetSize = other.UvOffsetSize;

        Tiles = CopyTiles(Tiles, other.Tiles.Length);
        for (int i = 0; i < Tiles.Length; i++) {
            Tiles[i].CopyFrom(other.Tiles[i]);
        }

        Applied = false;
    }

    public Tile[] CopyTiles(Tile[] original, int length)
    {
        var result = new Tile[length];

        for (int i = 0; i < length; i++) {
            if (i < original.Length) {
                result[i] = original[i];
            }else {
                result[i] = new Tile();
            }
        }
        return result;
    }

    public void SetDefaultLayer(TileSetLayerType layerType, Tile[] tiles)
    {
        if (layerType == TileSetLayerType.BaseLayer) {
            foreach (var tile in tiles) {
                tile.Pathing = TilePathing.BaseUnwalkable;
            }
        } else if (layerType == TileSetLayerType.OnTopOverlay || layerType == TileSetLayerType.Overlay) {
            foreach (var tile in tiles) {
                tile.Pathing = TilePathing.OverlayInherit;
            }
        }

        LayerType = layerType;
    }

    public bool Apply()
    {

        if (TileSetWidth <= 0 || TileSetHeight <= 0) {
            Debug.LogError("Unable to apply tileset changes: The width and height must be above 0");
            return false;
        }

        if (Texture == null) {
            Debug.LogError("Unable to apply tileset changes: Texture can't be null");
            return false;
        }

        int tileCount = TileSetWidth * TileSetHeight;
        var tiles = new Tile[tileCount];

        UvOffsetSize = new Vector2();
        UvOffsetSize.x = 1.0f / TileSetWidth;
        UvOffsetSize.y = 1.0f / TileSetHeight;

        for (int y = 0; y < TileSetHeight; y++) {
            for (int x = 0; x < TileSetWidth; x++) {
                int num = (y * TileSetWidth) + x;
                tiles[num] = new Tile { X = x, Y = y };
                tiles[num].UvCords = new Vector2[4];
                tiles[num].UvCords[0] = new Vector2(x * UvOffsetSize.x, y * UvOffsetSize.y);
                tiles[num].UvCords[1] = new Vector2((x + 1) * UvOffsetSize.x, y * UvOffsetSize.y);
                tiles[num].UvCords[2] = new Vector2(x * UvOffsetSize.x, (y + 1) * UvOffsetSize.y);
                tiles[num].UvCords[3] = new Vector2((x + 1) * UvOffsetSize.x, (y + 1) * UvOffsetSize.y);
                tiles[num].Rect = new Rect(x * UvOffsetSize.x, y * UvOffsetSize.y, UvOffsetSize.x, UvOffsetSize.y);
            }
        }

        SetDefaultLayer(LayerType, tiles);
        Tiles = tiles;
        Applied = true;
        return true;
    }

    public int GetTileTypeId(int x, int y)
    {
        return (y * TileSetWidth + x);
    }
}
