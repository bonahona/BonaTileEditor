using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(MapSegment))]
public class MapSegmentEditor : Editor
{
    public const string SEGMENT_WIDTH_TOOLTIP = "Width of this map segment countet in tiles";
    public const string SEGMENT_HEIGHT_TOOLTIP = "Height of this map segment countet in tiles";

    public const float BASE_LAYER_DEPTH = 0;
    public const float OVERLAY_BASE_DEPTH = 0;
    public const float OVERLAY_INCREMENT_DEPTH = 0.01f;
    public const float ONTOP_OVERLAY_BASE_DEPTH = 1;
    public const float ONTOP_OVERLAY_INCREMENT_DEPTH = 0.01f;

    public const int MAX_WIDTH = 100;
    public const int MAX_HEIGHT = 100;

    public MapSegment MapSegment { get; set; }

    #region Local copies of the MapSegment variables
    public int Width { get; set; }
    public int Height { get; set; }
    public TileSet TileSet { get; set; }

    public int CurrentLayerIndex { get; set; }
    public int CurrentHoverIndex { get; set; }

    public Vector2 GridTileSize { get; set; }
    #endregion

    #region Inspector state variables
    public Vector2 MapSegmentDrawOffset { get; set; }

    public bool MainFoldout { get; set; }
    public bool TileSetFoldout { get; set; }

    public bool AltPress { get; set; }
    public bool ControlPress { get; set; }
    public bool ShiftPress { get; set; }
    public List<bool> NumericButtonPress { get; set; }

    public bool IsMouseOver { get; set; }
    public RaycastHit RaycastHit { get; set; }
    public IntVector2 CurrentlyHoverPoint { get; set; }

    public IntVector2 SelectionBlockStart { get; set; }
    
    public bool MouseLeftClicked { get; set; }
    public bool MouseRightClicked { get; set; }
    public IntVector2 BlockStart { get; set; }

    public MapSegmentPreview TileSetPreview { get; set; }

    #endregion

    // Loaded resources
    public Texture2D[] ToolBarBrushTextures;
    public Texture2D SelectedTileTexture;
    public Texture2D HoverTileTexture;

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

        TileSet.AddMapSegment(MapSegment);
        ApplyTilsetChanges();

