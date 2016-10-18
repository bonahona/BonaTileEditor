using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MapSegment : MonoBehaviour {

    public TileSet TileSet;
    public int Width;
    public int Height;

    public Vector2 GridTileSize = Vector2.one;

    // Work details, so the get stored even when an object is briefly deselected and then reselected
    public MapSegmentLayer CurrentLayer { get; set; }
    public MapSegmentBrushType CurrentBrush { get; set; }
    public MapSegmentPaletteSelection CurrentTileSelection { get; set; }
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

    public MapSegmentPreview GetPreviewObject()
    {
        var previewObject = GetComponentInChildren<MapSegmentPreview>();
        return previewObject;
    }

    public bool HasPreviewObject()
    {
        return GetPreviewObject() != null;
    }

    public MapSegmentPreview CreatePreviewObject()
    {
        var previewObject = new GameObject("Preview");
        previewObject.transform.parent = transform;
        previewObject.transform.localPosition = Vector3.zero;
        previewObject.transform.localRotation = Quaternion.identity;
        previewObject.transform.localScale = Vector3.one;

        var result = previewObject.AddComponent<MapSegmentPreview>();
        result.Init();
        result.MapSegment = this;

        return result;
    }

    public bool ValidateBounds(int offsetX, int offsetY, Point startPoint)
    {
        var currentX = startPoint.X + offsetX;
        var currentY = startPoint.Y - offsetY;

        if (currentX < 0 || currentX >= Width) {
            return false;
        }

        if (currentY < 0 || currentY >= Height) {
            return false;
        }

        return true;
    }

    public void Paint(Point point, MapSegmentPaletteSelection selection)
    {
        if (CurrentLayer == null) {
            return;
        }

        CurrentLayer.Paint(point, selection);
    }

    public List<Point> FindAllAdjecentTilesOfType(Point startPoint, int tileType)
    {
        return CurrentLayer.FindAllAdjecentTilesOfType(startPoint, tileType);
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

    public IntVector2 GetTilePosition(Vector3 hitCordinate)
    {
        var result = new IntVector2();

        var localHit = transform.InverseTransformPoint(hitCordinate);
        result.X = Mathf.FloorToInt(localHit.x / GridTileSize.x);
        result.Y = Mathf.FloorToInt(localHit.y / GridTileSize.y);

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

        foreach (var layer in layers) {
            var tileType = layer.TilesCollection.GetTileType(point.ToInt2Vector());

            // Base layer overrides
            if(layer.TileSetLayer.Tiles[tileType].Pathing == TilePathing.BaseWalkable) {
                isWalkable = true;
            } else if (layer.TileSetLayer.Tiles[tileType].Pathing == TilePathing.OverlayUnwalkable) {
                isWalkable = false;
            } else if (layer.TileSetLayer.Tiles[tileType].Pathing == TilePathing.OverlayWalkable) {
                isWalkable = true;
            }
        }

        result.IsWalkable = isWalkable;
        return result;
    }
}
