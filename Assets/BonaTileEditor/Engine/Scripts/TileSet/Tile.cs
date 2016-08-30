using UnityEngine;
using System.Collections;

[System.Serializable]
public class Tile
{
    public int X;
    public int Y;
    public Vector2[] UvCords;
    public Rect Rect;
    public TilePathing Pathing;

    public Tile Clone()
    {
        Tile result = new Tile();
        result.X = X;
        result.Y = Y;
        result.Rect = Rect;
        result.Pathing = Pathing;

        result.UvCords = new Vector2[UvCords.Length];

        for (int i = 0; i < UvCords.Length; i++) {
            result.UvCords[i] = UvCords[i];
        }

        return result;
    }
}
