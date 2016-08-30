﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(TileSet))]
public class TileSetEditor : Editor
{
    public TileSet TileSet;
    public List<TileSetLayer> LocalLayers;
    public List<bool> ShowFoldout;

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
        foreach(var entry in TileSet.Layers){
            var tmpEntry = entry.Clone();
            tmpEntry.Applied = true;
            LocalLayers.Add(tmpEntry);
        }

        ShowFoldout = new List<bool>();
        for (int i = 0; i < LocalLayers.Count; i++ ) {
            ShowFoldout.Add(false);
        }
    }

    public void Apply()
    {           
        foreach (var entry in LocalLayers) {
            if (!entry.Applied) {
                if (!entry.Apply()) {
                    return;
                }
            }
        }

        TileSet.Layers = new List<TileSetLayer>();
        foreach (var entry in LocalLayers) {
            var tmpEntry = entry.Clone();
            tmpEntry.Applied = true;
            TileSet.Layers.Add(tmpEntry);
        }

        // Make sure the new tileset is saved
        EditorUtility.SetDirty(target);
    }

    public void AddLayer()
    {
        LocalLayers.Add(new TileSetLayer());
        ShowFoldout.Add(true);

        this.Repaint();
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Add layer")){
            AddLayer();
        }

        int index = 0;
        foreach (var entry in LocalLayers) {
            ShowFoldout[index] = EditorGUILayout.Foldout(ShowFoldout[index], entry.Name);

            if (ShowFoldout[index]) {
                DrawLayerEditor(entry);
            }
            ++index;
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
        var tmpName = EditorGUILayout.TextField("Width", layer.Name);
        var tmpLayerType = (TileSetLayerType)EditorGUILayout.EnumPopup("Type", layer.LayerType);
        var tmpWidth = EditorGUILayout.IntField("Width", layer.TileSetWidth);
        var tmpHeight = EditorGUILayout.IntField("Heigh", layer.TileSetHeight);
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

        int bufferZone = 8;
        int windowSize = Screen.width - bufferZone;
        int tileWidth = windowSize / layer.TileSetWidth;

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
            }else if(tile.Pathing == TilePathing.BaseWalkable){
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