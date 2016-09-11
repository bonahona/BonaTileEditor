using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(MapSegment))]
public class MapSegmentEditor : Editor
{
    public const string SEGMENT_WIDTH_TOOLTIP = "Width of this map segment countet in tiles";
    public const string SEGMENT_HEIGHT_TOOLTIP = "Height of this map segment countet in tiles";

    public static readonly string[] BRUSH_TYPES_VALUES = new string[] { "Point", "Brush", "Block", "Bucket" };

    public const float BASE_LAYER_DEPTH = 0;
    public const float OVERLAY_BASE_DEPTH = 0;
    public const float OVERLAY_INCREMENT_DEPTH = 0.01f;
    public const float ONTOP_OVERLAY_BASE_DEPTH = 1;
    public const float ONTOP_OVERLAY_INCREMENT_DEPTH = 0.01f;

    public const int MAX_WIDTH = 100;
    public const int MAX_HEIGHT = 100;

    public MapSegment MapSegment { get; set; }

    public bool MainFoldout { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TileSet TileSet { get; set; }

    public bool TileSetFoldout { get; set; }
    public int CurrentLayerIndex { get; set; }

    public bool OptionsFoldout { get; set; }
    public Vector2 GridTileSize { get; set; }

    // State variabes
    public bool AltPress { get; set; }
    public bool ControlPress { get; set; }
    public bool ShiftPress { get; set; }
    public List<bool> NumericButtonPress { get; set; }

    // Loaded resources
    public Texture2D[] ToolBarBrushTextures;
    public Texture2D TileTexture;
    public Texture2D SelectedTileTexture;

    public bool MouseClicked { get; set; }
    public IntVector2 BlockStart { get; set; }

    private GUIStyle m_internalTileStyle;
    void OnEnabled()
    {
        MapSegment = (MapSegment)target;
        MainFoldout = true;
        Width = MapSegment.Width;
        Height = MapSegment.Height;
        TileSet = MapSegment.TileSet;
        GridTileSize = MapSegment.GridTileSize;

        TileSetFoldout = true;
        OptionsFoldout = false;

        var layers = MapSegment.GetComponentsInChildren<MapSegmentLayer>();
        CurrentLayerIndex = 0;
        for (int i = 0; i < layers.Length; i++) {
            if (MapSegment.CurrentLayer == layers[i]) {
                CurrentLayerIndex = i;
            }
        }

        // Create the invisbile style used for the tiles
        m_internalTileStyle = new GUIStyle();
        m_internalTileStyle.alignment = TextAnchor.MiddleCenter;
        m_internalTileStyle.fontSize = 18;
        m_internalTileStyle.fontStyle = FontStyle.Bold;
    }

    public void Apply()
    {
        if (Width == 0 || Height == 0) {
            Debug.LogError("Cannot create a map segment with the width or height of 0");
            return;
        }

        if (TileSet == null) {
            Debug.LogError("Cannot create a map segment without a tile set");
            return;
        }

        Undo.RegisterCompleteObjectUndo(target, "Changed size and tileset");

        MapSegment.Width = Width;
        MapSegment.Height = Height;
        MapSegment.TileSet = TileSet;

        MapSegment.GridTileSize = GridTileSize;
        ApplyTilsetChanges();
        EditorUtility.SetDirty(target);
    }

    public void ApplyTilsetChanges()
    {
        for(int i = 0; i < TileSet.Layers.Count; i++) {
            var layer = TileSet.Layers[i];

            if (MapSegment.HasLayerOfType(layer)) {
                AlterLayer(layer, i);
            } else {
                CreateNewLayer(layer, i);
            }
        }


        // Also make sure there are no remnats of layers no longer present in the tileset
        var tileSetGuid = GetGuidsForLayers();

        foreach (var tileSetLayer in MapSegment.GetComponentsInChildren<MapSegmentLayer>()) {
            if (!tileSetGuid.Contains(tileSetLayer.TileSetLayer.Guid)) {
                GameObject.DestroyImmediate(tileSetLayer.gameObject);
            }
        }

        // Now sort the layers according to type
        int index = 1;
        foreach (var tileSetLayer in MapSegment.GetComponentsInChildren<MapSegmentLayer>()) {
            Vector3 tmpPosition = tileSetLayer.gameObject.transform.position;
            if (tileSetLayer.TileSetLayer.LayerType == TileSetLayerType.BaseLayer) {
                tmpPosition.z = 0;
            } else if (tileSetLayer.TileSetLayer.LayerType == TileSetLayerType.Overlay) {
                tmpPosition.z = -index;
            } else if (tileSetLayer.TileSetLayer.LayerType == TileSetLayerType.OnTopOverlay) {
                tmpPosition.z = -100 + index;
            }

            tileSetLayer.gameObject.transform.position = tmpPosition;
        }
    }

    public List<string> GetGuidsForLayers()
    {
        var layers = MapSegment.GetComponentsInChildren<MapSegmentLayer>();
        List<string> result = new List<string>();

        for (int i = 0; i < layers.Length; i++) {
            result.Add(layers[i].TileSetLayer.Guid);
        }

        return result;
    }

    public void Reset()
    {
        OnEnabled();
    }

    public override void OnInspectorGUI()
    {
        MainFoldout = EditorGUILayout.Foldout(MainFoldout, "Main data");
        if (MainFoldout) {
            DrawMainDataSegment();
        }

        TileSetFoldout = EditorGUILayout.Foldout(TileSetFoldout, "Tile set");
        if (TileSetFoldout) {
            DrawTileSetSegment();
        }

        OptionsFoldout = EditorGUILayout.Foldout(OptionsFoldout, "Options");
        if (OptionsFoldout) {
            GridTileSize = EditorGUILayout.Vector2Field("Grid Tile Size", GridTileSize);
        }

        if (GUILayout.Button("Apply")) {
            Apply();
        }

        if (GUILayout.Button("Reset")) {
            Reset();
        }
    }

    protected Texture2D[] GetBrushTextures()
    {
        var result = new Texture2D[4];

        result[0] = Resources.Load("eyedropper") as Texture2D;
        result[1] = Resources.Load("pencil") as Texture2D;
        result[2] = Resources.Load("paint-roller") as Texture2D; 
        result[3] = Resources.Load("paint-can") as Texture2D;

        return result;
    }

    protected Texture2D GetTileTexture()
    {
        return Resources.Load("TileUnSelected") as Texture2D;
    }

    protected Texture2D GetSelectedTileTexture()
    {
        return Resources.Load("TileSelected") as Texture2D;
    }

    protected void DrawMainDataSegment()
    {
        Width = EditorGUILayout.IntSlider(new GUIContent("Width", SEGMENT_WIDTH_TOOLTIP), Width, 0, MAX_WIDTH);
        Height = EditorGUILayout.IntSlider(new GUIContent("Height", SEGMENT_HEIGHT_TOOLTIP), Height, 0, MAX_HEIGHT);
        TileSet = (TileSet)EditorGUILayout.ObjectField(TileSet, typeof(TileSet), false);
    }

    protected void DrawTileSetSegment()
    {
        // Select the current layer to draw to
        var layers = MapSegment.GetComponentsInChildren<MapSegmentLayer>();
        if(layers.Length == 0) {
            MapSegment.CurrentLayer = null;
            EditorGUILayout.LabelField("Warning! Tileset does not have any layers loaded.");
            EditorGUILayout.LabelField("Resolve this by selecting a tileset and applying the changes.");
            return;
        }

        string[] layerNames = new string[layers.Length];

        int index = 0;
        foreach (var tmpLayer in layers) {
            layerNames[index] = tmpLayer.TileSetLayer.Name;
            ++index;
        }

        CurrentLayerIndex = EditorGUILayout.Popup("Current layer", CurrentLayerIndex, layerNames);
        MapSegment.CurrentLayer = layers[CurrentLayerIndex];

        // Select the current brush
        int bufferZone = 8;
        int windowSize = Screen.width - bufferZone;

        if (MapSegment.TileSet != null) {
            if (MapSegment.CurrentLayer != null) {

                if (ToolBarBrushTextures == null) {
                    ToolBarBrushTextures = GetBrushTextures();
                }

                if(TileTexture == null) {
                    TileTexture = GetTileTexture();
                }

                if (SelectedTileTexture == null) {
                    SelectedTileTexture = GetSelectedTileTexture();
                }

                // Select current brush
                MapSegment.CurrentBrush = (MapSegmentBrushType)GUILayout.Toolbar(((int)MapSegment.CurrentBrush), ToolBarBrushTextures);

                var layer = MapSegment.CurrentLayer.TileSetLayer;
                int tileWidth = windowSize / layer.TileSetWidth;

                // Select the current tile to draw, from the selected tile layer
                GUI.BeginGroup(GUILayoutUtility.GetRect(windowSize, layer.TileSetHeight * tileWidth));
                int height = layer.TileSetHeight * tileWidth;
                for (int y = 0; y < layer.TileSetHeight; y++) {
                    for (int x = 0; x < layer.TileSetWidth; x++) {
                        int num = (y * layer.TileSetWidth) + x;

                        Rect rect = new Rect(x * tileWidth, height - ((y + 1) * tileWidth), tileWidth, tileWidth);
                        GUI.DrawTextureWithTexCoords(rect, layer.Texture, layer.Tiles[num].Rect);

                        var currentTileTexture = TileTexture;
                        if(num == MapSegment.CurrentTileId) {
                            currentTileTexture = SelectedTileTexture;
                        }

                        if (GUI.Button(rect, currentTileTexture, m_internalTileStyle)) {
                            MapSegment.CurrentTile = layer.Tiles[num];
                            MapSegment.CurrentTileId = num;
                        }
                    }
                }

                GUI.EndGroup();
            } else {
                EditorGUILayout.LabelField("Select a layer first");
            }
        }
    }

    public void CreateNewLayer(TileSetLayer tileSetLayer, int layerIndex)
    {
        GameObject segmentGameObject = new GameObject("MapSegmentLayer");
        var layer = segmentGameObject.AddComponent<MapSegmentLayer>();
        layer.MapSegment = MapSegment;
        layer.TileSetLayer = tileSetLayer;

        segmentGameObject.gameObject.transform.parent = MapSegment.transform;
        segmentGameObject.transform.localPosition = new Vector3();

        ApplyChanges(layer, segmentGameObject, layerIndex);
        EditorUtility.SetDirty(segmentGameObject);
    }


    public GameObject GetObjectFromLayer(TileSetLayer tileSetLayer)
    {
        foreach (var tmpLayer in MapSegment.GetComponentsInChildren<MapSegmentLayer>()) {
            if (tmpLayer.TileSetLayer.Guid == tileSetLayer.Guid) {
                return tmpLayer.gameObject;
            }
        }

        // This should really never happen
        Debug.LogError("Could not find the correct GameObject to alter");
        return null;
    }

    public void AlterLayer(TileSetLayer tileSetLayer, int layerIndex)
    {
        var segmentGameObject = GetObjectFromLayer(tileSetLayer);

        if (segmentGameObject != null) {
            var layer = segmentGameObject.GetComponent<MapSegmentLayer>();
            ApplyChanges(layer, segmentGameObject, layerIndex);
        }
    }

    public int TranslateTilePosition(TileTypeCollection tiles, int x, int y)
    {
        if (tiles == null) {
            return 0;
        }

        // Get the limits of the old tile array
        int maxWidth = tiles.Width;
        int maxHeight = tiles.Height;

        if (y >= maxHeight || x >= maxWidth) {
            return 0;
        }

        return tiles.GetTileType(x, y);
    }

    public void ApplyChanges(MapSegmentLayer layer, GameObject gameObject, int layerIndex)
    {
        TileTypeCollection oldTiles = null;

        if (layer.TilesCollection != null) {
            oldTiles = layer.TilesCollection.Clone();
        }
        layer.TilesCollection = new TileTypeCollection(MapSegment.Width, MapSegment.Height);

        var tileSetLayer = layer.TileSetLayer;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();
        List<Vector3> normals = new List<Vector3>();
      
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        var mesh = new Mesh();

        var index = 0;
        for (int y = 0; y < MapSegment.Height; y++) {
            for (int x = 0; x < MapSegment.Width; x++) {

                int tmpTileType = 0;
                if (oldTiles != null) {
                    tmpTileType = TranslateTilePosition(oldTiles, x, y);
                }

                layer.TilesCollection.SetTileType(x, y, tmpTileType);
                AddVertices(x, y, vertices, GridTileSize);
                AddUvs(layer.TileSetLayer, tmpTileType, uvs);
                index = AddTris(index, tris);
                AddNormals(normals);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.normals = normals.ToArray();

        if (meshFilter.GetComponent<Renderer>().sharedMaterial != null) {
            meshFilter.GetComponent<Renderer>().sharedMaterial.mainTexture = tileSetLayer.Texture;
        } else {
            meshFilter.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            meshFilter.GetComponent<Renderer>().sharedMaterial.mainTexture = tileSetLayer.Texture;
        }

        var totalWidth = MapSegment.Width * GridTileSize.x;
        var totalHeight = MapSegment.Height * GridTileSize.y;
        var boxCollider = gameObject.GetComponent<BoxCollider>();
        UpdateBoxColliderSize(boxCollider, totalWidth, totalHeight);

        Undo.RegisterCompleteObjectUndo(meshFilter, "Updated map segment mesh");
        meshFilter.mesh = null;
        Undo.RegisterCompleteObjectUndo(meshFilter, "Updated map segment mesh");
        meshFilter.mesh = mesh;

        // Update the game object
        gameObject.hideFlags = HideFlags.HideInHierarchy;

        var layerType = layer.TileSetLayer.LayerType;
        if(layerType == TileSetLayerType.BaseLayer) {
            gameObject.transform.position = GetBaseLayerPosition(gameObject.transform.position);
        }else if(layerType == TileSetLayerType.Overlay) {
            gameObject.transform.position = GetOverlayPosition(gameObject.transform.position, layerIndex);
        }else if(layerType == TileSetLayerType.OnTopOverlay) {
            gameObject.transform.position = GetOnTopOverlayPosition(gameObject.transform.position, layerIndex);
        }

        EditorUtility.SetDirty(layer);       
    }

    public Vector3 GetBaseLayerPosition(Vector3 currentPosition)
    {
        var result = currentPosition;
        result.z = BASE_LAYER_DEPTH;
        return result;
    }

    public Vector3 GetOverlayPosition(Vector3 currentPostition, int layerIndex)
    {
        var result = currentPostition;
        result.z = OVERLAY_BASE_DEPTH + (layerIndex * OVERLAY_INCREMENT_DEPTH);
        return result;
    }

    public Vector3 GetOnTopOverlayPosition(Vector3 currentPostition, int layerIndex)
    {
        var result = currentPostition;
        result.z = ONTOP_OVERLAY_BASE_DEPTH + (layerIndex * ONTOP_OVERLAY_INCREMENT_DEPTH);
        return result;
    }
    protected void AddVertices(int x, int y, List<Vector3> vertices, Vector2 tileSize)
    {
        float sizeX = tileSize.x;
        float sizeY = tileSize.y;

        vertices.Add(new Vector3(x * sizeX, y * sizeY, 0));
        vertices.Add(new Vector3((x + 1) * sizeX, y * sizeY, 0));
        vertices.Add(new Vector3(x * sizeX, (y + 1) * sizeY, 0));
        vertices.Add(new Vector3((x + 1) * sizeX, (y + 1) * sizeY, 0));
    }

    protected void AddUvs(TileSetLayer tileSetLayer, int tile, List<Vector2> uvs)
    {
        foreach (var uvCords in tileSetLayer.Tiles[tile].UvCords) {
            uvs.Add(uvCords);
        }
    }

    protected int AddTris(int startIndex, List<int> tris)
    {
        tris.Add(startIndex + 1);
        tris.Add(startIndex + 2);
        tris.Add(startIndex + 3);

        tris.Add(startIndex + 2);
        tris.Add(startIndex + 1);
        tris.Add(startIndex);
        return startIndex + 4;
    }

    protected void AddNormals(List<Vector3> normals)
    {
        for (int i = 0; i < 4; i++) {
            normals.Add(Vector3.forward);
        }
    }

    protected void UpdateBoxColliderSize(BoxCollider boxCollider, float totalWidth, float totalHeight)
    {
        if(boxCollider == null) {
            Debug.LogError("Map segment failed to or is missing a BoxCollider");
            return;
        }

        var totalSize = new Vector2(totalWidth, totalHeight);
        boxCollider.size = totalSize;
        boxCollider.center = (totalSize / 2);
        boxCollider.isTrigger = true;
    }

    void OnSceneGUI()
    {
        MapSegment mapSegment = (MapSegment)target;
        var controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        UpdateKeyInput();
        HandleShortCuts();

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space) {
            mapSegment.CurrentBrush = MapSegmentBrushType.None;
            Repaint();
        }

        if (mapSegment.CurrentBrush == MapSegmentBrushType.Single) {
            HandleSingleBrush(mapSegment);
        } else if (mapSegment.CurrentBrush == MapSegmentBrushType.Block) {
            HandleBlockBrush(mapSegment);
        } else if (mapSegment.CurrentBrush == MapSegmentBrushType.Fill) {
            HandleFillBrush(mapSegment);
        }
    }

    protected void UpdateKeyInput()
    {
        if (NumericButtonPress == null) {
            NumericButtonPress = new List<bool>();

            for(int i = 0; i < 9; i++) {
                NumericButtonPress.Add(false);
            }
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftAlt) {
            AltPress = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftAlt) {
            AltPress = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftControl) {
            ControlPress = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftControl) {
            ControlPress = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftShift) {
            ShiftPress = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftShift) {
            ShiftPress = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha1) {
            NumericButtonPress[0] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha1) {
            NumericButtonPress[0] = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha2) {
            NumericButtonPress[1] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha2) {
            NumericButtonPress[1] = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha3) {
            NumericButtonPress[2] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha3) {
            NumericButtonPress[2] = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha4) {
            NumericButtonPress[3] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha4) {
            NumericButtonPress[3] = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha5) {
            NumericButtonPress[4] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha5) {
            NumericButtonPress[4] = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha6) {
            NumericButtonPress[5] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha6) {
            NumericButtonPress[5] = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha7) {
            NumericButtonPress[6] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha7) {
            NumericButtonPress[6] = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha8) {
            NumericButtonPress[7] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha8) {
            NumericButtonPress[7] = false;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha9) {
            NumericButtonPress[8] = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Alpha9) {
            NumericButtonPress[8] = false;
        }
    }

    protected void HandleShortCuts()
    {
        if (ShiftPress) {
            // Updates the current layer. Eg. Ctrl + 1 means layer 0 and Ctrl + 2 means layer 0
            for(int i = 0; i < NumericButtonPress.Count; i++) {
                if (NumericButtonPress[i]) {
                    if (LayerIndexExists(i)) {
                        CurrentLayerIndex = i;
                    }
                }
            }
        }
    }

    protected bool LayerIndexExists(int layerIndex)
    {
        var layers = MapSegment.GetComponentsInChildren<MapSegmentLayer>();

        if (layers.Length >= layerIndex) {
            return false;
        }

        return true;
    }

    protected void HandleSingleBrush(MapSegment mapSegment)
    {
        // Find the tile to paint
        RaycastHit raycastHit;
        bool isMouseOver = Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit);

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
            MouseClicked = true;
        } else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {
            MouseClicked = false;
        }

        if (MouseClicked && !AltPress && mapSegment.CurrentBrush >= 0 && isMouseOver) {

            var tilePosition = GetTilePosition(mapSegment, raycastHit.point);
            Paint(tilePosition.X, tilePosition.Y , mapSegment.CurrentTile, mapSegment.CurrentTileId, mapSegment);

            // Set dirty so the editor serializes it
            EditorUtility.SetDirty(mapSegment.CurrentLayer);
        }
    }

