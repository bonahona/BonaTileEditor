﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MapSegmentPaletteSelection
{
    public bool IsCustom { get; set; }           // If the selection is from the inspector window, its not custom. If its from the already painted tiles on a segment it is
    public int Width { get; set; }
    public int Height { get; set; }

    public int[] InternalData { get; set; }

    public MapSegmentPaletteSelection()
    {
        Clear();
    }

    public void Clear()
    {
        Width = 0;
        Height = 0;

        InternalData = null;
    }

    // Creates a new selection consiting of a single block
    public void SetSingleSelection(int tileType)
    {
        Width = 1;
        Height = 1;

        InternalData = new int[] { tileType };
    }

    public int GetSingleSelecttion()
    {
        if(Width == 1 && Height == 1) {
            return InternalData[0];
        }

        return -1;
    }

    // Scale down (if pssobile) the current selection to only cover the top left block
    public void SetToSingleSelection()
    {
        // Nothing to scale down
        if(Width <= 1 || Height <= 1) {
            return;
        }

        var tileIndexToKeep = (Height - 1) * Width;
        var tileToKeep = InternalData[tileIndexToKeep];

        InternalData = new int[] { tileToKeep };
        
        Width = 1;
        Height = 1;
    }

    public int GetIndexFor(int x, int y)
    {
        return (y * Width) + x;
    }

    public int GetTileType(int x, int y)
    {
        var currentIndex = GetIndexFor(x, y);
        return InternalData[currentIndex];
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
        Height = deltaHeight;

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
