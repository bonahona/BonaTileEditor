using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(TileSet))]
public class TileSetEditor : Editor
{
    const string TILESET_WIDTH_TOOLTIP = "Number of tiles in a row in the tileset";
    const string TILESET_HEIGHT_TOOLTIP = "Number of tiles in a column in the tileset";

    public TileSet TileSet;
    public List<TileSetLayer> LocalLayers;

    public GUIStyle GuiStyle;

    void OnEnable()
    {
        GuiStyle = new GUIStyle();
        GuiStyle.alignment = TextAnchor.MiddleCenter;
        GuiStyle.fontSize = 18;
        GuiStyle.fontStyle = FontStyle.Bold;

        Reset();
    }

    public void Reset()
    {
        TileSet = (TileSet)target;
        LocalLayers = new List<TileSetLayer>();
        foreach (var entry in TileSet.Layers) {
            var tmpEntry = new TileSetLayer(entry);
            tmpEntry.Applied = true;
            LocalLayers.Add(tmpEntry);
        }
    }

    public List<string> GetGuids()
    {
        var result = new List<string>();
        foreach(var layer in TileSet.Layers) {
            result.Add(layer.Guid);
        }

        return result;
    }

    public bool ContainsLayer(TileSetLayer layer)
    {
        return (GetGuids().Contains(layer.Guid));
    }

    public TileSetLayer GetLayerFromGuid(string guid)
    {
        foreach(var layer in TileSet.Layers) {
            if(layer.Guid == guid) {
                return layer;
            }
        }

        return null;
    }

    public void Apply()
    {
        if (!ValidateTileSet()) {
            return;
        }

        foreach (var entry in LocalLayers) {
            if (!entry.Applied) {
                if (!entry.Apply()) {
                    return;
                }
            }
        }

        foreach (var layer in LocalLayers) {
            var tileSetEntry = GetLayerFromGuid(layer.Guid);
            if(tileSetEntry == null) {
                tileSetEntry = new TileSetLayer(layer);
                TileSet.Layers.Add(tileSetEntry);
            }else {
                tileSetEntry.CopyFrom(layer);
            }
        }

        // Make sure the new tileset is saved
        EditorUtility.SetDirty(target);
    }

    public bool ValidateTileSet()
    {
        var baseLayerCount = 0;
        foreach(var entry in LocalLayers) {
            if(entry.LayerType == TileSetLayerType.BaseLayer) {
                baseLayerCount++;
            }
        }

        if(baseLayerCount > 1) {
            Debug.LogError("Unable to apply tileset changes. Tileset contains multiple baselayers. Allowed amount is 0-1");
            return false;
        }

        return true;
    }

    public void AddLayer()
    {
        var localLayer = new TileSetLayer();
        localLayer.IsOpenInEditor = true;
        LocalLayers.Add(localLayer);

        this.Repaint();
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Add layer")) {
            AddLayer();
        }

        foreach (var entry in LocalLayers) {
            entry.IsOpenInEditor = EditorGUILayout.Foldout(entry.IsOpenInEditor, entry.Name);

            if (entry.IsOpenInEditor) {
                DrawLayerEditor(entry);
            }

            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("Apply")) {
            Apply();
        }

        if (GUILayout.Button("Reset")) {
            Reset();
        }
    }

    public void DrawLayerEditor(TileSetLayer layer)
    {
        var tmpName = EditorGUILayout.TextField("Layer name", layer.Name);
        var tmpLayerType = (TileSetLayerType)EditorGUILayout.EnumPopup("Type", layer.LayerType);
        var tmpWidth = EditorGUILayout.IntField(new GUIContent("Width", TILESET_WIDTH_TOOLTIP), layer.TileSetWidth);
        var tmpHeight = EditorGUILayout.IntField(new GUIContent("Height", TILESET_HEIGHT_TOOLTIP), layer.TileSetHeight);
        var tmpTexture = (Texture2D)EditorGUILayout.ObjectField("Texture", layer.Texture, typeof(Texture2D), false);

        // Detect changes
        if (tmpName != layer.Name || tmpLayerType != layer.LayerType || tmpWidth != layer.TileSetWidth || tmpHeight != layer.TileSetHeight || tmpTexture != layer.Texture) {
            layer.Applied = false;
        }

        layer.Name = tmpName;
        layer.LayerType = tmpLayerType;
        layer.TileSetWidth = tmpWidth;
        layer.TileSetHeight = tmpHeight;
        layer.Texture = tmpTexture;

        if (layer.Applied && layer.Texture != null) {
            DrawLayerTileEditor(layer);
        }
    }

    public void DrawLayerTileEditor(TileSetLayer layer)
    {
        if (layer.TileSetWidth <= 0 || layer.TileSetHeight <= 0) {
            return;
        }

        int bufferSize = 32;
        int windowSize = Screen.width - bufferSize;
        int tileWidth = (int)Mathf.Floor(windowSize / layer.TileSetWidth);

        GUI.BeginGroup(GUILayoutUtility.GetRect(windowSize, layer.TileSetHeight * tileWidth));
        int height = layer.TileSetHeight * tileWidth;
        for (int y = 0; y < layer.TileSetHeight; y++) {
            for (int x = 0; x < layer.TileSetWidth; x++) {
                int num = (y * layer.TileSetWidth) + x;

                Rect rect = new Rect(x * tileWidth, height - ((y + 1) * tileWidth), tileWidth, tileWidth);
                GUI.DrawTextureWithTexCoords(rect, layer.Texture, layer.Tiles[num].Rect);

                string buttonText = "";
                if (layer.Tiles[num].Pathing == TilePathing.BaseUnwalkable) {
                    buttonText = "X";
                } else if (layer.Tiles[num].Pathing == TilePathing.BaseWalkable) {
                    buttonText = "O";
                } else if (layer.Tiles[num].Pathing == TilePathing.OverlayInherit) {
                    buttonText = "-";
                } else if (layer.Tiles[num].Pathing == TilePathing.OverlayUnwalkable) {
                    buttonText = "U";
                } else if (layer.Tiles[num].Pathing == TilePathing.OverlayWalkable) {
                    buttonText = "W";
                }

                if (GUI.Button(rect, buttonText, GuiStyle)) {
                    CycleType(layer, layer.Tiles[num]);
                }
            }
        }

        GUI.EndGroup();
    }

    public void CycleType(TileSetLayer layer, Tile tile)
    {
        if (layer.LayerType == TileSetLayerType.BaseLayer) {
            if (tile.Pathing == TilePathing.BaseUnwalkable) {
                tile.Pathing = TilePathing.BaseWalkable;
            } else if (tile.Pathing == TilePathing.BaseWalkable) {
                tile.Pathing = TilePathing.BaseUnwalkable;
            } else {
                tile.Pathing = TilePathing.BaseUnwalkable;
            }
        } else if (layer.LayerType == TileSetLayerType.OnTopOverlay || layer.LayerType == TileSetLayerType.Overlay) {
            if (tile.Pathing == TilePathing.OverlayInherit) {
                tile.Pathing = TilePathing.OverlayUnwalkable;
            } else if (tile.Pathing == TilePathing.OverlayUnwalkable) {
                tile.Pathing = TilePathing.OverlayWalkable;
            } else if (tile.Pathing == TilePathing.OverlayWalkable) {
                tile.Pathing = TilePathing.OverlayInherit;
            } else {
                tile.Pathing = TilePathing.OverlayInherit;
            }
        } else {
            tile.Pathing = TilePathing.BaseUnwalkable;
        }
    }
}