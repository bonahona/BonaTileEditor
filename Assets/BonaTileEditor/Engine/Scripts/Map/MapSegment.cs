using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapSegment : MonoBehaviour {

    public TileSet TileSet;
    public int Width;
    public int Height;

    public Vector2 GridTileSize = new Vector2(1, 1);

    // Work details, so the get stored even when an object is briefly deselected and then reselected
    public MapSegmentLayer CurrentLayer { get; set; }
    public MapSegmentBrushType CurrentBrush { get; set; }
    public Tile CurrentTile { get; set; }
    public int CurrentTileId { get; set; }


    public bool HasLayerOfType(TileSetLayer tileSetLayer)
    {
        // The layers list might be out of date, so fetch the latest list from the actuall children
        var tmpLayers = GetComponentsInChildren<MapSegmentLayer>();

        foreach (var layer in tmpLayers) {
            if (layer.TileSetLayer.Guid == tileSetLayer.Guid) {
                return true;
            }
        }


        return false;
    }
}
