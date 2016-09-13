using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[System.Serializable]
public class TileTypeCollection
{
    public int[] InternalData;
    public int Width;
    public int Height;

    public TileTypeCollection(int width, int height)
    {
        Width = width;
        Height = height;

        InternalData = new int[width * height];
    }

    public TileTypeCollection Clone()
    {
        TileTypeCollection result = new TileTypeCollection(Width, Height);

        for(int i = 0; i < InternalData.Length; i ++){
            result.InternalData[i] = InternalData[i];
        }

        return result;
    }

    public int GetTileType(IntVector2 vector)
    {
        return GetTileType(vector.X, vector.Y);
    }

    public int GetTileType(Point point)
    {
        return GetTileType(point.X, point.Y);
    }

    public int GetTileType(int x, int y)
    {
        int index = y * Width + x;
        return InternalData[index];
    }

    public void SetTileType(int x, int y, int value)
    {  
        int index = y * Width + x;
        InternalData[index] = value;
    }
}
