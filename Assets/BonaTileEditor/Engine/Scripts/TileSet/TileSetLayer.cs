using UnityEngine;
using System.Collections;
using System;

[System.Serializable]
public class TileSetLayer {

    public String Guid;
    public string Name;
    public TileSetLayerType LayerType;
    public int TileSetWidth;
    public int TileSetHeight;
    public Texture2D Texture;

    public Vector2 UvOffsetSize;
    public bool Applied = true;

    public Tile[] Tiles;

    public TileSetLayer()
    {
        this.Guid = System.Guid.NewGuid().ToString();
        this.Name = "Unnamed layer";
    }

    public TileSetLayer Clone()
    {
        TileSetLayer result = new TileSetLayer();
        result.Guid = Guid;
        result.Name = Name;
        result.TileSetWidth = TileSetWidth;
        result.TileSetHeight = TileSetHeight;
        result.LayerType = LayerType;
        result.Texture = Texture;
        result.UvOffsetSize = UvOffsetSize;

        result.Tiles = new Tile[this.Tiles.Length];
        for (int i = 0; i < Tiles.Length; i++) {
            result.Tiles[i] = Tiles[i].Clone();
        }

        result.Applied = false;

        return result;
    }

    public void SetLayer(TileSetLayerType layerType)
    {
        if (layerType == TileSetLayerType.BaseLayer) {
            foreach (var tile in Tiles) {
                tile.Pathing = TilePathing.BaseUnwalkable;
            }
        } else if (layerType == TileSetLayerType.OnTopOverlay || layerType == TileSetLayerType.Overlay) {
            foreach (var tile in Tiles) {
                tile.Pathing = TilePathing.OverlayInherit;
            }
        }

        LayerType = layerType;
    }

    public bool Apply()
    {

        if (TileSetWidth <= 0 || TileSetHeight <= 0) {
            Debug.LogError("The width and height must be above 0");
            return false;
        }

        if (Texture == null) {
            Debug.LogError("Texture can't be null");
            return false;
        }

        int tileCount = TileSetWidth * TileSetHeight;
        Tiles = new Tile[tileCount];

        UvOffsetSize = new Vector2();
        UvOffsetSize.x = 1.0f / TileSetWidth;
        UvOffsetSize.y = 1.0f / TileSetHeight;

        for (int y = 0; y < TileSetHeight; y++) {
            for (int x = 0; x < TileSetWidth; x++) {
                int num = (y * TileSetWidth) + x;
                Tiles[num] = new Tile { X = x, Y = y };
                Tiles[num].UvCords = new Vector2[4];
                Tiles[num].UvCords[0] = new Vector2(x * UvOffsetSize.x, y * UvOffsetSize.y);
                Tiles[num].UvCords[1] = new Vector2((x + 1) * UvOffsetSize.x, y * UvOffsetSize.y);
                Tiles[num].UvCords[2] = new Vector2(x * UvOffsetSize.x, (y + 1) * UvOffsetSize.y);
                Tiles[num].UvCords[3] = new Vector2((x + 1) * UvOffsetSize.x, (y + 1) * UvOffsetSize.y);
                Tiles[num].Rect = new Rect(x * UvOffsetSize.x, y * UvOffsetSize.y, UvOffsetSize.x, UvOffsetSize.y);

            }
        }
        SetLayer(LayerType);

        Applied = true;
        return true;
    }
}
