using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(MapSegment))]
public class MapSegmentEditor : Editor
{

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
                CurrentLayerIndex = i + 1;
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
            Debug.LogError("Cannot create a map segment withput a tile set");
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
        foreach (var layer in TileSet.Layers) {
            if (MapSegment.HasLayerOfType(layer)) {
                AlterLayer(layer);
            } else {
                CreateNewLayer(layer);
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
            Width = EditorGUILayout.IntSlider(Width, 0, MAX_WIDTH);
            Height = EditorGUILayout.IntSlider(Height, 0, MAX_HEIGHT);
            TileSet = (TileSet)EditorGUILayout.ObjectField(TileSet, typeof(TileSet), false);
        }

        TileSetFoldout = EditorGUILayout.Foldout(TileSetFoldout, "Tile set");
        if (TileSetFoldout) {

            // Select the current layer to draw to
            var layers = MapSegment.GetComponentsInChildren<MapSegmentLayer>();
            string[] layerNames = new string[layers.Length + 1];
            layerNames[0] = " - Select layer - ";

            int index = 1;
            foreach (var tmpLayer in layers) {
                layerNames[index] = tmpLayer.TileSetLayer.Name;
                ++index;
            }

            CurrentLayerIndex = EditorGUILayout.Popup(CurrentLayerIndex, layerNames);
            if (CurrentLayerIndex > 0) {
                MapSegment.CurrentLayer = layers[CurrentLayerIndex - 1];
            } else {
                MapSegment.CurrentLayer = null;
            }

            // Select the current brush
            int bufferZone = 8;
            int windowSize = Screen.width - bufferZone;

            if (MapSegment.TileSet != null) {
                if (MapSegment.CurrentLayer != null) {

                    GUI.BeginGroup(GUILayoutUtility.GetRect(windowSize, 52));
                    GUI.BeginGroup(new Rect((windowSize / 2) - 50, 0, 100, 100));
                    EditorGUI.LabelField(new Rect(0, 0, 128, 16), "Brush type");

                    if (EditorGUI.Toggle(new Rect(0, 16, 24, 24), (MapSegment.CurrentBrush == MapSegmentBrushType.None), "Button")) {
                        MapSegment.CurrentBrush = MapSegmentBrushType.None;
                    }
                    if (EditorGUI.Toggle(new Rect(24, 16, 24, 24), (MapSegment.CurrentBrush == MapSegmentBrushType.Single), "Button")) {
                        MapSegment.CurrentBrush = MapSegmentBrushType.Single;
                    }
                    if (EditorGUI.Toggle(new Rect(48, 16, 24, 24), (MapSegment.CurrentBrush == MapSegmentBrushType.Block), "Button")) {
                        MapSegment.CurrentBrush = MapSegmentBrushType.Block;
                    }
                    if (EditorGUI.Toggle(new Rect(72, 16, 24, 24), (MapSegment.CurrentBrush == MapSegmentBrushType.Fill), "Button")) {
                        MapSegment.CurrentBrush = MapSegmentBrushType.Fill;
                    }

                    GUI.EndGroup();
                    GUI.EndGroup();


                    var layer = MapSegment.CurrentLayer.TileSetLayer;
                    int tileWidth = windowSize / layer.TileSetWidth;

                    GUI.BeginGroup(GUILayoutUtility.GetRect(windowSize, layer.TileSetHeight * tileWidth));
                    int height = layer.TileSetHeight * tileWidth;
                    for (int y = 0; y < layer.TileSetHeight; y++) {
                        for (int x = 0; x < layer.TileSetWidth; x++) {
                            int num = (y * layer.TileSetWidth) + x;

                            Rect rect = new Rect(x * tileWidth, height - ((y + 1) * tileWidth), tileWidth, tileWidth);
                            GUI.DrawTextureWithTexCoords(rect, layer.Texture, layer.Tiles[num].Rect);
                            if(GUI.Button(rect, "", m_internalTileStyle)){
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

    public void CreateNewLayer(TileSetLayer tileSetLayer)
    {
        GameObject segmentGameObject = new GameObject("MapSegmentLayer");
        var layer = segmentGameObject.AddComponent<MapSegmentLayer>();
        layer.MapSegment = MapSegment;
        layer.TileSetLayer = tileSetLayer;

        segmentGameObject.GetComponent<MeshCollider>().isTrigger = true;
        segmentGameObject.gameObject.transform.parent = MapSegment.transform;
        segmentGameObject.transform.localPosition = new Vector3();

        ApplyChanges(layer, segmentGameObject);
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

    public void AlterLayer(TileSetLayer tileSetLayer)
    {
        var segmentGameObject = GetObjectFromLayer(tileSetLayer);

        if (segmentGameObject != null) {
            var layer = segmentGameObject.GetComponent<MapSegmentLayer>();
            ApplyChanges(layer, segmentGameObject);
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

    public void ApplyChanges(MapSegmentLayer layer, GameObject gameObject)
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

        Undo.RegisterCompleteObjectUndo(meshFilter, "Updated map segment mesh");
        meshFilter.mesh = null;
        Undo.RegisterCompleteObjectUndo(meshFilter, "Updated map segment mesh");
        meshFilter.mesh = mesh;

        var meshCollider = gameObject.GetComponent<MeshCollider>();
        Undo.RegisterCompleteObjectUndo(meshCollider, "Updated map segment collider");
        meshCollider.sharedMesh = mesh;

        EditorUtility.SetDirty(layer);       
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


    void OnSceneGUI()
    {
        MapSegment mapSegment = (MapSegment)target;
        var controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.LeftAlt) {
            AltPress = true;
        } else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftAlt) {
            AltPress = false;
        }

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

            // Find the cordinates of the selected tile
            int triIndex = Mathf.FloorToInt((float)raycastHit.triangleIndex / 2);
            int y = (triIndex / Width);
            int x = triIndex - (y * Width);

            Paint(x, y, mapSegment.CurrentTile, mapSegment.CurrentTileId, mapSegment);

            // Set dirty so the editor serializes it
            EditorUtility.SetDirty(mapSegment.CurrentLayer);
        }
    }

    protected void HandleBlockBrush(MapSegment mapSegment)
    {
        if (!AltPress) {
            // Find the tile to paint
            RaycastHit raycastHit;
            bool isMouseOver = Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out raycastHit);


            // Find the cordinates of the selected tile
            int triIndex = Mathf.FloorToInt((float)raycastHit.triangleIndex / 2);
            int y = (triIndex / Width);
            int x = triIndex - (y * Width);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
                BlockStart = new IntVector2(x, y);
            } else if (Event.current.type == EventType.MouseUp && Event.current.button == 0) {

                if (BlockStart != null) {
                    IntVector2 endBlock = new IntVector2(x, y);

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
            int triIndex = Mathf.FloorToInt((float)raycastHit.triangleIndex / 2);
            int y = (triIndex / Width);
            int x = triIndex - (y * Width);

            int tileTypeId = mapSegment.CurrentLayer.TilesCollection.GetTileType(x, y);

            foreach (var tilePoint in FindAllAdjecentTilesOfType(new Point(x, y), tileTypeId, MapSegment)) {
                Paint(tilePoint.X, tilePoint.X, mapSegment.CurrentTile, mapSegment.CurrentTileId, mapSegment);
            }
        }
    }

    protected List<Point> FindAllAdjecentTilesOfType(Point startPoint, int tileTypeId, MapSegment mapSegment)
    {
        HashSet<Point> result = new HashSet<Point>();
        SearchDepthFirst(startPoint, result, mapSegment.Width, mapSegment.Height, tileTypeId, mapSegment);

        return new List<Point>(result);
    }

    protected void SearchDepthFirst(Point currentPoint, HashSet<Point> result, int MaxX, int MaxY, int tileTypeId, MapSegment mapSegment)
    {
        // If the result set already contains this, the tile has already been processedes
        if(result.Contains(currentPoint)){
            return;
        }else{

            // Make sure this tile is of the type we're looking for
            if (MapSegment.CurrentLayer.TilesCollection.GetTileType(currentPoint.X, currentPoint.Y) == tileTypeId) {
                result.Add(currentPoint);

                for (int y = -1; y <= 1; y++) {
                    for (int x = -1; x <= 1; x++) {
                        if (x != 0 || y != 0) {

                            Point tmpPoint = new Point(currentPoint.X + x, currentPoint.Y + y);

                            if (tmpPoint.X >= 0 && tmpPoint.X < MaxX && tmpPoint.Y < MaxY) {
                                SearchDepthFirst(tmpPoint, result, MaxY, MaxY, tileTypeId, mapSegment);
                            }
                        }
                    }
                }
            }
        }
    }

    public void Paint(int x, int y, Tile tile, int tileId, MapSegment target)
    {
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

        target.CurrentLayer.TilesCollection.SetTileType(x, y, tileId);
        Undo.RecordObject(targetLayer.MeshFilter, "Painted tile");
        targetLayer.MeshFilter.sharedMesh.uv = uvs;
    }
}