    protected void HandleBlockBrush(MapSegment mapSegment)
    {
        if (!AltPress) {
            // Find the tile to paint
            RaycastHit raycastHit;
            Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit);

            // Find the cordinates of the selected tile
            var tilePosition = GetTilePosition(mapSegment, raycastHit.point);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
                BlockStart = new IntVector2(tilePosition);
            } else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {

                if (BlockStart != null) {
                    IntVector2 endBlock = new IntVector2(tilePosition);

                    for (int by = Mathf.Min(BlockStart.Y, endBlock.Y); by <= Mathf.Max(BlockStart.Y, endBlock.Y); by++) {
                        for (int bx = Mathf.Min(BlockStart.X, endBlock.X); bx <= Mathf.Max(BlockStart.X, endBlock.X); bx++) {
                            Paint(bx, by, MapSegment.CurrentTile, MapSegment.CurrentTileId, mapSegment);
                        }
                    }

                    // Set dirty so the editor serializes it
                    EditorUtility.SetDirty(mapSegment.CurrentLayer);
                    BlockStart = null;
                }
            }
        }
    }

    protected void HandleFillBrush(MapSegment mapSegment)
    {
        if (AltPress) {
            return;
        }

        // Find the tile to paint
        RaycastHit raycastHit;
        bool isMouseOver = Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit);

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
            MouseClicked = true;
        } else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {
            MouseClicked = false;
        }

        if (MouseClicked && !AltPress && mapSegment.CurrentBrush >= 0 && isMouseOver) {

            // Find the cordinates of the selected tile
            var tilePosition = GetTilePosition(mapSegment, raycastHit.point);

            int tileTypeId = mapSegment.CurrentLayer.TilesCollection.GetTileType(tilePosition);

            foreach (var tilePoint in FindAllAdjecentTilesOfType(new Point(tilePosition), tileTypeId, MapSegment)) {
                Paint(tilePoint.X, tilePoint.X, mapSegment.CurrentTile, mapSegment.CurrentTileId, mapSegment);
            }
        }
    }

    protected List<Point> FindAllAdjecentTilesOfType(Point startPoint, int tileTypeId, MapSegment mapSegment)
    {
        HashSet<Point> result = new HashSet<Point>();
        SearchDepthFirst(startPoint, result, mapSegment.Width, mapSegment);

        return new List<Point>(result);
    }

    protected void SearchDepthFirst(Point currentPoint, HashSet<Point> result, int tileTypeId, MapSegment mapSegment)
    {
        // If the result set already contains this, the tile has already been processedes
        if(result.Contains(currentPoint)){
            return;
        }

        // We are looking outside the map segments max and min values regarding width and height
        if(!IsWithinBounds(currentPoint, mapSegment)) {
            return;
        }

        // This is not the tiletype where looking for
        var currentTileTypeId = MapSegment.CurrentLayer.TilesCollection.GetTileType(currentPoint);
        if (currentTileTypeId != tileTypeId) {
            return;
        }

        result.Add(currentPoint);

        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                if (x != 0 || y != 0) {

                    Point tmpPoint = new Point(currentPoint.X + x, currentPoint.Y + y);
                    SearchDepthFirst(tmpPoint, result, tileTypeId, mapSegment);
                }
            }
        }
    }

    protected bool IsWithinBounds(Point point, MapSegment mapSegment)
    {
        if(point.X < 0 || point.Y < 0) {
            return false;
        }

        if(point.X >= mapSegment.Width || point.Y >= mapSegment.Height) {
            return false;
        }

        return true;
    }

    protected IntVector2 GetTilePosition(MapSegment mapSegment, Vector3 hitCordinate)
    {
        var result = new IntVector2();

        var localHit = mapSegment.transform.InverseTransformPoint(hitCordinate);
        result.X = Mathf.FloorToInt(localHit.x / mapSegment.GridTileSize.x);
        result.Y = Mathf.FloorToInt(localHit.y / mapSegment.GridTileSize.y);

        return result;
    }

    public void Paint(int x, int y, Tile tile, int tileId, MapSegment target)
    {
        if(x < 0 || y < 0) {
            Debug.LogWarning(string.Format("Could not paint tile {0}:{1}", x, y));
            return;
        }

        var targetLayer = MapSegment.CurrentLayer;
        if (targetLayer.MeshFilter == null) {
            targetLayer.MeshFilter = targetLayer.GetComponent<MeshFilter>();
            if (targetLayer.MeshFilter == null) {
                return;
            }
        }

        Vector2[] uvs = targetLayer.MeshFilter.sharedMesh.uv;

        int uvOffset = ((y * Width) + x) * 4;

        for (int i = 0; i < 4; i++) {
            uvs[uvOffset + i] = tile.UvCords[i];
        }

        Undo.RecordObjects(new Object[] { targetLayer, targetLayer.GetComponent<MeshFilter>().sharedMesh }, "Painted tile");

        target.CurrentLayer.TilesCollection.SetTileType(x, y, tileId);
        targetLayer.MeshFilter.sharedMesh.uv = uvs;
        SceneView.RepaintAll();
    }
}