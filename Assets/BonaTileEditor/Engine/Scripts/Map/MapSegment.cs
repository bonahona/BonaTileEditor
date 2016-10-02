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
        var currentY = startPoint.Y + offsetY;

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

    public IntVector2 GetTilePosition(Vector3 hitCordinate)
    {
        var result = new IntVector2();

        var localHit = transform.InverseTransformPoint(hitCordinate);
        result.X = Mathf.FloorToInt(localHit.x / GridTileSize.x);
        result.Y = Mathf.FloorToInt(localHit.y / GridTileSize.y);

        return result;
    }
}