        // The colliders will be out of scale by this and must be adapted
        UpdateCollider();
        EditorUtility.SetDirty(target);
    }

    public void ApplyTilsetChanges()
    {
        // Find any layers that is in the map segment but not in the tileset
        foreach(var layer in MapSegment.GetComponentsInChildren<MapSegmentLayer>()) {
            if (!TileSet.HasLayer(layer.TileSetLayer)) {
                GameObject.DestroyImmediate(layer.gameObject);
                Debug.Log("Destroy layer");
            }
        }

        // ALter or create new layers for the ones found in the 
        for(int i = 0; i < TileSet.Layers.Count; i++) {
            var layer = TileSet.Layers[i];

            if (MapSegment.HasLayerOfType(layer)) {

                AlterLayer(layer, i);
            } else {
                CreateNewLayer(layer, i);
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
        if (TileSetPreview == null) {
            if (!MapSegment.HasPreviewObject()) {
                TileSetPreview = MapSegment.CreatePreviewObject();
            } else {
                TileSetPreview = MapSegment.GetComponentInChildren<MapSegmentPreview>();
             }
        }

        SetDefaults();
        UpdateMouseClick();

        if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space) {
            MapSegment.CurrentTileSelection.Clear();
        }

        if (MapSegment.CurrentTileSelection == null) {
            MapSegment.CurrentTileSelection = new MapSegmentPaletteSelection();
        }

        MainFoldout = EditorGUILayout.Foldout(MainFoldout, "Main data");
        if (MainFoldout) {
            DrawMainDataSegment();
        }

        TileSetFoldout = EditorGUILayout.Foldout(TileSetFoldout, "Tile set");
        if (TileSetFoldout) {
            DrawTileSetSegment();
        }

        if(GUILayout.Button("Update collider")){
            UpdateCollider();
        }

        Repaint();
    }

    protected void SetDefaults()
    {
        if(MapSegment.CurrentLayer == null) {
            var layers = MapSegment.GetComponentsInChildren<MapSegmentLayer>();

            // No layers exists, so nothing can be set as default (duh!)
            if(layers.Length == 0) {
                return;
            }

            MapSegment.CurrentLayer = layers[CurrentLayerIndex];
        }
    }

    protected Texture2D[] GetBrushTextures()
    {
        var result = new Texture2D[5];

        result[0] = Resources.Load("eyedropper") as Texture2D;
        result[1] = Resources.Load("pencil") as Texture2D;
        result[2] = Resources.Load("paint-roller") as Texture2D; 
        result[3] = Resources.Load("paint-can") as Texture2D;
        result[4] = Resources.Load("eyedropper") as Texture2D;

        return result;
    }

    protected Texture2D GetSelectedTileTexture()
    {
        return Resources.Load("SelectedTile") as Texture2D;
    }


    protected Texture2D GetHoverTexture()
    {
        return Resources.Load("HoverTile") as Texture2D;
    }

    protected void DrawMainDataSegment()
    {
        Width = EditorGUILayout.IntSlider(new GUIContent("Width", SEGMENT_WIDTH_TOOLTIP), Width, 0, MAX_WIDTH);
        Height = EditorGUILayout.IntSlider(new GUIContent("Height", SEGMENT_HEIGHT_TOOLTIP), Height, 0, MAX_HEIGHT);
        TileSet = (TileSet)EditorGUILayout.ObjectField(TileSet, typeof(TileSet), false);
        EditorGUILayout.Separator();

        GridTileSize = EditorGUILayout.Vector2Field("Grid Tile Size", GridTileSize);
        EditorGUILayout.Separator();

        if (GUILayout.Button("Apply")) {
            Apply();
        }

        if (GUILayout.Button("Reset")) {
            Reset();
        }
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

        var layerIndex = EditorGUILayout.Popup("Current layer", CurrentLayerIndex, layerNames);
        if (layerIndex != CurrentLayerIndex) {
            ChangeLayer(layerIndex, layers);
        }

        // Select the current brush
        int bufferZone = 8;
        int windowSize = Screen.width - bufferZone;

        if (MapSegment.TileSet != null) {
            if (MapSegment.CurrentLayer != null) {

                if (ToolBarBrushTextures == null || ToolBarBrushTextures.Length == 0) {
                    ToolBarBrushTextures = GetBrushTextures();
                }

                if (SelectedTileTexture == null) {
                    SelectedTileTexture = GetSelectedTileTexture();
                }

                if (HoverTileTexture == null) {
                    HoverTileTexture = GetHoverTexture();
                }

                // Select current brush
                var selectedBrush = (MapSegmentBrushType)GUILayout.Toolbar(((int)MapSegment.CurrentBrush), ToolBarBrushTextures);
                if(selectedBrush != MapSegment.CurrentBrush) {
                    ChangeBrush(selectedBrush);
                }

                var layer = MapSegment.CurrentLayer.TileSetLayer;
                int tileWidth = windowSize / layer.TileSetWidth;

                // Get the group rectangle to we can correct the mouse position by it as an offset
                var guiGroupRect = GUILayoutUtility.GetRect(windowSize, layer.TileSetHeight * tileWidth);

                // Adjust the mouse position by the GUI Group Rectangles start value to the correct tile can be selected
                GUI.BeginGroup(guiGroupRect);

                var eventType = Event.current.type;
                var mousePosition = new Vector2();

                if(eventType == EventType.Repaint || eventType == EventType.MouseDown || eventType == EventType.MouseUp || eventType == EventType.MouseDrag) {
                    mousePosition = Event.current.mousePosition;
                    MapSegmentDrawOffset = guiGroupRect.position - Vector2.one;
                } else {
                    mousePosition = Event.current.mousePosition - MapSegmentDrawOffset;
                }

                int height = layer.TileSetHeight * tileWidth;
                for (int y = 0; y < layer.TileSetHeight; y++) {
                    for (int x = 0; x < layer.TileSetWidth; x++) {
                        int num = (y * layer.TileSetWidth) + x;

                        var rect = new Rect(x * tileWidth, height - ((y + 1) * tileWidth), tileWidth, tileWidth);
                        GUI.DrawTextureWithTexCoords(rect, layer.Texture, layer.Tiles[num].Rect);

                        Texture2D currentTexture = null;
                        if (RectContains(rect, mousePosition)) {
                            currentTexture = HoverTileTexture;
                            SetTileSelection(x, y, num);
                        } else if (MapSegment.CurrentTileSelection.Contains(num)) {
                            currentTexture = SelectedTileTexture;
                        }

                        if (currentTexture != null) {
                            GUI.DrawTexture(rect, currentTexture);
                        }
                    }
                }

                // When all is drawn, if the left mouse is no longer pressed, the selection phase is over
                if (!MouseLeftClicked) {
                    SelectionBlockStart = null;
                }

                GUI.EndGroup();
            } else {
                EditorGUILayout.LabelField("Select a layer first");
            }
        }
    }

    public bool RectContains(Rect rect, Vector2 point)
    {
        if (rect.Contains(point)) {
            return true;
        }

        return false;
    }

    public void SetTileSelection(int x, int y, int tileType)
    {
        if (!BrushAllowsSelection(MapSegment.CurrentBrush)) {
            return;
        }

        if (MouseLeftClicked && SelectionBlockStart == null) {
            MapSegment.CurrentTileSelection.SetSingleSelection(tileType);
            SelectionBlockStart = new IntVector2(x, y);
        }else if(MouseLeftClicked && SelectionBlockStart != null && BrushAllowsBlockSelection(MapSegment.CurrentBrush)) {
            IntVector2 selectionEnd = new IntVector2(x, y);
            MapSegment.CurrentTileSelection.SetSelection(SelectionBlockStart, selectionEnd, MapSegment.CurrentLayer);
        }else {
            SelectionBlockStart = null;
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
            layer.TileSetLayer = tileSetLayer;
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
        // Make sure to call a repaint of the MapSegments when Undo or Redo is called
        var currentEvent = Event.current;
        if(currentEvent.type == EventType.ValidateCommand) {
            if (currentEvent.commandName == "UndoRedoPerform") {
                // TODO: Add logics for Undo/Redo
            }
        }

        MapSegment mapSegment = (MapSegment)target;
        var controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        HandleShortCuts();
        UpdateKeyInput();

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space) {
            mapSegment.CurrentTileSelection.Clear();
        }

        if (mapSegment.CurrentBrush == MapSegmentBrushType.Single) {
            HandleSingleBrush(mapSegment);
        } else if (mapSegment.CurrentBrush == MapSegmentBrushType.Block) {
            HandleBlockBrush(mapSegment);
        } else if (mapSegment.CurrentBrush == MapSegmentBrushType.Fill) {
            HandleFillBrush(mapSegment);
        }
    }

    protected void UpdateMouseClick()
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
            MouseLeftClicked = true;
        } else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {
            MouseLeftClicked = false;
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1) {
            MouseRightClicked = true;
        } else if (Event.current.type == EventType.MouseUp && Event.current.button == 1) {
            MouseRightClicked = false;
        }
    }

    protected void UpdateMouse(MapSegment mapSegment)
    {
        RaycastHit raycastHit;
        IsMouseOver = Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit);
        RaycastHit = raycastHit;

        if (IsMouseOver) {
            CurrentlyHoverPoint = mapSegment.GetTilePosition(raycastHit.point);
        } else {
            CurrentlyHoverPoint = null;
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

    protected void ChangeBrush(MapSegmentBrushType brushType)
    {
        if(!BrushAllowsSelection(brushType)) {
            MapSegment.CurrentTileSelection.Clear();
        }

        if(!BrushAllowsBlockSelection(brushType)) {
            MapSegment.CurrentTileSelection.SetToSingleSelection();
        }

        MapSegment.CurrentBrush = brushType;
    }

    protected void ChangeLayer(int layerIndex, MapSegmentLayer[] layers)
    {
        // To avoid immediately selecting the first thing the mouse hovers over on the new layer
        MouseLeftClicked = false;
        MouseRightClicked = false;

        MapSegment.CurrentTileSelection.Clear();
        SelectionBlockStart = null;

        MapSegment.CurrentLayer = layers[layerIndex];
        CurrentLayerIndex = layerIndex;
    }

    protected void HandleSingleBrush(MapSegment mapSegment)
    {
        // Find the tile to paint
        RaycastHit raycastHit;
        bool isMouseOver = Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit);

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
            MouseLeftClicked = true;
        } else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {
            MouseLeftClicked = false;
        }

        if (!AltPress && mapSegment.CurrentBrush >= 0 && isMouseOver) {
            var tilePosition = mapSegment.GetTilePosition(raycastHit.point);
            var currentPoint = new Point(tilePosition.X, tilePosition.Y);
            if (MouseLeftClicked) {
                // Paint the tile
                mapSegment.Paint(new Point(tilePosition.X, tilePosition.Y), mapSegment.CurrentTileSelection);

                // Set dirty so the editor serializes it
                EditorUtility.SetDirty(mapSegment.CurrentLayer);
            }else {
                // Show a preview of the tile that would be painted
                TileSetPreview.SetPreviewZone(mapSegment.CurrentTileSelection, currentPoint);            
            }
        }
    }

    protected void HandleBlockBrush(MapSegment mapSegment)
    {
        if (!AltPress) {
            // Find the tile to paint
            RaycastHit raycastHit;
            Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit);

            // Find the cordinates of the selected tile
            var tilePosition = mapSegment.GetTilePosition(raycastHit.point);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
                BlockStart = new IntVector2(tilePosition);
            } else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {

                if (BlockStart != null) {
                    IntVector2 endBlock = new IntVector2(tilePosition);

                    for (int y = Mathf.Min(BlockStart.Y, endBlock.Y); y <= Mathf.Max(BlockStart.Y, endBlock.Y); y++) {
                        for (int x = Mathf.Min(BlockStart.X, endBlock.X); x <= Mathf.Max(BlockStart.X, endBlock.X); x++) {
                            mapSegment.Paint(new Point(x, y), MapSegment.CurrentTileSelection);
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
        // Find the tile to paint
        RaycastHit raycastHit;
        bool isMouseOver = Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit);

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
            MouseLeftClicked = true;
        } else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {
            MouseLeftClicked = false;
        }

        if (MouseLeftClicked && !AltPress && mapSegment.CurrentBrush >= 0 && isMouseOver) {

            var tilePosition = mapSegment.GetTilePosition(raycastHit.point);
            var tileType = mapSegment.CurrentLayer.TilesCollection.GetTileType(tilePosition);
            var adjecentTiles = mapSegment.FindAllAdjecentTilesOfType(tilePosition.ToPoint(), tileType);

            foreach (var adjecentTile in adjecentTiles) {
                mapSegment.Paint(adjecentTile, mapSegment.CurrentTileSelection);
            }

            // Set dirty so the editor serializes it
            EditorUtility.SetDirty(mapSegment.CurrentLayer);
        }
    }    
   
    protected bool BrushAllowsSelection(MapSegmentBrushType type)
    {
        if(type ==  MapSegmentBrushType.Block || type == MapSegmentBrushType.Fill || type == MapSegmentBrushType.Single) {
            return true;
        }

        return false;
    }

    protected bool BrushAllowsBlockSelection(MapSegmentBrushType type)
    {
        if(type == MapSegmentBrushType.Single) {
            return true;
        }

        return false;
    }

    protected void UpdateCollider()
    {
        try {
            var resultPathing = MapSegment.GetMapSegmentPathing();
            resultPathing.UpdateInternalBoundries();

            var edgeColliders = MapSegment.GetComponentsInChildren<EdgeCollider2D>();
            foreach(var edgeCollider in edgeColliders) {
                GameObject.DestroyImmediate(edgeCollider.gameObject);
            }

            var edgesGroups = resultPathing.GetColliderPoints();

            foreach(var edgeGroup in edgesGroups) {
                var edgeGameObject = new GameObject("Collider2D");
                edgeGameObject.hideFlags = HideFlags.HideInHierarchy;

                var edgeCollider = edgeGameObject.AddComponent<EdgeCollider2D>();
                edgeCollider.points = edgeGroup.ToArray();

                edgeGameObject.transform.parent = MapSegment.transform;
                edgeGameObject.transform.localPosition = Vector3.zero;
                edgeGameObject.transform.localRotation = Quaternion.identity;
                edgeGameObject.transform.localScale = Vector3.one;
            }

            Debug.Log(string.Format("MapSegment {0}'s colliders has been updated", MapSegment.name));
        }catch(System.Exception) {
            Debug.LogError("Could not generate a collider. Mapsegment might not be up-to-date with some changes to the tileset. Apply the tileset again.");
        }
    }

    public Vector2[] ScaleColliderPoints(Vector2[] points, Vector2 scale)
    {
        var result = new Vector2[points.Length];

        for (int i = 0; i < points.Length; i++) {
            result[i] = GetScaledVector(points[i], scale);
        }

        return result;
    }

    public Vector2 GetScaledVector(Vector2 vector, Vector2 scale)
    {
        return new Vector2(vector.x * scale.x, vector.y * scale.y);
    }
}