﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[CreateAssetMenu( fileName = "New Tileset", menuName = "Bona Tile Editor/Tileset")]
public class TileSet : ScriptableObject
{
    public List<TileSetLayer> Layers;
    public List<MapSegment> MapSegments;        // List of all mapsegments using this tileset

    public TileSet()
    {
        Layers = new List<TileSetLayer>();
        MapSegments = new List<MapSegment>();
    }

    public void AddMapSegment(MapSegment mapSegment)
    {
        if (!MapSegments.Contains(mapSegment)) {
            MapSegments.Add(mapSegment);
        }
    }

    public void ApplyToMapSegments()
    {
        // Remove all mapsegments that is null
        foreach(var mapSegment in MapSegments.ToList()) {
            if(mapSegment == null) {
                MapSegments.Remove(mapSegment);
            }
        }
    }

    public bool HasLayer(TileSetLayer segmentLayer)
    {
        foreach(var layer in Layers) {
            if(layer.Guid == segmentLayer.Guid) {
                return true;
            }
        }

        return false;
    }
}