using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MapSegmentSelection
{
    public int Width { get; set; }
    public int Height { get; set; }

    public int[] InternalData { get; set; }

    public MapSegmentSelection()
    {
        Width = 0;
        Height = 0;

        InternalData = null;
    }

    public void SetSingleSelection(int tileType)
    {
        Width = 1;
        Height = 1;

        InternalData = new int[] { tileType };
    }

    public void SetSelection(IntVector2 startPoint, IntVector2 endPoint, MapSegmentLayer mapSegmentLayer)
    {
        var tileSetLayer = mapSegmentLayer.TileSetLayer;

        var minWidth = Mathf.Min(startPoint.X, endPoint.X);
        var maxWidth = Mathf.Max(startPoint.X, endPoint.X);

        var minHeight = Mathf.Min(startPoint.Y, endPoint.Y);
        var maxHeight = Mathf.Max(startPoint.Y, endPoint.Y);

        var deltaWidth = (maxWidth - minWidth) +1;
        var deltaHeight = (maxHeight - minHeight) + 1;

        Width = deltaWidth;
        Height = deltaWidth;

        var totalSize = deltaWidth * deltaHeight;
        InternalData = new int[totalSize];

        for (int y = 0; y < deltaHeight; y++) {
            for (int x = 0; x < deltaWidth; x++) {
                var tileSetX = minWidth + x;
                var tileSetY = minHeight + y;
                var currentTileIndex = (tileSetY * tileSetLayer.TileSetWidth) + tileSetX;
                var currentInternalIndex = (y * deltaWidth) + x;

                InternalData[currentInternalIndex] = currentTileIndex;
            }
        }
    }

    public bool Contains(int tileType)
    {
        if(InternalData == null) {
            return false;
        }

        return InternalData.Contains(tileType);
    }
}
