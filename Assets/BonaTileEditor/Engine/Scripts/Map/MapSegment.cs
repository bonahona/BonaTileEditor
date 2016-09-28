using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MapSegment : MonoBehaviour {

    public TileSet TileSet;
    public int Width;
    public int Height;

    public Vector2 GridTileSize = new Vector2(1, 1);

    // Work details, so the get stored even when an object is briefly deselected and then reselected
    public MapSegmentLayer CurrentLayer { get; set; }
    public MapSegmentBrushType CurrentBrush { get; set; }
    public MapSegmentSelection CurrentTileSelection { get; set; }
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

    public bool HasPreviewObject()
    {
        var previewObject = GetComponentInChildren<MapSegmentPreview>();
        return (previewObject != null);
    }

    public MapSegmentPreview CreatePreviewObject()
    {
        var gameObject = new GameObject("Preview");
        gameObject.transform.parent = transform;
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
        gameObject.transform.localScale = Vector3.one;

        var result = gameObject.AddComponent<MapSegmentPreview>();
        return result;
    }

    public bool ValidateBounds(int offsetX, int offsetY, Point startPoint)
    {
        var currentX = startPoint.X + offsetX;
        var currentY = startPoint.Y + offsetY;

        if (currentX < 0 || currentX >= Width) {
            return false;
        }

        if (currentY < 0 || currentY >= Height) {
            return false;
        }

        return true;
    }

    public void Paint(Point point, MapSegmentSelection selection)
    {
        if (CurrentLayer == null) {
            return;
        }

        CurrentLayer.Paint(point, selection);
    }

    public List<MapSegmentLayer> GetOrderedLayerList()
    {
        var result = new List<MapSegmentLayer>();

        var mapSegmentLayers = GetComponentsInChildren<MapSegmentLayer>();
        var baseLayers = mapSegmentLayers.Where(m => m.TileSetLayer.LayerType == TileSetLayerType.BaseLayer).ToList();
        var overaysLayers = mapSegmentLayers.Where(m => m.TileSetLayer.LayerType == TileSetLayerType.Overlay).ToList();
        var onTopLayers = mapSegmentLayers.Where(m => m.TileSetLayer.LayerType == TileSetLayerType.OnTopOverlay).ToList();
        result.AddRange(baseLayers);
        result.AddRange(overaysLayers);
        result.AddRange(onTopLayers);

        return result; 
    }

    public MapSegmentPathing GetMapSegmentPathing()
    {
        var result = new MapSegmentPathing(Width, Height);
        var mapSegmentLayers = GetOrderedLayerList();

        // Get a complete map of all walkable and unwalkable tiles
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var currentPoint = new Point(x, y);
                result.SetCordinate(x, y, GetPathTileForCordinate(currentPoint, mapSegmentLayers));
            }
        }

        return result;
    }

    public MapSegmentPathTile GetPathTileForCordinate(Point point, List<MapSegmentLayer> layers)
    {
        var result = new MapSegmentPathTile();

        var isWalkable = false;

        var test = "====\n";

        foreach (var layer in layers) {
            var tileType = layer.TilesCollection.GetTileType(point.ToInt2Vector());

            test += layer.TileSetLayer.Tiles[tileType].Pathing.ToString() + "\n";
            // Base layer overrides
            if(layer.TileSetLayer.Tiles[tileType].Pathing == TilePathing.BaseWalkable) {
                isWalkable = true;
            } else if (layer.TileSetLayer.Tiles[tileType].Pathing == TilePathing.OverlayUnwalkable) {
                isWalkable = false;
            } else if (layer.TileSetLayer.Tiles[tileType].Pathing == TilePathing.OverlayWalkable) {
                isWalkable = true;
            }
        }

        Debug.Log(test);


        result.IsWalkable = isWalkable;
        return result;
    }
}
