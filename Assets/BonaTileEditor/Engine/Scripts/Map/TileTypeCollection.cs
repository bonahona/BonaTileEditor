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

    public TileTypeCollection()
    {
        Width = 0;
        Height = 0;
        InternalData = new int[0];
    }

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

    public void SetStartSelection(int tileType)
    {
        Width = 1;
        Height = 1;

        InternalData = new int[] { tileType };
    }

    public void UpdateSelection(IntVector2 start, IntVector2 end, TileSetLayer tileLayer)
    {
        Debug.Log(String.Format("{0}:{1}", start, end));

        return;
        int startX = Mathf.Min(start.X, end.X);
        int endX = Mathf.Max(start.X, end.X);

        // The Rows are drawn from the bottoms up hence the revserse order
        int startY = Mathf.Max(start.Y, end.Y);
        int endY = Mathf.Min(start.Y, end.Y);

        Width = (endX - startX) + 1;
        Height = (endY - startY) + 1;

        var totalSize = Width * Height;
        InternalData = new int[totalSize];

        // These two stores values baed from zero to be used when calculating array indices
        var currentX = 0;
        var currentY = 0;
        for(int y = startY; y <= end.Y; y++) {
            for(int x = startX; x <= endX; x++) {
                int currentIndex = (currentY * Height) + currentX;
                InternalData[currentIndex] = tileLayer.GetTileTypeId(x, y);
                currentX++;
            }
            currentY++;
        }
    }

    public bool Contains(int tileType)
    {
        return InternalData.Contains(tileType);
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
