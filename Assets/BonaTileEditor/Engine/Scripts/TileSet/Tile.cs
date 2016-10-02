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

    public Tile()
    {
        X = 0;
        Y = 0;
        UvCords = new Vector2[0];
        Rect = new Rect();
        Pathing = TilePathing.BaseUnwalkable;
    }

    public Tile(Tile other)
    {
        CopyFrom(other);
    }

    public void CopyFrom(Tile other)
    {
        X = other.X;
        Y = other.Y;
        Rect = other.Rect;
        Pathing = other.Pathing;

        UvCords = new Vector2[other.UvCords.Length];
        for (int i = 0; i < UvCords.Length; i++) {
            UvCords[i] = other.UvCords[i];
        }
    }
}
